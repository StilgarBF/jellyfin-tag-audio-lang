using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AudioLangTagger.Tagging;

/// <summary>
/// Tags movies and episodes shortly after they are added or updated in the library.
/// </summary>
public sealed class LibraryEventTagger : BackgroundService
{
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly ILibraryManager _libraryManager;
    private readonly TaggerService _tagger;
    private readonly ILogger<LibraryEventTagger> _logger;
    private readonly ConcurrentDictionary<Guid, byte> _pending = new();
    private long _lastEventTicks;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryEventTagger"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="tagger">Instance of the <see cref="TaggerService"/> class.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{LibraryEventTagger}"/> interface.</param>
    public LibraryEventTagger(ILibraryManager libraryManager, TaggerService tagger, ILogger<LibraryEventTagger> logger)
    {
        _libraryManager = libraryManager;
        _tagger = tagger;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _libraryManager.ItemAdded += OnItemChanged;
        _libraryManager.ItemUpdated += OnItemChanged;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
                if (!_pending.IsEmpty && DateTime.UtcNow.Ticks - Interlocked.Read(ref _lastEventTicks) >= QuietPeriod.Ticks)
                {
                    await ProcessPendingAsync(stoppingToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Server is shutting down.
        }
        finally
        {
            _libraryManager.ItemAdded -= OnItemChanged;
            _libraryManager.ItemUpdated -= OnItemChanged;
        }
    }

    private void OnItemChanged(object? sender, ItemChangeEventArgs e)
    {
        if (Plugin.Instance?.Configuration.AutoTagNewItems != true
            || e.Item is not (Movie or Episode)
            || e.Item.IsVirtualItem)
        {
            return;
        }

        _pending[e.Item.Id] = 0;
        Interlocked.Exchange(ref _lastEventTicks, DateTime.UtcNow.Ticks);
    }

    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var ids = _pending.Keys.ToList();
        foreach (var id in ids)
        {
            _pending.TryRemove(id, out _);
        }

        var seriesIds = new HashSet<Guid>();
        foreach (var id in ids)
        {
            try
            {
                var item = _libraryManager.GetItemById(id);
                if (item is null)
                {
                    continue;
                }

                await _tagger.TagVideoAsync(item, cancellationToken).ConfigureAwait(false);
                if (item is Episode { SeriesId: var seriesId } && seriesId != Guid.Empty)
                {
                    seriesIds.Add(seriesId);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to tag item {Id}", id);
            }
        }

        foreach (var seriesId in seriesIds)
        {
            try
            {
                if (_libraryManager.GetItemById(seriesId) is Series series)
                {
                    await _tagger.TagSeriesAsync(series, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to tag series {Id}", seriesId);
            }
        }
    }
}
