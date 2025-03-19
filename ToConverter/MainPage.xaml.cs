using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using MApplicationData = Microsoft.Windows.Storage.ApplicationData;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.FileProperties;
using Microsoft.Windows.ApplicationModel.Resources;

namespace ToConverter;

public sealed partial class MainPage : Page
{
    readonly StorageFolder localFolder;
    ObservableCollection<ImageInfo>? images;
    ResourceLoader resourceLoader;
    public MainPage()
    {
        InitializeComponent();
        resourceLoader = new();
        localFolder = MApplicationData.GetDefault().LocalFolder;
    }
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();

        foreach (StorageFile file in files)
        {
            await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
        }
    }
    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        MainWindow mainWindow = (MainWindow)((App)Microsoft.UI.Xaml.Application.Current).MWindow!;

        if (mainWindow.AppWindow.Size.Width > 700)
        {
            VisualStateManager.GoToState(this, "WideState", false);
        }
        else
        {
            VisualStateManager.GoToState(this, "DefaultState", false);
        }
    }

    private async void ConvertFolderButton_Click(object sender, RoutedEventArgs e)
    {
        LoadingRing.IsActive = true;
        FolderPicker folderPicker = new();

        MainWindow mainWindow = (MainWindow)((App)Microsoft.UI.Xaml.Application.Current).MWindow!;
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;

        StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();

        if (storageFolder is not null)
        {
            await ConvertFolderImages(storageFolder);
        }

        LoadingRing.IsActive = false;
        AppInfoBar.IsOpen = true;
        SetInfoBarTexts();
    }
    private async void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        LoadingRing2.IsActive = true;
        DisableControlsWhileConverting();
        await ConvertListOfImages();
        LoadingRing2.IsActive = false;
        EnableControlsAfterConverting();

        SaveImagesButton.IsEnabled = true;
        AppInfoBar.IsOpen = true;

        ConvertButton.IsEnabled = SaveImagesButton.IsEnabled = false;
        SetInfoBarTexts();
    }

    private void SetInfoBarTexts()
    {
        AppInfoBar.Title = resourceLoader.GetString("ConversionCompleteTitle");

        if (AppSelectorBar.SelectedItem == AppSelectorBar.Items[0])
        {
            AppInfoBar.Content = resourceLoader.GetString("ConversionCompleteContent");
        }
        else
        {
            AppInfoBar.Content = resourceLoader.GetString("ConversionCompleteContent2");
        }
    }

    private async void SaveImagesButton_Click(object sender, RoutedEventArgs e)
    {
        FolderPicker folderPicker = new();

        MainWindow mainWindow = (MainWindow)((App)Microsoft.UI.Xaml.Application.Current).MWindow!;
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;

        StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();
        IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();

        if (storageFolder is not null)
        {
            foreach (StorageFile file in files)
            {
                await file.MoveAsync(storageFolder);
            }
        }

        files = await localFolder.GetFilesAsync();
    }
    private async Task ConvertFolderImages(StorageFolder folder)
    {
        if (IncludeSubFoldersCheckBox.IsChecked is not null and true)
        {
            IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();

            if (folders.Count != 0)
            {
                foreach (StorageFolder storageFolder in folders)
                {
                    await ConvertFolderImages(storageFolder);
                }
            }
        }

        StorageFolder jxlFolder = await folder.CreateFolderAsync("ConvertedImages");

        IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
        int exitCode = 0;

        StorageFolder locationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
        StorageFolder assetsFolder = await locationFolder.GetFolderAsync("Assets");
        StorageFolder programFolder = await assetsFolder.GetFolderAsync("Program");

        ProcessStartInfo processStart = new($@"{programFolder.Path}\cjxl.exe")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };

        foreach (StorageFile file in files)
        {
            switch (file.FileType)
            {
                case ".exr":
                case ".gif":
                case ".jpg" or ".jpeg":
                case ".pam" or ".pgm" or ".ppm":
                case ".pfm":
                case ".pgx":
                case ".png" or "apng":
                    {
                        StorageFile newFile = await file.CopyAsync(localFolder);

                        if (newFile.Name.Contains(' '))
                        {
                            string newName = newFile.Name.Replace(' ', '_');
                            await newFile.RenameAsync(newName);
                        }

                        string path = localFolder.Path;
                        string arguments = $@"{path}\{newFile.Name} {jxlFolder.Path}\{newFile.DisplayName}.jxl";

                        processStart.Arguments = arguments;

                        using Process? process = Process.Start(processStart);
                        if (process is not null)
                        {
                            process.WaitForExit();
                            exitCode = process.ExitCode;
                            process.Close();
                        }
                        break;
                    }
            }
        }

        foreach (StorageFile storageFile in await localFolder.GetFilesAsync())
        {
            await storageFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
        }

        if (exitCode == 0)
        {

        }
    }
    private void PicturesView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }
    private async void PicturesView_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

            if (items.Count != 0)
            {
                images ??= [];

                foreach (IStorageItem storageItem in items)
                {
                    BitmapImage bitmapImage = new();

                    StorageFile storageFile = (StorageFile)storageItem;
                    StorageItemThumbnail? storageItemThumbnail = await storageFile.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 200);

                    if (storageItemThumbnail is not null)
                    {
                        bitmapImage.SetSource(storageItemThumbnail);
                        storageItemThumbnail.Dispose();
                    }
                    else
                    {
                        bitmapImage.DecodePixelHeight = 200;
                        bitmapImage.DecodePixelWidth = 200;
                        bitmapImage.UriSource = new Uri(storageFile.Path);
                    }

                    ImageInfo imageInfo = new(storageFile, bitmapImage);

                    images.Add(imageInfo);
                }
            }

            if (PicturesView.ItemsSource != images)
            {
                PicturesView.ItemsSource = images;
            }

            if (!ConvertButton.IsEnabled)
            {
                ConvertButton.IsEnabled = true;
            }
        }
    }
    private void AtachmentDataTemplateRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        ImageInfo imageInfo = (ImageInfo)((Button)sender).DataContext;

        images!.Remove(imageInfo);
    }

    private async Task ConvertListOfImages()
    {
        StorageFolder locationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
        StorageFolder assetsFolder = await locationFolder.GetFolderAsync("Assets");
        StorageFolder programFolder = await assetsFolder.GetFolderAsync("Program");

        ProcessStartInfo processStart = new($@"{programFolder.Path}\cjxl.exe")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };

        foreach (ImageInfo imageInfo in images!)
        {
            switch (imageInfo.StorageFile.FileType)
            {
                case ".exr":
                case ".gif":
                case ".jpg" or ".jpeg":
                case ".pam" or ".pgm" or ".ppm":
                case ".pfm":
                case ".pgx":
                case ".png" or "apng":
                    {
                        StorageFile newFile = await imageInfo.StorageFile.CopyAsync(localFolder);

                        if (newFile.Name.Contains(' '))
                        {
                            string newName = newFile.Name.Replace(' ', '_');
                            await newFile.RenameAsync(newName);
                        }

                        string path = localFolder.Path;
                        string arguments = $@"{path}\{newFile.Name} {localFolder.Path}\{newFile.DisplayName}.jxl";

                        processStart.Arguments = arguments;

                        using Process? process = Process.Start(processStart);
                        if (process is not null)
                        {
                            process.WaitForExit();
                            process.Close();
                        }

                        await newFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                        break;
                    }
            }
        }

        images.Clear();
    }

    private void DisableControlsWhileConverting()
    {
        IncludeSubFoldersCheckBox.IsEnabled =
            ConvertFolderButton.IsEnabled =
            ConvertButton.IsEnabled =
            PicturesView.IsEnabled = false;
    }

    private void EnableControlsAfterConverting()
    {
        IncludeSubFoldersCheckBox.IsEnabled =
            ConvertFolderButton.IsEnabled =
            ConvertButton.IsEnabled =
            PicturesView.IsEnabled = true;
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem == sender.Items[0])
        {
            SelectFolderStackPanel.Visibility = Visibility.Visible;
            DragAndDropGrid.Visibility = Visibility.Collapsed;
        }
        else
        {
            DragAndDropGrid.Visibility = Visibility.Visible;
            SelectFolderStackPanel.Visibility = Visibility.Collapsed;
        }
    }
}

public class ImageInfo(StorageFile storageFile, BitmapImage source)
{
    public BitmapImage Source => source;
    public StorageFile StorageFile => storageFile;
}