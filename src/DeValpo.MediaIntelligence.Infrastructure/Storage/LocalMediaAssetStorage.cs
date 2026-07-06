using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Storage;

public sealed class LocalMediaAssetStorage(IOptions<StorageOptions> options) : IMediaAssetStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".tif", ".tiff", ".mp4", ".mov", ".avi", ".txt"
    };

    public async Task<StoredMediaAsset> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var rootPath = Path.GetFullPath(options.Value.LocalRootPath);
        var today = DateTimeOffset.UtcNow;
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            extension = ".bin";
        }

        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(today.Year.ToString("0000"), today.Month.ToString("00"), today.Day.ToString("00"), safeFileName);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved storage path is outside the configured storage root.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = File.Create(fullPath);
        await content.CopyToAsync(output, cancellationToken);

        return new StoredMediaAsset(
            relativePath.Replace('\\', '/'),
            safeFileName,
            contentType,
            output.Length);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var rootPath = Path.GetFullPath(options.Value.LocalRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, storagePath));

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Requested storage path is outside the configured storage root.");
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }
}
