using System.IO;
using System.Xml.Serialization;
using Jellyfin.Plugin.AudioLangTagger.Detection;
using Xunit;

namespace Jellyfin.Plugin.AudioLangTagger.Tests;

public class ConfigSerializationTests
{
    [Fact]
    public void RuleSurvivesXmlRoundTrip()
    {
        var serializer = new XmlSerializer(typeof(LanguageRule[]));
        using var writer = new StringWriter();
        serializer.Serialize(writer, new[] { LanguageRule.German() });
        using var reader = new StringReader(writer.ToString());
        var rules = (LanguageRule[])serializer.Deserialize(reader)!;

        var rule = Assert.Single(rules);
        Assert.Equal(["de", "deu", "ger"], rule.IsoCodes);
        Assert.Equal(["German", "Deutsch"], rule.Tags);
    }

    [Fact]
    public void DetectionOptionsSurviveXmlRoundTrip()
    {
        var serializer = new XmlSerializer(typeof(DetectionOptions));
        using var writer = new StringWriter();
        serializer.Serialize(writer, new DetectionOptions { UseFolderName = false });
        using var reader = new StringReader(writer.ToString());
        var options = (DetectionOptions)serializer.Deserialize(reader)!;

        Assert.False(options.UseFolderName);
        Assert.True(options.UseStreamLanguage);
    }
}
