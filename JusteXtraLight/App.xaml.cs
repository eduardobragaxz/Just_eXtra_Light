namespace JustExtraLight;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        MWindow = new MainWindow();
        MWindow.Activate();
    }

    public static Window? MWindow { get; private set; }
}
