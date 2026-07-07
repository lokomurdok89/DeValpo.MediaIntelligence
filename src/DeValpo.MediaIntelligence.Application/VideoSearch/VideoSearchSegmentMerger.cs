using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.VideoSearch;

internal static class VideoSearchSegmentMerger
{
    public static IReadOnlyCollection<VideoSearchTargetMatchSegment> Merge(
        IReadOnlyCollection<VideoSearchTargetMatchSegment> segments,
        decimal mergeGapSeconds)
    {
        return segments
            .GroupBy(item => new { item.TargetKey, item.VisibleNumber, item.VideoAssetId, item.VideoKey, item.VideoFileName, item.SourceProvider })
            .SelectMany(group =>
            {
                var merged = new List<VideoSearchTargetMatchSegment>();
                foreach (var segment in group.OrderBy(item => item.StartTimeSeconds))
                {
                    var current = merged.LastOrDefault();
                    if (current is null || segment.StartTimeSeconds > current.EndTimeSeconds + mergeGapSeconds)
                    {
                        merged.Add(segment);
                        continue;
                    }

                    current.EndTimeSeconds = Math.Max(current.EndTimeSeconds, segment.EndTimeSeconds);
                    current.Confidence = Math.Max(current.Confidence, segment.Confidence);
                }

                return merged;
            })
            .ToArray();
    }
}
