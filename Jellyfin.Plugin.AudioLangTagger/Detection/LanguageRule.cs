namespace Jellyfin.Plugin.AudioLangTagger.Detection;

/// <summary>
/// Describes how a language is recognised and which tags it produces.
/// </summary>
public class LanguageRule
{
    /// <summary>
    /// Gets or sets the display name, used in log messages.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO 639 codes (alpha-2, alpha-3 B and T) matched against the stream language tag.
    /// </summary>
    public string[] IsoCodes { get; set; } = [];

    /// <summary>
    /// Gets or sets the words matched in stream titles, file names and folder names.
    /// Matching is by whole word; patterns of two characters or fewer are case-sensitive.
    /// </summary>
    public string[] TextPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the tags added to items in this language.
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Gets the built-in German rule.
    /// </summary>
    /// <returns>A new German rule.</returns>
    public static LanguageRule German() => new()
    {
        Name = "German",
        IsoCodes = ["de", "deu", "ger"],
        TextPatterns = ["German", "Deutsch", "DE"],
        Tags = ["German", "Deutsch"]
    };
}
