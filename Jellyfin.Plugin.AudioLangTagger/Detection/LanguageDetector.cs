using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.AudioLangTagger.Detection;

/// <summary>
/// Decides whether a video contains a language, layer by layer, most reliable first.
/// </summary>
public static class LanguageDetector
{
    private static readonly string[] UndeterminedCodes = ["", "und", "zxx", "mis", "mul"];

    private static readonly string[] NonMainTrackWords =
    [
        "commentary", "kommentar", "audio description", "audiodeskription", "audiodescription", "descriptive"
    ];

    private static readonly ConcurrentDictionary<string, Regex> PatternCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Checks a video against a language rule.
    /// </summary>
    /// <param name="rule">The language to look for.</param>
    /// <param name="streams">The audio streams of the video.</param>
    /// <param name="fileName">The file name of the video.</param>
    /// <param name="folderName">The name of the folder containing the video.</param>
    /// <param name="options">The enabled detection layers.</param>
    /// <returns>The detection result.</returns>
    public static DetectionResult Detect(
        LanguageRule rule,
        IReadOnlyList<AudioStreamInfo> streams,
        string? fileName,
        string? folderName,
        DetectionOptions options)
    {
        var candidates = streams.Where(s => !IsNonMainTrack(s)).ToList();

        if (options.UseStreamLanguage)
        {
            foreach (var stream in candidates)
            {
                var code = NormalizeCode(stream.Language);
                if (rule.IsoCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase)))
                {
                    return new(true, DetectionSource.StreamLanguage, $"audio stream {stream.Index} language '{stream.Language}'");
                }
            }
        }

        if (options.UseStreamTitle)
        {
            foreach (var stream in candidates)
            {
                if (ContainsWord(stream.Title, rule.TextPatterns, out var hit))
                {
                    return new(true, DetectionSource.StreamTitle, $"audio stream {stream.Index} title contains '{hit}'");
                }
            }
        }

        var languageKnown = candidates.Any(s => !UndeterminedCodes.Contains(NormalizeCode(s.Language)));
        if (languageKnown && options.NameHintsOnlyWhenLanguageUnknown)
        {
            return DetectionResult.NoMatch;
        }

        if (options.UseFileName && ContainsWord(fileName, rule.TextPatterns, out var fileHit))
        {
            return new(true, DetectionSource.FileName, $"file name contains '{fileHit}'");
        }

        if (options.UseFolderName && ContainsWord(folderName, rule.TextPatterns, out var folderHit))
        {
            return new(true, DetectionSource.FolderName, $"folder name contains '{folderHit}'");
        }

        return DetectionResult.NoMatch;
    }

    private static string NormalizeCode(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return string.Empty;
        }

        var trimmed = language.Trim();
        var end = trimmed.IndexOfAny(['-', '_']);
        return (end < 0 ? trimmed : trimmed[..end]).ToLowerInvariant();
    }

    private static bool IsNonMainTrack(AudioStreamInfo stream)
    {
        var title = stream.Title;
        return !string.IsNullOrEmpty(title)
            && NonMainTrackWords.Any(w => title.Contains(w, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsWord(string? text, string[] patterns, out string hit)
    {
        hit = string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (var pattern in patterns)
        {
            if (!string.IsNullOrWhiteSpace(pattern) && PatternRegex(pattern).IsMatch(text))
            {
                hit = pattern;
                return true;
            }
        }

        return false;
    }

    private static Regex PatternRegex(string pattern) => PatternCache.GetOrAdd(pattern, p =>
    {
        var options = RegexOptions.CultureInvariant;
        if (p.Length > 2)
        {
            options |= RegexOptions.IgnoreCase;
        }

        return new Regex(
            string.Create(CultureInfo.InvariantCulture, $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(p)}(?![\p{{L}}\p{{N}}])"),
            options,
            TimeSpan.FromSeconds(1));
    });
}
