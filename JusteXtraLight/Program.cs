using Microsoft.UI.Xaml.Settings;

namespace JustExtraLight;

static class Program
{
    static void Main(string[] args)
    {
        XamlOptionalChanges.EnableChange(XamlChangeId.DefaultStyleOptimizations);
        XamlOptionalChanges.EnableChange(XamlChangeId.DeferContextFlyoutInit);
        XamlOptionalChanges.EnableChange(XamlChangeId.IconNoGridOptimization);
        XamlOptionalChanges.EnableChange(XamlChangeId.OptimizeApplyStyles);

        Application.Start((p) =>
        {
            _ = new App();
        });
    }
}