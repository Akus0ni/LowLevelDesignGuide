using IMauiAppInfo = Microsoft.Maui.ApplicationModel.IAppInfo;
using SharedAppInfo = Lld.Contracts.AppInfo;

namespace Lld.App;

/// <summary>
/// The C# side of the React <c>BridgeDataSource</c>. Public instance methods are callable from
/// JavaScript through <c>window.HybridWebView.InvokeDotNet("MethodName")</c>.
/// </summary>
public sealed class BridgeService(IMauiAppInfo appInfo, IDeviceInfo deviceInfo)
{
    private const string ApplicationName = "LLD Guide";

    public SharedAppInfo GetInfo() =>
        new(ApplicationName, appInfo.VersionString, deviceInfo.Platform.ToString());
}
