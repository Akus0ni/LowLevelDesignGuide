using Lld.App.Hosting;
using Lld.ServerStatus;

namespace Lld.App;

public partial class MainPage : ContentPage
{
    public MainPage(BridgeService bridge, ServerStatusTracker status, AccessRequestOutbox outbox)
    {
        InitializeComponent();
        hybridWebView.SetInvokeJavaScriptTarget(bridge);

        // Push events to React (Observer): status changes and queued requests that reached the server.
        status.Changed += (_, snapshot) => Push(BridgeEvent.ForStatus(snapshot));
        outbox.Sent += (_, request) => Push(BridgeEvent.ForAccessRequestSent(request.Email));

#if ANDROID
        // Android WebView reports env(safe-area-inset-*) as 0 (verified in spec 000), so CSS cannot pad
        // itself. Keep the page out of the status and navigation bars natively instead.
        SafeAreaEdges = SafeAreaEdges.All;
#endif
    }

    /// <summary>
    /// Android Back walks the app's own history (Learn → module → lesson → cheat sheet) and only leaves the app from
    /// the first screen. MAUI's default would leave from anywhere, since the whole app is one native page.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
#if ANDROID
        _ = GoBackOrLeaveAsync();
        return true;
#else
        return base.OnBackButtonPressed();
#endif
    }

#if ANDROID
    // react-router keeps its position in history.state.idx; 0 means the first screen of this session.
    private const string GoBackScript =
        "(window.history.state && window.history.state.idx > 0) ? (window.history.back(), 'back') : 'first'";

    private async Task GoBackOrLeaveAsync()
    {
        var result = await hybridWebView.EvaluateJavaScriptAsync(GoBackScript);
        if (result != "back")
        {
            // What Android itself does on a root screen: send the app to the background, keep it warm.
            Platform.CurrentActivity?.MoveTaskToBack(true);
        }
    }
#endif

    private void Push(BridgeEvent message) =>
        MainThread.BeginInvokeOnMainThread(() => hybridWebView.SendRawMessage(message.ToJson()));
}
