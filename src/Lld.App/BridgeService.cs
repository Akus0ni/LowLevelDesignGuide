using Lld.App.Auth;
using Lld.App.Content;
using Lld.App.Hosting;
using Lld.App.Progress;
using Lld.Contracts;
using IMauiAppInfo = Microsoft.Maui.ApplicationModel.IAppInfo;
using SharedAppInfo = Lld.Contracts.AppInfo;

namespace Lld.App;

/// <summary>What every bridge call returns to JavaScript: a value, or an error code and message.</summary>
public sealed record BridgeResult<T>(bool Ok, T? Value, BridgeError? Error);

public static class BridgeResult
{
    public static BridgeResult<T> From<T>(ApiResult<T> result) => new(result.Ok, result.Value, result.Error);

    public static BridgeResult<T> Success<T>(T value) => new(true, value, null);

    public static BridgeResult<T> Failure<T>(BridgeError error) => new(false, default, error);
}

/// <summary>
/// The C# side of the React <c>BridgeDataSource</c>. Public instance methods are callable from
/// JavaScript through <c>window.HybridWebView.InvokeDotNet("MethodName", [args])</c>.
/// Tokens never cross the bridge: React only receives the user summary.
/// </summary>
public sealed class BridgeService(
    IMauiAppInfo appInfo,
    IDeviceInfo deviceInfo,
    SessionService session,
    AccessRequestOutbox outbox,
    ServerStatusMonitor status,
    ContentService content,
    ProgressService progress,
    IBrowser browser)
{
    private const string ApplicationName = "LLD Guide";

    public static readonly BridgeError LinkNotAllowed = new("link.not_allowed", "Only web links (http or https) can be opened.");

    public SharedAppInfo GetInfo() =>
        new(ApplicationName, appInfo.VersionString, deviceInfo.Platform.ToString());

    /// <summary>Sends the request, or saves it on the device while the server is offline (<c>queued: true</c>).</summary>
    public async Task<BridgeResult<AccessRequestOutcome>> RequestAccess(string email, string name, string? note) =>
        BridgeResult.From(await outbox.SubmitAsync(new AccessRequestRequest(email, name, note), CancellationToken.None));

    /// <summary>The current status; later changes arrive as <c>serverStatus</c> raw messages.</summary>
    public ServerStatusView GetServerStatus() => ServerStatusView.From(status.Tracker.Current);

    /// <summary>Signs in, then gives what the guest read on this device to the user (spec 005 §3.9).</summary>
    public async Task<BridgeResult<UserSummary>> Login(string email, string password)
    {
        var result = await session.LoginAsync(email, password, CancellationToken.None);
        if (result.Value is { } user)
        {
            await progress.AdoptGuestAsync(user.Id);
        }

        return BridgeResult.From(result);
    }

    public async Task<BridgeResult<UserSummary?>> RestoreSession()
    {
        var result = await session.RestoreAsync(CancellationToken.None);
        if (result.Value is { } user)
        {
            await progress.AdoptGuestAsync(user.Id);
        }

        return BridgeResult.From(result);
    }

    public async Task<BridgeResult<UserSummary>> ChangePassword(string currentPassword, string newPassword) =>
        BridgeResult.From(await session.ChangePasswordAsync(currentPassword, newPassword, CancellationToken.None));

    public async Task<BridgeResult<bool>> Logout() =>
        BridgeResult.From(await session.LogoutAsync(CancellationToken.None));

    public async Task<BridgeResult<bool>> LogoutAll() =>
        BridgeResult.From(await session.LogoutAllAsync(CancellationToken.None));

    // Content (spec 003): read from the device database, so these work offline.

    public Task<BridgeResult<IReadOnlyList<ModuleSummary>>> GetModules() => content.GetModulesAsync();

    public Task<BridgeResult<ModuleDetail>> GetModule(string slug) => content.GetModuleAsync(slug);

    public Task<BridgeResult<LessonDetail>> GetLesson(string id) => content.GetLessonAsync(id);

    /// <param name="moduleSlug">One module's cheat sheets, or every cheat sheet when null.</param>
    public Task<BridgeResult<IReadOnlyList<CheatSheetSummary>>> GetCheatSheets(string? moduleSlug) => content.GetCheatSheetsAsync(moduleSlug);

    public Task<BridgeResult<CheatSheetDetail>> GetCheatSheet(string lessonId) => content.GetCheatSheetAsync(lessonId);

    // Progress (spec 005): kept on the device for whoever owns this session (or the guest), so these work offline.

    public Task<BridgeResult<ProgressSnapshotDto>> GetProgress() => progress.GetAsync();

    /// <param name="recordsJson">JSON array of lesson progress records; the device id and owner are filled in here.</param>
    public Task<BridgeResult<bool>> SaveLessonProgress(string recordsJson) => progress.SaveLessonsAsync(recordsJson);

    /// <param name="day">The learner's local date, <c>yyyy-MM-dd</c>.</param>
    public Task<BridgeResult<bool>> RecordActivity(string day) => progress.RecordActivityAsync(day);

    /// <summary>
    /// Opens a web link (e.g. source code on GitHub) in the system browser, so the WebView never leaves the app.
    /// Only http and https: a lesson can't make the app open other schemes.
    /// </summary>
    public async Task<BridgeResult<bool>> OpenExternal(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return BridgeResult.Failure<bool>(LinkNotAllowed);
        }

        var opened = await MainThread.InvokeOnMainThreadAsync(() => browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred));
        return BridgeResult.Success(opened);
    }
}
