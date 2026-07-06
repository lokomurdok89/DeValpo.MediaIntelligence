using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.HumanReview;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.HumanReview;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class HumanReviewApplicationServiceTests
{
    [Fact]
    public async Task StartAsync_WithPendingTask_AssignsOperatorAndMovesToInReview()
    {
        var task = CreateTask();
        var service = CreateService(task);

        var response = await service.StartAsync(
            task.Id,
            "operator-1",
            new StartHumanReviewTaskRequest("Opening review"),
            CancellationToken.None);

        response.Status.Should().Be("InReview");
        response.OperatorId.Should().Be("operator-1");
        response.Comment.Should().Be("Opening review");
        task.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteAsync_WithApprove_CompletesTaskAndJob()
    {
        var task = CreateTask(status: "InReview");
        var service = CreateService(task);

        var response = await service.CompleteAsync(
            task.Id,
            "operator-1",
            new CompleteHumanReviewTaskRequest(HumanReviewDecision.Approve, "Looks good", null),
            CancellationToken.None);

        response.Status.Should().Be("Completed");
        response.Decision.Should().Be(HumanReviewDecision.Approve);
        task.Job!.Status.Should().Be(AnalysisJobStatus.Completed.ToString());
        task.Job.AnalysisResult!.RequiresHumanReview.Should().BeFalse();
        task.OriginalFieldsJson.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CompleteAsync_WithCorrect_ReplacesCurrentFieldsAndPreservesCorrections()
    {
        var task = CreateTask(status: "InReview");
        var service = CreateService(task);

        var correctedFields = new[]
        {
            new ReviewFieldCorrectionRequest("document_id", "ABC-123", 1.00m, null)
        };

        var response = await service.CompleteAsync(
            task.Id,
            "operator-1",
            new CompleteHumanReviewTaskRequest(HumanReviewDecision.Correct, "Corrected id", correctedFields),
            CancellationToken.None);

        response.Status.Should().Be("Completed");
        response.CorrectedFields.Should().ContainSingle(item => item.Name == "document_id" && item.Value == "ABC-123");
        task.Job!.AnalysisResult!.ExtractedFields.Should().ContainSingle(field =>
            field.Name == "document_id" &&
            field.Value == "ABC-123" &&
            field.SourceSkill == "HumanReview");
        task.OriginalFieldsJson.Should().NotBeNullOrWhiteSpace();
        task.CorrectedFieldsJson.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CompleteAsync_WithReject_MarksTaskRejectedAndJobFailed()
    {
        var task = CreateTask(status: "InReview");
        var service = CreateService(task);

        var response = await service.CompleteAsync(
            task.Id,
            "operator-1",
            new CompleteHumanReviewTaskRequest(HumanReviewDecision.Reject, "Not acceptable", null),
            CancellationToken.None);

        response.Status.Should().Be("Rejected");
        task.Job!.Status.Should().Be(AnalysisJobStatus.Failed.ToString());
        task.Job.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        task.Job.AnalysisResult!.RequiresHumanReview.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_WithCorrectWithoutFields_ThrowsClearError()
    {
        var task = CreateTask(status: "InReview");
        var service = CreateService(task);

        var act = () => service.CompleteAsync(
            task.Id,
            "operator-1",
            new CompleteHumanReviewTaskRequest(HumanReviewDecision.Correct, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Corrected fields are required*");
    }

    [Fact]
    public async Task StartAsync_WithCompletedTask_ThrowsClearError()
    {
        var task = CreateTask(status: "Completed");
        var service = CreateService(task);

        var act = () => service.StartAsync(
            task.Id,
            "operator-1",
            new StartHumanReviewTaskRequest(null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be started*");
    }

    private static HumanReviewApplicationService CreateService(params HumanReviewTask[] tasks)
    {
        return new HumanReviewApplicationService(
            new FakeHumanReviewTaskRepository(tasks),
            new FakeUnitOfWork());
    }

    private static HumanReviewTask CreateTask(string status = "Pending")
    {
        var job = new AnalysisJob
        {
            Id = Guid.NewGuid(),
            ClientKey = "generic",
            ProfileKey = "generic-document-ocr-v1",
            AssetType = MediaAssetType.Document.ToString(),
            Status = AnalysisJobStatus.RequiresReview.ToString(),
            AnalysisResult = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                RequiresHumanReview = true,
                Confidence = 0.70m
            }
        };

        job.AnalysisResult.ExtractedFields.Add(new ExtractedField
        {
            AnalysisResultId = job.AnalysisResult.Id,
            Name = "text",
            Value = "original",
            Confidence = 0.70m,
            SourceSkill = "GoogleDocumentAiDocumentOcrSkill"
        });

        return new HumanReviewTask
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            Job = job,
            Status = status,
            Reason = "Needs review"
        };
    }

    private sealed class FakeHumanReviewTaskRepository(params HumanReviewTask[] tasks) : IHumanReviewTaskRepository
    {
        public Task<IReadOnlyCollection<HumanReviewTask>> ListAsync(
            string? status,
            CancellationToken cancellationToken)
        {
            var result = tasks
                .Where(task => string.IsNullOrWhiteSpace(status) ||
                    string.Equals(task.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<HumanReviewTask>>(result);
        }

        public Task<HumanReviewTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
        {
            return Task.FromResult(tasks.FirstOrDefault(task => task.Id == taskId));
        }

        public Task ReplaceExtractedFieldsAsync(
            Guid analysisResultId,
            IReadOnlyCollection<ExtractedField> correctedFields,
            CancellationToken cancellationToken)
        {
            var result = tasks
                .Select(task => task.Job?.AnalysisResult)
                .First(item => item?.Id == analysisResultId);

            result!.ExtractedFields.Clear();
            result.ExtractedFields.AddRange(correctedFields);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
