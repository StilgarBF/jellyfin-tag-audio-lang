using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AudioLangTagger.Configuration;
using Jellyfin.Plugin.AudioLangTagger.Detection;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AudioLangTagger.Tagging;

/// <summary>
/// Applies language tags to movies, episodes and series.
/// </summary>
public class TaggerService
{
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly ILogger<TaggerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaggerService"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TaggerService}"/> interface.</param>
    public TaggerService(
        ILibraryManager libraryManager,
        IMediaSourceManager mediaSourceManager,
        ILogger<TaggerService> logger)
    {
        _libraryManager = libraryManager;
        _mediaSourceManager = mediaSourceManager;
        _logger = logger;
    }

    private static PluginConfiguration Config => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    /// <summary>
    /// Tags a movie or episode from its audio streams.
    /// </summary>
    /// <param name="item">The video item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the item was (or, in dry-run mode, would be) changed.</returns>
    public async Task<bool> TagVideoAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var config = Config;
        var streams = _mediaSourceManager.GetMediaStreams(item.Id)
            .Where(s => s.Type == MediaStreamType.Audio)
            .Select(s => new AudioStreamInfo(s.Index, s.Language, s.Title))
            .ToList();

        var fileName = item.Path is null ? null : System.IO.Path.GetFileName(item.Path);
        var folderName = item.ContainingFolderPath is null ? null : System.IO.Path.GetFileName(item.ContainingFolderPath.TrimEnd('/', '\\'));

        var matched = new List<LanguageRule>();
        foreach (var rule in config.Rules)
        {
            var result = LanguageDetector.Detect(rule, streams, fileName, folderName, config.Detection);
            if (result.Matched)
            {
                matched.Add(rule);
                _logger.LogDebug("{Item}: {Language} detected ({Reason})", item.Name, rule.Name, result.Reason);
            }
        }

        return await ApplyAsync(item, matched, config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Tags a series from the tags of its episodes.
    /// </summary>
    /// <param name="series">The series.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the series was (or, in dry-run mode, would be) changed.</returns>
    public async Task<bool> TagSeriesAsync(Series series, CancellationToken cancellationToken)
    {
        var config = Config;
        if (config.SeriesRollUp == SeriesRollUpMode.Off)
        {
            return false;
        }

        var episodes = _libraryManager.GetItemList(new InternalItemsQuery
        {
            AncestorIds = [series.Id],
            IncludeItemTypes = [BaseItemKind.Episode],
            Recursive = true,
            IsVirtualItem = false
        });

        var matched = config.Rules
            .Where(rule => rule.Tags.Length > 0
                && SeriesRollUp.Matches(
                    config.SeriesRollUp,
                    episodes.Count(e => e.Tags.Contains(rule.Tags[0], StringComparer.OrdinalIgnoreCase)),
                    episodes.Count))
            .ToList();

        return await ApplyAsync(series, matched, config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Tags every movie, episode and series in the library.
    /// </summary>
    /// <param name="progress">Reports progress from 0 to 100.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the scan is done.</returns>
    public async Task ScanLibraryAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var videos = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Episode],
            Recursive = true,
            IsVirtualItem = false
        });
        var allSeries = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Series],
            Recursive = true
        });

        var total = videos.Count + allSeries.Count;
        var done = 0;
        var changed = 0;

        foreach (var video in videos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await TagVideoAsync(video, cancellationToken).ConfigureAwait(false))
            {
                changed++;
            }

            progress.Report(100.0 * ++done / Math.Max(total, 1));
        }

        foreach (var series in allSeries.OfType<Series>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await TagSeriesAsync(series, cancellationToken).ConfigureAwait(false))
            {
                changed++;
            }

            progress.Report(100.0 * ++done / Math.Max(total, 1));
        }

        _logger.LogInformation("Audio language scan finished: {Changed} of {Total} items changed", changed, total);
    }

    private async Task<bool> ApplyAsync(
        BaseItem item,
        IReadOnlyCollection<LanguageRule> matched,
        PluginConfiguration config,
        CancellationToken cancellationToken)
    {
        var change = TagPlanner.Plan(item.Tags, config.Rules, matched, config.RemoveStaleTags);
        if (change.IsEmpty)
        {
            return false;
        }

        _logger.LogInformation(
            "{DryRun}{Item}: add [{Add}] remove [{Remove}]",
            config.DryRun ? "[dry-run] " : string.Empty,
            item.Name,
            string.Join(", ", change.ToAdd),
            string.Join(", ", change.ToRemove));

        if (config.DryRun)
        {
            return true;
        }

        item.Tags = item.Tags
            .Where(t => !change.ToRemove.Contains(t, StringComparer.OrdinalIgnoreCase))
            .Concat(change.ToAdd)
            .ToArray();
        await _libraryManager.UpdateItemAsync(item, item.GetParent(), ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
