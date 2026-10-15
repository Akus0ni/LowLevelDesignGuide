using Lld.App.Content;
using Lld.App.Hosting;

namespace Lld.App;

public partial class App : Application
{
    private readonly MainPage _mainPage;
    private readonly ServerStatusMonitor _status;
    private readonly AccessRequestOutbox _outbox;
    private readonly ContentGate _content;

    public App(MainPage mainPage, ServerStatusMonitor status, AccessRequestOutbox outbox, ContentGate content)
    {
        InitializeComponent();
        _mainPage = mainPage;
        _status = status;
        _outbox = outbox;
        _content = content;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(_mainPage) { Title = "LLD Guide" };

    // Listen for server status only while the app is in the foreground (no polling, no background data).
    protected override void OnStart()
    {
        // Prepare lessons while the WebView loads, so the first screen doesn't wait for them.
        _ = _content.ReadyAsync();
        Resume();
    }

    protected override void OnResume() => Resume();

    protected override void OnSleep() => _status.Stop();

    private void Resume()
    {
        _status.Start();
        _ = _outbox.FlushAsync(CancellationToken.None); // anything saved while offline
    }
}
