namespace DeValpo.MediaIntelligence.Application.VideoSearch;

internal static class VideoSearchTextNormalizer
{
    public static string NormalizeNumber(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }
}
