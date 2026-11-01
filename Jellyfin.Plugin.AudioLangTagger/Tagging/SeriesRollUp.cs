using Jellyfin.Plugin.AudioLangTagger.Configuration;

namespace Jellyfin.Plugin.AudioLangTagger.Tagging;

/// <summary>
/// Decides whether a series carries a language based on its episodes.
/// </summary>
public static class SeriesRollUp
{
    /// <summary>
    /// Evaluates the roll-up.
    /// </summary>
    /// <param name="mode">The configured mode.</param>
    /// <param name="matchingEpisodes">The number of episodes with the language.</param>
    /// <param name="totalEpisodes">The number of episodes.</param>
    /// <returns>Whether the series has the language.</returns>
    public static bool Matches(SeriesRollUpMode mode, int matchingEpisodes, int totalEpisodes) => mode switch
    {
        SeriesRollUpMode.AnyEpisode => matchingEpisodes > 0,
        SeriesRollUpMode.AllEpisodes => totalEpisodes > 0 && matchingEpisodes == totalEpisodes,
        _ => false
    };
}
