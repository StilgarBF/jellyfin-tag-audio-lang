using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.AudioLangTagger.Tagging;

/// <summary>
/// Scheduled task that tags the whole library.
/// </summary>
public class TagLanguagesTask : IScheduledTask
{
    private readonly TaggerService _tagger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagLanguagesTask"/> class.
    /// </summary>
    /// <param name="tagger">Instance of the <see cref="TaggerService"/> class.</param>
    public TagLanguagesTask(TaggerService tagger)
    {
        _tagger = tagger;
    }

    /// <inheritdoc />
    public string Name => "Tag audio languages";

    /// <inheritdoc />
    public string Key => "AudioLangTaggerScan";

    /// <inheritdoc />
    public string Description => "Adds language tags to movies, episodes and series based on their audio tracks.";

    /// <inheritdoc />
    public string Category => "Audio Language Tagger";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) =>
        _tagger.ScanLibraryAsync(progress, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
}
