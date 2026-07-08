using System.Security.Claims;
using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Api.Controllers;
using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Api.Validation;
using DeValpo.MediaIntelligence.Application.Audit;
using DeValpo.MediaIntelligence.Application.FaceSearch;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.Audit;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Contracts.FaceSearch;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class FaceSearchJobsControllerTests
{
    [Fact]
    public async Task Create_RecordsSanitizedAuditEvent_WhenJobIsQueued()
    {
        var audit = new FakeAuditService();
        var service = new FakeFaceSearchApplicationService();
        var controller = CreateController(service, auditService: audit);

        var result = await controller.Create(
            "generic",
            "event-face-search-v1",
            "external-consent-secret",
            """{"targets":[{"targetKey":"target-1","referenceImageKey":"reference-1"}]}""",
            [CreateFile("race.mp4", "video/mp4")],
            [CreateFile("face.jpg", "image/jpeg")],
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        audit.Events.Should().ContainSingle();
        var auditEvent = audit.Events.Single();
        auditEvent.EventType.Should().Be("face_search.created");
        auditEvent.Outcome.Should().Be("Success");
        auditEvent.Metadata.Should().ContainKey("videoCount").WhoseValue.Should().Be("1");
        auditEvent.Metadata.Should().ContainKey("referenceImageCount").WhoseValue.Should().Be("1");
        auditEvent.Metadata.Should().ContainKey("consentReferenceProvided").WhoseValue.Should().Be("True");
        auditEvent.Metadata!.Values.Should().NotContain(value => value.Contains("external-consent-secret", StringComparison.OrdinalIgnoreCase));
        service.LastCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_RecordsDeniedAuditEvent_WhenClientCannotSubmit()
    {
        var audit = new FakeAuditService();
        var service = new FakeFaceSearchApplicationService();
        var controller = CreateController(
            service,
            audit,
            new FakeAccessService { CanSubmit = false });

        var result = await controller.Create(
            "generic",
            "event-face-search-v1",
            "external-consent-secret",
            """{"targets":[{"targetKey":"target-1","referenceImageKey":"reference-1"}]}""",
            [CreateFile("race.mp4", "video/mp4")],
            [CreateFile("face.jpg", "image/jpeg")],
            CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        audit.Events.Should().ContainSingle();
        audit.Events.Single().EventType.Should().Be("face_search.created");
        audit.Events.Single().Outcome.Should().Be("Denied");
        audit.Events.Single().Metadata!.Values.Should().NotContain(value => value.Contains("external-consent-secret", StringComparison.OrdinalIgnoreCase));
        service.LastCommand.Should().BeNull();
    }

    [Fact]
    public async Task GetResult_RecordsSanitizedAuditEvent_WhenResultIsRead()
    {
        var jobId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var audit = new FakeAuditService();
        var controller = CreateController(
            new FakeFaceSearchApplicationService
            {
                Result = new FaceSearchResultResponse(
                    jobId,
                    "generic",
                    "Completed",
                    "external-consent-secret",
                    [
                        new FaceSearchMatchSegmentResponse(
                            "target-1",
                            "reference-1",
                            "video-1",
                            "race.mp4",
                            10m,
                            10m,
                            "00:00:10.000",
                            "00:00:10.000",
                            95m,
                            "FaceReference",
                            "AwsRekognition")
                    ],
                    [new AnalysisIssueResponse("WARN", "sanitized warning", "Warning", "AwsRekognition")],
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow)
            },
            auditService: audit);

        var result = await controller.GetResult(jobId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        audit.Events.Should().ContainSingle();
        var auditEvent = audit.Events.Single();
        auditEvent.EventType.Should().Be("face_search.result_read");
        auditEvent.Outcome.Should().Be("Success");
        auditEvent.Metadata.Should().ContainKey("matchCount").WhoseValue.Should().Be("1");
        auditEvent.Metadata.Should().ContainKey("issueCount").WhoseValue.Should().Be("1");
        auditEvent.Metadata!.Values.Should().NotContain(value => value.Contains("external-consent-secret", StringComparison.OrdinalIgnoreCase));
    }

    private static FaceSearchJobsController CreateController(
        FakeFaceSearchApplicationService? service = null,
        FakeAuditService? auditService = null,
        FakeAccessService? accessService = null)
    {
        var controller = new FaceSearchJobsController(
            service ?? new FakeFaceSearchApplicationService(),
            accessService ?? new FakeAccessService(),
            new FakeFileValidationService(),
            Options.Create(new VideoAnalysisOptions()),
            Options.Create(new FaceSearchOptions()),
            Options.Create(new UploadValidationOptions()),
            auditService ?? new FakeAuditService());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("client_key", "generic"),
                        new Claim(ClaimTypes.NameIdentifier, "tester")
                    ],
                    "Test"))
            }
        };

        return controller;
    }

    private static IFormFile CreateFile(string fileName, string contentType)
    {
        var stream = new MemoryStream([0x00, 0x00, 0x00, 0x18]);
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class FakeFaceSearchApplicationService : IFaceSearchApplicationService
    {
        public CreateFaceSearchJobCommand? LastCommand { get; private set; }
        public FaceSearchResultResponse? Result { get; init; }

        public Task<CreateFaceSearchJobResponse> EnqueueAsync(CreateFaceSearchJobCommand command, CancellationToken cancellationToken)
        {
            LastCommand = command;
            return Task.FromResult(new CreateFaceSearchJobResponse(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                "Pending",
                DateTimeOffset.UtcNow));
        }

        public Task<bool> ProcessNextPendingAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<int> ResetRunningJobsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task<FaceSearchJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FaceSearchJobResponse?>(null);
        }

        public Task<FaceSearchResultResponse?> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeAccessService : IRequestClientAccessService
    {
        public bool CanSubmit { get; init; } = true;

        public Task<bool> CanSubmitAsync(ClaimsPrincipal user, string clientKey, string profileKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(CanSubmit);
        }

        public bool CanAccessClientResource(ClaimsPrincipal user, string resourceClientKey)
        {
            return true;
        }

        public bool HasOperationalAccess(ClaimsPrincipal user)
        {
            return false;
        }
    }

    private sealed class FakeFileValidationService : IUploadedFileValidationService
    {
        public Task<UploadedFileValidationResult> ValidateAsync(IFormFile? file, MediaAssetType assetType, CancellationToken cancellationToken)
        {
            return Task.FromResult(UploadedFileValidationResult.Valid);
        }

        public Task<UploadedFileValidationResult> ValidateAsync(
            IFormFile? file,
            MediaAssetType assetType,
            long maxFileSizeBytes,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(UploadedFileValidationResult.Valid);
        }
    }

    private sealed class FakeAuditService : IAuditApplicationService
    {
        public List<AuditEventRequest> Events { get; } = [];

        public Task RecordAsync(AuditEventRequest request, CancellationToken cancellationToken)
        {
            Events.Add(request);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<AuditEventResponse>> ListAsync(
            string? clientKey,
            string? eventType,
            int limit,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AuditEventResponse>>([]);
        }
    }
}
