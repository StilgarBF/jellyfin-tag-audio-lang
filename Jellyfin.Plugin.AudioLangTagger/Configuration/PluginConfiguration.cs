using Jellyfin.Plugin.AudioLangTagger.Detection;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AudioLangTagger.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the languages to detect.
    /// </summary>
    public LanguageRule[] Rules { get; set; } = [LanguageRule.German()];

    /// <summary>
    /// Gets or sets the enabled detection layers.
    /// </summary>
    public DetectionOptions Detection { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether tags of the configured rules are removed from items that no longer match.
    /// </summary>
    public bool RemoveStaleTags { get; set; } = true;

    /// <summary>
    /// Gets or sets how episodes are rolled up into series tags.
    /// </summary>
    public SeriesRollUpMode SeriesRollUp { get; set; } = SeriesRollUpMode.AnyEpisode;

    /// <summary>
    /// Gets or sets a value indicating whether newly added and updated items are tagged automatically.
    /// </summary>
    public bool AutoTagNewItems { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether changes are only logged and not applied.
    /// </summary>
    public bool DryRun { get; set; }
}
