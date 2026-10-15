using Lld.App.Auth;
using Lld.App.Hosting;
using Lld.Contracts;
using IMauiAppInfo = Microsoft.Maui.ApplicationModel.IAppInfo;
using SharedAppInfo = Lld.Contracts.AppInfo;

namespace Lld.App;

/// <summary>What every bridge call returns to JavaScript: a value, or an error code and message.</summary>
public sealed record BridgeResult<T>(bool Ok, T? Value, BridgeError? Error);

public static class BridgeResult
{
    public static BridgeResult<T> From<T>(ApiResult<T> result) => new(result.Ok, result.Value, result.Error);
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
    ServerStatusMonitor status)
{
    private const string ApplicationName = "LLD Guide";

    public SharedAppInfo GetInfo() =>
        new(ApplicationName, appInfo.VersionString, deviceInfo.Platform.ToString());

    /// <summary>Sends the request, or saves it on the device while the server is offline (<c>queued: true</c>).</summary>
    public async Task<BridgeResult<AccessRequestOutcome>> RequestAccess(string email, string name, string? note) =>
        BridgeResult.From(await outbox.SubmitAsync(new AccessRequestRequest(email, name, note), CancellationToken.None));

    /// <summary>The current status; later changes arrive as <c>serverStatus</c> raw messages.</summary>
    public ServerStatusView GetServerStatus() => ServerStatusView.From(status.Tracker.Current);

    public async Task<BridgeResult<UserSummary>> Login(string email, string password) =>
        BridgeResult.From(await session.LoginAsync(email, password, CancellationToken.None));

    public async Task<BridgeResult<UserSummary?>> RestoreSession() =>
        BridgeResult.From(await session.RestoreAsync(CancellationToken.None));

    public async Task<BridgeResult<UserSummary>> ChangePassword(string currentPassword, string newPassword) =>
        BridgeResult.From(await session.ChangePasswordAsync(currentPassword, newPassword, CancellationToken.None));

    public async Task<BridgeResult<bool>> Logout() =>
        BridgeResult.From(await session.LogoutAsync(CancellationToken.None));

    public async Task<BridgeResult<bool>> LogoutAll() =>
        BridgeResult.From(await session.LogoutAllAsync(CancellationToken.None));
}
