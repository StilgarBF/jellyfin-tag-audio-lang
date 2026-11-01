namespace Jellyfin.Plugin.AudioLangTagger.Detection;

/// <summary>
/// Selects which detection layers are used.
/// </summary>
public class DetectionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the stream language tag is used.
    /// </summary>
    public bool UseStreamLanguage { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the stream title is used.
    /// </summary>
    public bool UseStreamTitle { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the file name is used.
    /// </summary>
    public bool UseFileName { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the folder name is used.
    /// </summary>
    public bool UseFolderName { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether file and folder names are only consulted
    /// when no audio stream carries a known language.
    /// </summary>
    public bool NameHintsOnlyWhenLanguageUnknown { get; set; } = true;
}
