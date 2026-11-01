namespace Jellyfin.Plugin.AudioLangTagger.Configuration;

/// <summary>
/// Controls when a series receives a language tag based on its episodes.
/// </summary>
public enum SeriesRollUpMode
{
    /// <summary>
    /// Series are not tagged.
    /// </summary>
    Off,

    /// <summary>
    /// A series is tagged when at least one episode has the language.
    /// </summary>
    AnyEpisode,

    /// <summary>
    /// A series is tagged when every episode has the language.
    /// </summary>
    AllEpisodes
}
