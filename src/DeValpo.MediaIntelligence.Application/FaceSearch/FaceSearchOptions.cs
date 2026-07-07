namespace DeValpo.MediaIntelligence.Application.FaceSearch;

public sealed class FaceSearchOptions
{
    public decimal MinSimilarity { get; set; } = 90m;
    public int MaxReferenceImagesPerJob { get; set; } = 10;
}
