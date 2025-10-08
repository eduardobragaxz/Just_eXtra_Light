using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.Storage;
using System.Threading.Tasks;

namespace JustExtraLight;

public sealed partial class MainPage : Page
{
    readonly MainPageViewModel viewModel;
    public MainPage()
    {
        InitializeComponent();
        viewModel = new();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await PageLoaded();
    }
    private async Task PageLoaded()
    {
        App.MWindow!.SetTitleBar(AppTitleBar);
        StorageFolder storageFolder = Microsoft.Windows.Storage.ApplicationData.GetDefault().TemporaryFolder;
        IReadOnlyList<StorageFolder> folders = await storageFolder.GetFoldersAsync();
        viewModel.TempFolder = await storageFolder.CreateFolderAsync($"TempStorage{folders.Count}");
    }
}