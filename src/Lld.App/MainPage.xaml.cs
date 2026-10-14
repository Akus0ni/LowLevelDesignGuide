namespace Lld.App;

public partial class MainPage : ContentPage
{
    public MainPage(BridgeService bridge)
    {
        InitializeComponent();
        hybridWebView.SetInvokeJavaScriptTarget(bridge);

#if ANDROID
        // Android WebView reports env(safe-area-inset-*) as 0 (verified in spec 000), so CSS cannot pad
        // itself. Keep the page out of the status and navigation bars natively instead.
        SafeAreaEdges = SafeAreaEdges.All;
#endif
    }
}
