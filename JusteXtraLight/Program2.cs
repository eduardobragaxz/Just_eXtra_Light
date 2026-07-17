using Microsoft.UI.Xaml.Settings;

namespace JustExtraLight;

static class Program2
{
    static void Main2(string[] args)
    {
        //XamlOptionalChanges.EnableChange(XamlChangeId.DefaultStyleOptimizations);
        //XamlOptionalChanges.EnableChange(XamlChangeId.DeferContextFlyoutInit);
        //XamlOptionalChanges.EnableChange(XamlChangeId.IconNoGridOptimization);
        //XamlOptionalChanges.EnableChange(XamlChangeId.OptimizeApplyStyles);

        //XamlOptionalChanges.DisableChange(XamlChangeId.DefaultStyleOptimizations);
        //XamlOptionalChanges.DisableChange(XamlChangeId.DeferContextFlyoutInit);
        //XamlOptionalChanges.DisableChange(XamlChangeId.IconNoGridOptimization);
        //XamlOptionalChanges.DisableChange(XamlChangeId.OptimizeApplyStyles);

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start((p) =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}