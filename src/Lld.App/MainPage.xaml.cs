namespace Lld.App;

public partial class MainPage : ContentPage
{
    public MainPage(BridgeService bridge)
    {
        InitializeComponent();
        hybridWebView.SetInvokeJavaScriptTarget(bridge);
    }
}
