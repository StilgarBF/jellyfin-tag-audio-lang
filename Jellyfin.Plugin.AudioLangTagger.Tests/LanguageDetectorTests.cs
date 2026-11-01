using System.Collections.Generic;
using Jellyfin.Plugin.AudioLangTagger.Detection;
using Xunit;

namespace Jellyfin.Plugin.AudioLangTagger.Tests;

public class LanguageDetectorTests
{
    private static readonly LanguageRule German = LanguageRule.German();

    private static DetectionResult Detect(
        IReadOnlyList<AudioStreamInfo> streams,
        string file = "movie.mkv",
        string folder = "Movie",
        DetectionOptions? options = null) =>
        LanguageDetector.Detect(German, streams, file, folder, options ?? new DetectionOptions());

    private static AudioStreamInfo Stream(int index, string? lang, string? title = null) => new(index, lang, title);

    [Fact]
    public void GermanLanguageTagMatchesWithoutNameHints()
    {
        // "65 2023 DE": eng, ger, ita
        var result = Detect([Stream(1, "eng"), Stream(2, "ger"), Stream(3, "ita")], folder: "Some Film 2023");
        Assert.True(result.Matched);
        Assert.Equal(DetectionSource.StreamLanguage, result.Source);
    }

    [Theory]
    [InlineData("ger")]
    [InlineData("deu")]
    [InlineData("de")]
    [InlineData("DE")]
    [InlineData("de-AT")]
    [InlineData("de_CH")]
    public void LanguageCodeAliasesMatch(string code) =>
        Assert.True(Detect([Stream(1, code)]).Matched);

    [Fact]
    public void EnglishOnlyFileDoesNotMatch()
    {
        // "1917": eng
        Assert.False(Detect([Stream(1, "eng")], "1917.2019.720p.mkv", "1917").Matched);
    }

    [Fact]
    public void ReleaseGroupTitleDoesNotMatch()
    {
        // "Abigail 2024": title is the release group name
        var stream = Stream(1, "eng", "GalaxyRG - Abigail.2024.1080p.WEBRip.1400MB.DD5.1.x264-GalaxyRG");
        Assert.False(Detect([stream]).Matched);
    }

    [Theory]
    [InlineData("CODEC")]
    [InlineData("DEad Man")]
    [InlineData("Demon")]
    public void SubstringsInsideOtherWordsDoNotMatch(string title) =>
        Assert.False(Detect([Stream(1, "und", title)], "a.mkv", "a").Matched);

    [Fact]
    public void TitleWordMatchesWhenLanguageIsUndetermined()
    {
        var result = Detect([Stream(1, "und", "Deutsch DD5.1")]);
        Assert.True(result.Matched);
        Assert.Equal(DetectionSource.StreamTitle, result.Source);
    }

    [Fact]
    public void TwoLetterPatternIsCaseSensitive()
    {
        Assert.True(Detect([Stream(1, "und")], "Film.2020.DE.1080p.mkv").Matched);
        Assert.False(Detect([Stream(1, "und")], "Le Fabuleux Destin de Amelie.mkv").Matched);
    }

    [Fact]
    public void FolderNameIsUsedWhenStreamLanguageIsUnknown()
    {
        // "Afire (2023) German" with an untagged stream
        var result = Detect([Stream(1, "und")], folder: "Afire (2023) German");
        Assert.True(result.Matched);
        Assert.Equal(DetectionSource.FolderName, result.Source);
    }

    [Fact]
    public void NameHintsAreIgnoredWhenAnotherLanguageIsKnown()
    {
        Assert.False(Detect([Stream(1, "eng")], folder: "Film German Edition").Matched);
    }

    [Fact]
    public void NameHintsCanBeAppliedAlways()
    {
        var options = new DetectionOptions { NameHintsOnlyWhenLanguageUnknown = false };
        Assert.True(Detect([Stream(1, "eng")], folder: "Film German Edition", options: options).Matched);
    }

    [Fact]
    public void UntaggedFileWithoutHintsDoesNotMatch()
    {
        // "12 Monkeys": und
        Assert.False(Detect([Stream(1, "und")], "12.Monkeys.1995.1080p.mp4", "12 Monkeys").Matched);
    }

    [Fact]
    public void GermanCommentaryTrackDoesNotCount()
    {
        Assert.False(Detect([Stream(1, "eng"), Stream(2, "ger", "Director's Commentary")]).Matched);
        Assert.False(Detect([Stream(1, "eng"), Stream(2, "ger", "Kommentar")]).Matched);
    }

    [Fact]
    public void DisabledLayersAreSkipped()
    {
        var options = new DetectionOptions { UseStreamLanguage = false, UseStreamTitle = false, UseFileName = false, UseFolderName = false };
        Assert.False(Detect([Stream(1, "ger")], "x.German.mkv", "German", options).Matched);
    }

    [Fact]
    public void DualAudioFileMatches()
    {
        Assert.True(Detect([Stream(1, "eng"), Stream(2, "ger")]).Matched);
    }

    [Fact]
    public void NoAudioStreamsFallsBackToNames()
    {
        Assert.True(Detect([], folder: "Film German").Matched);
    }
}
