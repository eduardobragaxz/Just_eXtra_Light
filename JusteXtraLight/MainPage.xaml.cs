using System;
using System.Linq;
using System.Text;
using Microsoft.UI;
using Windows.Storage;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Windows.Storage.FileProperties;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.CompilerServices;
using Microsoft.Windows.AppNotifications;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Windows.ApplicationModel.Resources;
using MApplicationData = Microsoft.Windows.Storage.ApplicationData;
using Microsoft.Windows.Storage.Pickers;
using System.Runtime.InteropServices;

namespace JustExtraLight;

public sealed partial class MainPage : Page
{
    bool canDropImages;
    bool conversionSuccessful;
    StorageFolder? conversionsFolder;
    StorageFolder? tempFolder;
    readonly ImmutableArray<string> types;
    readonly ResourceLoader resourceLoader;
    readonly ObservableCollection<string> images;
    public MainPage()
    {
        InitializeComponent();

        images = [];
        images.CollectionChanged += Images_CollectionChanged;
        canDropImages = true;
        resourceLoader = new();
        conversionSuccessful = true;
        types = ImmutableArray.Create(".exr", ".gif", ".jpg", ".jpeg", ".pam", ".pgm", ".ppm", ".pfm", ".pgx", ".png", ".apng");
    }

    private void Images_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        ClearListButton.IsEnabled = ConvertButton.IsEnabled = images.Count != 0;
    }
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        App.MWindow!.SetTitleBar(AppTitleBar);
        StorageFolder storageFolder = Microsoft.Windows.Storage.ApplicationData.GetDefault().TemporaryFolder;
        IReadOnlyList<StorageFolder> folders = await storageFolder.GetFoldersAsync();
        tempFolder = await storageFolder.CreateFolderAsync($"TempStorage{folders.Count}");
    }
    private async void AddFolderButton_Click(object sender, RoutedEventArgs e)
    {
        await AddFolderImages();
    }
    private async Task AddFolderImages()
    {
        Microsoft.Windows.Storage.Pickers.FolderPicker folderPicker = new(App.MWindow!.AppWindow.Id);
        PickFolderResult result = await folderPicker.PickSingleFolderAsync();

        if (result is not null)
        {
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(result.Path);
            IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

            if (files.Count != 0)
            {
                foreach (StorageFile file in files)
                {
                    if (types.Contains(file.FileType))
                    {
                        if (await TryToCopyImageToTempFolder(file))
                        {
                            TryAddImageToList(file);
                        }
                    }
                }
            }
        }
    }
    private async void AddImagesButton_Click(object sender, RoutedEventArgs e)
    {
        await AddImages();
    }
    private async Task AddImages()
    {
        Microsoft.Windows.Storage.Pickers.FileOpenPicker fileOpenPicker = new(App.MWindow!.AppWindow.Id);

        foreach (string type in types)
        {
            fileOpenPicker.FileTypeFilter.Add(type);
        }

        IReadOnlyList<PickFileResult> results = await fileOpenPicker.PickMultipleFilesAsync();

        if (results is not null && results.Count != 0)
        {
            StorageFolder storageFolder = Microsoft.Windows.Storage.ApplicationData.GetDefault().TemporaryFolder;

            foreach (PickFileResult result in results)
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(result.Path);

                if (await TryToCopyImageToTempFolder(file))
                {
                    TryAddImageToList(file);
                }
            }
        }
    }
    private async Task<bool> TryToCopyImageToTempFolder(StorageFile file)
    {
        try
        {
            await file.CopyAsync(tempFolder);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }
    private void TryAddImageToList(StorageFile file)
    {
        images.Add(file.Path);
    }
    private async void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        await DeleteFilesAfterConversion();
    }
    private async Task DeleteFilesAfterConversion()
    {
        IReadOnlyList<StorageFile> files = await tempFolder!.GetFilesAsync();
        foreach (StorageFile file in files)
        {
            await file.DeleteAsync();
        }

        images.Clear();
    }

    private async void ClearListButton_Click(object sender, RoutedEventArgs e)
    {
        await DeleteFilesAfterConversion();
    }
}