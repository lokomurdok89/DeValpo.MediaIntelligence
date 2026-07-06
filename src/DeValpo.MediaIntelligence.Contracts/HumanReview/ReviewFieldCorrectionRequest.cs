namespace DeValpo.MediaIntelligence.Contracts.HumanReview;

public sealed record ReviewFieldCorrectionRequest(
    string Name,
    string? Value,
    decimal? Confidence,
    string? SourceSkill);
