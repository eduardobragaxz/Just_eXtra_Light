namespace JustExtraLight;

public sealed partial class MainPage : Page
{
    readonly MainPageViewModel? viewModel;
    public MainPage()
    {
        InitializeComponent();
        viewModel = new MainPageViewModel(Microsoft.Windows.Storage.ApplicationData.GetDefault().TemporaryFolder);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        App.MWindow!.SetTitleBar(AppTitleBar);
    }
}