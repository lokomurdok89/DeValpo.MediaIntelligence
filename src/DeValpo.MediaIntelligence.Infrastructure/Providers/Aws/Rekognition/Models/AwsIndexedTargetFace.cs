namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition.Models;

/// <summary>
/// Internal AWS face index reference mapped back to an opaque application target.
/// </summary>
internal sealed record AwsIndexedTargetFace(string TargetKey, string ReferenceImageKey, string FaceId);
