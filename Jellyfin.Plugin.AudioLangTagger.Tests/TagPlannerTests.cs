using Jellyfin.Plugin.AudioLangTagger.Configuration;
using Jellyfin.Plugin.AudioLangTagger.Detection;
using Jellyfin.Plugin.AudioLangTagger.Tagging;
using Xunit;

namespace Jellyfin.Plugin.AudioLangTagger.Tests;

public class TagPlannerTests
{
    private static readonly LanguageRule German = LanguageRule.German();
    private static readonly LanguageRule French = new() { Name = "French", Tags = ["French", "Francais"] };

    [Fact]
    public void AddsMissingTags()
    {
        var change = TagPlanner.Plan(["Action"], [German], [German], true);
        Assert.Equal(["German", "Deutsch"], change.ToAdd);
        Assert.Empty(change.ToRemove);
    }

    [Fact]
    public void NoChangeWhenTagsAlreadyPresentIgnoringCase()
    {
        var change = TagPlanner.Plan(["german", "DEUTSCH"], [German], [German], true);
        Assert.True(change.IsEmpty);
    }

    [Fact]
    public void RemovesOwnedTagsOfNonMatchingRulesOnly()
    {
        var change = TagPlanner.Plan(["Action", "German", "Deutsch"], [German, French], [], true);
        Assert.Equal(["German", "Deutsch"], change.ToRemove);
        Assert.Empty(change.ToAdd);
    }

    [Fact]
    public void KeepsStaleTagsWhenRemovalDisabled()
    {
        var change = TagPlanner.Plan(["German"], [German], [], false);
        Assert.True(change.IsEmpty);
    }

    [Fact]
    public void SharedTagOfMatchedRuleIsKept()
    {
        var other = new LanguageRule { Name = "Austrian", Tags = ["Deutsch"] };
        var change = TagPlanner.Plan(["Deutsch"], [German, other], [other], true);
        Assert.DoesNotContain("Deutsch", change.ToRemove);
    }

    [Theory]
    [InlineData(SeriesRollUpMode.Off, 3, 3, false)]
    [InlineData(SeriesRollUpMode.AnyEpisode, 1, 3, true)]
    [InlineData(SeriesRollUpMode.AnyEpisode, 0, 3, false)]
    [InlineData(SeriesRollUpMode.AllEpisodes, 2, 3, false)]
    [InlineData(SeriesRollUpMode.AllEpisodes, 3, 3, true)]
    [InlineData(SeriesRollUpMode.AllEpisodes, 0, 0, false)]
    public void SeriesRollUpFollowsMode(SeriesRollUpMode mode, int matching, int total, bool expected) =>
        Assert.Equal(expected, SeriesRollUp.Matches(mode, matching, total));
}
