using Lld.App.Auth;
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
public sealed class BridgeService(IMauiAppInfo appInfo, IDeviceInfo deviceInfo, ApiClient api, SessionService session)
{
    private const string ApplicationName = "LLD Guide";

    public SharedAppInfo GetInfo() =>
        new(ApplicationName, appInfo.VersionString, deviceInfo.Platform.ToString());

    public async Task<BridgeResult<AcceptedResponse>> RequestAccess(string email, string name, string? note) =>
        BridgeResult.From(await api.RequestAccessAsync(new AccessRequestRequest(email, name, note), CancellationToken.None));

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
