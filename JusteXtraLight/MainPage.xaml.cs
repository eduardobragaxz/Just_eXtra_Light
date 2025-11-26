namespace JustExtraLight;

public sealed partial class MainPage : Page
{
    readonly MainPageViewModel? viewModel;
    public MainPage()
    {
        InitializeComponent();
        viewModel = new();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        App.MWindow!.SetTitleBar(AppTitleBar);
        await LoadPage();
    }

    private async Task LoadPage()
    {
        StorageFolder temporaryFolder = Microsoft.Windows.Storage.ApplicationData.GetDefault().TemporaryFolder;
        IReadOnlyList<StorageFolder> folders = await temporaryFolder.GetFoldersAsync();
        StorageFolder currentInstancetempFolder = await temporaryFolder.CreateFolderAsync($"TempFolder{folders.Count}");
        viewModel!.TempFolder = currentInstancetempFolder;
    }
}