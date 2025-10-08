using Microsoft.UI.Xaml;

namespace JustExtraLight;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    private static Window? m_window;
    public static Window? MWindow => m_window;
}
