using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Contracts.HumanReview;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.HumanReview;

public sealed class HumanReviewApplicationService(
    IHumanReviewTaskRepository repository,
    IUnitOfWork unitOfWork) : IHumanReviewApplicationService
{
    private const string PendingStatus = "Pending";
    private const string InReviewStatus = "InReview";
    private const string CompletedStatus = "Completed";
    private const string RejectedStatus = "Rejected";
    private const string HumanReviewSourceSkill = "HumanReview";

    /// <summary>
    /// Lists review tasks with current result fields for operator queues.
    /// </summary>
    public async Task<IReadOnlyCollection<HumanReviewTaskResponse>> ListAsync(
        string? status,
        CancellationToken cancellationToken)
    {
        var tasks = await repository.ListAsync(status, cancellationToken);
        return tasks.Select(Map).ToArray();
    }

    /// <summary>
    /// Returns the requested review task with current result fields.
    /// </summary>
    public async Task<HumanReviewTaskResponse?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(taskId, cancellationToken);
        return task is null ? null : Map(task);
    }

    /// <summary>
    /// Assigns a pending task to an operator and marks it in review.
    /// </summary>
    public async Task<HumanReviewTaskResponse> StartAsync(
        Guid taskId,
        string operatorId,
        StartHumanReviewTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await GetRequiredTaskAsync(taskId, cancellationToken);
        if (!string.Equals(task.Status, PendingStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(ApplicationResourceText.HumanReviewTaskCannotStart(taskId, task.Status));
        }

        var now = DateTimeOffset.UtcNow;
        task.Status = InReviewStatus;
        task.OperatorId = operatorId;
        task.Comment = request.Comment;
        task.StartedAt = now;
        task.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(task);
    }

    /// <summary>
    /// Applies the operator decision and persists audit data for the review task.
    /// </summary>
    public async Task<HumanReviewTaskResponse> CompleteAsync(
        Guid taskId,
        string operatorId,
        CompleteHumanReviewTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await GetRequiredTaskAsync(taskId, cancellationToken);
        if (!string.Equals(task.Status, PendingStatus, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(task.Status, InReviewStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(ApplicationResourceText.HumanReviewTaskCannotComplete(taskId, task.Status));
        }

        var result = task.Job?.AnalysisResult
            ?? throw new InvalidOperationException(ApplicationResourceText.HumanReviewResultMissing(taskId));
        var decision = NormalizeDecision(request.Decision);
        var now = DateTimeOffset.UtcNow;

        task.OriginalFieldsJson ??= JsonSerializer.Serialize(ToFieldCorrections(result.ExtractedFields));
        task.OperatorId = operatorId;
        task.Decision = decision;
        task.Comment = request.Comment;
        task.CompletedAt = now;
        task.UpdatedAt = now;

        if (task.StartedAt is null)
        {
            task.StartedAt = now;
        }

        switch (decision)
        {
            case HumanReviewDecision.Approve:
                task.Status = CompletedStatus;
                result.RequiresHumanReview = false;
                task.Job!.Status = AnalysisJobStatus.Completed.ToString();
                task.Job.ErrorMessage = null;
                task.Job.CompletedAt = now;
                break;
            case HumanReviewDecision.Correct:
                var correctedFields = RequireCorrections(request.CorrectedFields);
                task.Status = CompletedStatus;
                task.CorrectedFieldsJson = JsonSerializer.Serialize(correctedFields);
                result.RequiresHumanReview = false;
                await repository.ReplaceExtractedFieldsAsync(
                    result.Id,
                    CreateCorrectedFields(result.Id, correctedFields),
                    cancellationToken);
                task.Job!.Status = AnalysisJobStatus.Completed.ToString();
                task.Job.ErrorMessage = null;
                task.Job.CompletedAt = now;
                break;
            case HumanReviewDecision.Reject:
                task.Status = RejectedStatus;
                result.RequiresHumanReview = true;
                task.Job!.Status = AnalysisJobStatus.Failed.ToString();
                task.Job.ErrorMessage = ApplicationResourceText.HumanReviewRejectedResult;
                task.Job.CompletedAt = now;
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(task);
    }

    private async Task<HumanReviewTask> GetRequiredTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.HumanReviewTaskNotFound(taskId));
    }

    private static string NormalizeDecision(string decision)
    {
        if (string.IsNullOrWhiteSpace(decision))
        {
            throw new ArgumentException(ApplicationResourceText.HumanReviewDecisionRequired, nameof(decision));
        }

        if (string.Equals(decision, HumanReviewDecision.Approve, StringComparison.OrdinalIgnoreCase))
        {
            return HumanReviewDecision.Approve;
        }

        if (string.Equals(decision, HumanReviewDecision.Correct, StringComparison.OrdinalIgnoreCase))
        {
            return HumanReviewDecision.Correct;
        }

        if (string.Equals(decision, HumanReviewDecision.Reject, StringComparison.OrdinalIgnoreCase))
        {
            return HumanReviewDecision.Reject;
        }

        throw new ArgumentException(ApplicationResourceText.HumanReviewDecisionUnsupported(decision), nameof(decision));
    }

    private static IReadOnlyCollection<ReviewFieldCorrectionRequest> RequireCorrections(
        IReadOnlyCollection<ReviewFieldCorrectionRequest>? correctedFields)
    {
        if (correctedFields is null || correctedFields.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.HumanReviewCorrectionsRequired, nameof(correctedFields));
        }

        return correctedFields;
    }

    private static IReadOnlyCollection<ExtractedField> CreateCorrectedFields(
        Guid analysisResultId,
        IReadOnlyCollection<ReviewFieldCorrectionRequest> correctedFields)
    {
        return correctedFields
            .Select(field => new ExtractedField
            {
                AnalysisResultId = analysisResultId,
                Name = field.Name,
                Value = field.Value,
                Confidence = field.Confidence ?? 1.00m,
                SourceSkill = string.IsNullOrWhiteSpace(field.SourceSkill)
                    ? HumanReviewSourceSkill
                    : field.SourceSkill
            })
            .ToArray();
    }

    private static HumanReviewTaskResponse Map(HumanReviewTask task)
    {
        var result = task.Job?.AnalysisResult;
        return new HumanReviewTaskResponse(
            task.Id,
            task.JobId,
            task.Job?.ClientKey ?? string.Empty,
            task.Job?.ProfileKey ?? string.Empty,
            task.Status,
            task.Reason,
            task.OperatorId,
            task.Decision,
            task.Comment,
            result?.ExtractedFields.Select(MapField).ToArray() ?? [],
            DeserializeCorrections(task.CorrectedFieldsJson),
            task.CreatedAt,
            task.StartedAt,
            task.CompletedAt,
            task.UpdatedAt);
    }

    private static ExtractedFieldResponse MapField(ExtractedField field)
    {
        return new ExtractedFieldResponse(field.Name, field.Value, field.Confidence, field.SourceSkill);
    }

    private static IReadOnlyCollection<ReviewFieldCorrectionRequest> ToFieldCorrections(IEnumerable<ExtractedField> fields)
    {
        return fields
            .Select(field => new ReviewFieldCorrectionRequest(field.Name, field.Value, field.Confidence, field.SourceSkill))
            .ToArray();
    }

    private static IReadOnlyCollection<ReviewFieldCorrectionRequest> DeserializeCorrections(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<ReviewFieldCorrectionRequest[]>(json) ?? [];
    }
}
