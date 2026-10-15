using System.Diagnostics;
using Lld.App.Data.Content;
using Lld.App.Data.Schema;
using Microsoft.Extensions.Logging;

namespace Lld.App.Content;

/// <summary>
/// Prepares the device database once per app run: schema steps, then the bundled content (only when it changed).
/// Every content call awaits <see cref="ReadyAsync"/>; the first one waits for the work, later ones return at once.
/// A failed attempt is retried by the next call instead of leaving the app without lessons until a restart.
/// </summary>
public sealed partial class ContentGate(
    SchemaMigrator migrator,
    ContentPackLoader loader,
    IContentPackSource source,
    ILogger<ContentGate> logger)
{
    private readonly Lock _lock = new();
    private Task? _ready;

    public Task ReadyAsync()
    {
        lock (_lock)
        {
            if (_ready is null || _ready.IsFaulted || _ready.IsCanceled)
            {
                _ready = Task.Run(PrepareAsync);
            }

            return _ready;
        }
    }

    private async Task PrepareAsync()
    {
        var clock = Stopwatch.StartNew();
        var steps = await migrator.MigrateAsync(CancellationToken.None).ConfigureAwait(false);
        var result = await loader.EnsureLoadedAsync(source, CancellationToken.None).ConfigureAwait(false);
        LogReady(result.Loaded ? "loaded" : "already current", result.Version, steps, clock.ElapsedMilliseconds);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Content {Outcome} (version {Version}, {Steps} schema step(s)) in {Milliseconds} ms")]
    private partial void LogReady(string outcome, string version, int steps, long milliseconds);
}
