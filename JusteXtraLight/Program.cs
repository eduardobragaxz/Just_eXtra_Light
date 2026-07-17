using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Settings;
using System.Threading;

namespace JustExtraLight;

partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        XamlOptionalChanges.EnableChange(XamlChangeId.DefaultStyleOptimizations);
        XamlOptionalChanges.EnableChange(XamlChangeId.DeferContextFlyoutInit);
        XamlOptionalChanges.EnableChange(XamlChangeId.IconNoGridOptimization);
        XamlOptionalChanges.EnableChange(XamlChangeId.OptimizeApplyStyles);

        WinRT.ComWrappersSupport.InitializeComWrappers();

        Application.Start((p) =>
        {
            DispatcherQueueSynchronizationContext context = new(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });

        return 0;
    }
}