namespace Jellyfin.Plugin.AudioLangTagger.Detection;

/// <summary>
/// The parts of an audio stream that language detection looks at.
/// </summary>
/// <param name="Index">The stream index within the file.</param>
/// <param name="Language">The stream language tag, if any.</param>
/// <param name="Title">The stream title, if any.</param>
public sealed record AudioStreamInfo(int Index, string? Language, string? Title);
