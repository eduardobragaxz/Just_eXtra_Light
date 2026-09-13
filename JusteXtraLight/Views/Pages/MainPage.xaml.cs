using Microsoft.UI;
using Windows.UI;

namespace JustExtraLight.Views.Pages;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        App.MWindow!.SetTitleBar(AppTitleBar);
        //Can't do this while targeting the current min windows version
        //AppVersionRun.Text = $"{AppInfo.Current.Package.Id.Version.Major}.{AppInfo.Current.Package.Id.Version.Minor}.{AppInfo.Current.Package.Id.Version.Build}";
        AppVersionRun.Text = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";
        await CreateFolder();
    }

    private async Task CreateFolder()
    {
        StorageFolder temporaryFolder = Microsoft.Windows.Storage.ApplicationData.GetDefault().TemporaryFolder;
        IReadOnlyList<StorageFolder> folders = await temporaryFolder.GetFoldersAsync();
        StorageFolder currentInstancetempFolder = await temporaryFolder.CreateFolderAsync($"TempFolder{folders.Count}", CreationCollisionOption.GenerateUniqueName);

        viewModel.TempFolder = currentInstancetempFolder;
    }

    private void Page_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (ActualTheme == ElementTheme.Light)
        {
            App.MWindow!.AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.LightSkyBlue;
        }
        else
        {
            App.MWindow!.AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.MidnightBlue;
            App.MWindow!.AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.LightSkyBlue;
            App.MWindow!.AppWindow.TitleBar.InactiveBackgroundColor = Colors.Transparent;
        }
    }
}