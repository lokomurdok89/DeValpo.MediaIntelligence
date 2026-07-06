namespace DeValpo.MediaIntelligence.Application.Models;

public sealed record StoredMediaAsset(
    string StoragePath,
    string FileName,
    string ContentType,
    long SizeInBytes);
