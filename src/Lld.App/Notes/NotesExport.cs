using System.Text.RegularExpressions;
using Lld.App.Auth;

namespace Lld.App.Notes;

/// <summary>
/// Hands the exported notes file to the system share sheet (spec 007 §3.7): React builds the Markdown, this writes it to
/// the app's cache folder and opens <see cref="IShare"/>, so the learner can save or send it. No network.
/// </summary>
public sealed partial class NotesExport(IShare share)
{
    public static readonly BridgeError BadFileName =
        new("validation.notes.file_name", "The export file name must look like lld-notes-2026-10-20.md.");

    private const string Title = "My LLD notes";
    private const string MarkdownType = "text/markdown";

    public async Task<BridgeResult<bool>> ShareAsync(string fileName, string markdown)
    {
        if (!ExportFileName().IsMatch(fileName ?? string.Empty))
        {
            return BridgeResult.Failure<bool>(BadFileName);
        }

        var path = Path.Combine(FileSystem.CacheDirectory, fileName!);
        await File.WriteAllTextAsync(path, markdown ?? string.Empty).ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(
            () => share.RequestAsync(new ShareFileRequest { Title = Title, File = new ShareFile(path, MarkdownType) })).ConfigureAwait(false);
        return BridgeResult.Success(true);
    }

    /// <summary>Only the file name React makes: nothing that could leave the cache folder.</summary>
    [GeneratedRegex(@"^lld-notes-\d{4}-\d{2}-\d{2}\.md$")]
    private static partial Regex ExportFileName();
}
