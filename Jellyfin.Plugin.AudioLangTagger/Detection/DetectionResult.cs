namespace Jellyfin.Plugin.AudioLangTagger.Detection;

/// <summary>
/// The layer that produced a match.
/// </summary>
public enum DetectionSource
{
    /// <summary>
    /// Nothing matched.
    /// </summary>
    None,

    /// <summary>
    /// The stream language tag.
    /// </summary>
    StreamLanguage,

    /// <summary>
    /// The stream title.
    /// </summary>
    StreamTitle,

    /// <summary>
    /// The file name.
    /// </summary>
    FileName,

    /// <summary>
    /// The folder name.
    /// </summary>
    FolderName
}

/// <summary>
/// The outcome of language detection.
/// </summary>
/// <param name="Matched">Whether the language was found.</param>
/// <param name="Source">The layer that produced the match.</param>
/// <param name="Reason">A human readable explanation.</param>
public sealed record DetectionResult(bool Matched, DetectionSource Source, string Reason)
{
    /// <summary>
    /// Gets the result for "language not found".
    /// </summary>
    public static DetectionResult NoMatch { get; } = new(false, DetectionSource.None, "no match");
}
