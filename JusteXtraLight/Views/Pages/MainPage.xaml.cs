namespace JustExtraLight.Views.Pages;

public sealed partial class MainPage : Page
{
    private readonly MainPageViewModel mainPageViewModel;
    public MainPage()
    {
        InitializeComponent();
        mainPageViewModel = new MainPageViewModel();
        DataContext = mainPageViewModel;
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

        mainPageViewModel.TempFolder = currentInstancetempFolder;
    }
}