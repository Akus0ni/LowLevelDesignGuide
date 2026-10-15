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

    private void Push(BridgeEvent message) =>
        MainThread.BeginInvokeOnMainThread(() => hybridWebView.SendRawMessage(message.ToJson()));
}
