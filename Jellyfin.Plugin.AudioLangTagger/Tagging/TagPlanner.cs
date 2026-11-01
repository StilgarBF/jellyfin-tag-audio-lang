using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.AudioLangTagger.Detection;

namespace Jellyfin.Plugin.AudioLangTagger.Tagging;

/// <summary>
/// The tags to add to and remove from an item.
/// </summary>
/// <param name="ToAdd">Tags that are missing.</param>
/// <param name="ToRemove">Tags that no longer apply.</param>
public sealed record TagChange(IReadOnlyList<string> ToAdd, IReadOnlyList<string> ToRemove)
{
    /// <summary>
    /// Gets a value indicating whether nothing has to change.
    /// </summary>
    public bool IsEmpty => ToAdd.Count == 0 && ToRemove.Count == 0;
}

/// <summary>
/// Works out tag changes. Only tags that belong to a configured rule are ever removed,
/// so tags added by hand under other names are left alone.
/// </summary>
public static class TagPlanner
{
    /// <summary>
    /// Plans the tag changes for one item.
    /// </summary>
    /// <param name="currentTags">The tags currently on the item.</param>
    /// <param name="rules">All configured rules.</param>
    /// <param name="matchedRules">The rules the item matches.</param>
    /// <param name="removeStale">Whether tags of rules that do not match are removed.</param>
    /// <returns>The planned change.</returns>
    public static TagChange Plan(
        IReadOnlyCollection<string> currentTags,
        IReadOnlyCollection<LanguageRule> rules,
        IReadOnlyCollection<LanguageRule> matchedRules,
        bool removeStale)
    {
        var desired = matchedRules.SelectMany(r => r.Tags).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var owned = rules.SelectMany(r => r.Tags).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var current = currentTags.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = matchedRules.SelectMany(r => r.Tags)
            .Where(t => !current.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var toRemove = removeStale
            ? currentTags.Where(t => owned.Contains(t) && !desired.Contains(t)).ToList()
            : [];

        return new TagChange(toAdd, toRemove);
    }
}
