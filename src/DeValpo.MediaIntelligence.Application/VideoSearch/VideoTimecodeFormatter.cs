namespace DeValpo.MediaIntelligence.Application.VideoSearch;

internal static class VideoTimecodeFormatter
{
    public static string Format(decimal seconds)
    {
        var timeSpan = TimeSpan.FromSeconds((double)seconds);
        return timeSpan.Hours > 0
            ? timeSpan.ToString(@"hh\:mm\:ss\.fff")
            : timeSpan.ToString(@"mm\:ss\.fff");
    }
}
