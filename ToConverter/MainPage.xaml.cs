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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace JustExtraLight;

public sealed partial class MainPage : Page
{
    readonly StorageFolder localFolder;
    readonly ObservableCollection<ImageInfo> images;
    readonly ResourceLoader resourceLoader;
    bool canDropImages;
    public MainPage()
    {
        InitializeComponent();
        resourceLoader = new();
        canDropImages = true;
        images = [];
        localFolder = MApplicationData.GetDefault().LocalFolder;
    }
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await CreateFolders();
    }

    private async Task CreateFolders()
    {
        await localFolder.CreateFolderAsync("Conversions1", CreationCollisionOption.ReplaceExisting);
        await localFolder.CreateFolderAsync("Conversions2", CreationCollisionOption.ReplaceExisting);
    }

    private async void ConvertFolderButton_Click(object sender, RoutedEventArgs e)
    {
        LoadingRing.IsActive = true;
        EnableOrDisableControls(false);
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
        EnableOrDisableControls(true);

        void EnableOrDisableControls(bool enable)
        {
            ArgumentsTextBox.IsEnabled =
                IncludeSubFoldersCheckBox.IsEnabled =
                ConvertFolderButton.IsEnabled = enable;
        }
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

        IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

        if (files.Count != 0)
        {
            const string folderNameConst = "ConvertedImages";
            IReadOnlyList<StorageFolder> items = await folder.GetFoldersAsync();
            IEnumerable<StorageFolder> foldersSameName = items.Where(f => f.DisplayName == folderNameConst);

            int foldersCount = foldersSameName.Count();
            StringBuilder stringBuilder = new(folderNameConst);
            StorageFolder jxlFolder;

            if (foldersCount != 0)
            {
                while (items.Any(f => f.DisplayName == $"{folderNameConst}{foldersCount}"))
                {
                    foldersCount++;
                }

                jxlFolder = await folder.CreateFolderAsync($"{stringBuilder.Append($"{foldersCount}")}");
            }
            else
            {
                jxlFolder = await folder.CreateFolderAsync($"{folderNameConst}");
            }

            StorageFolder locationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder assetsFolder = await locationFolder.GetFolderAsync("Assets");
            StorageFolder programFolder = await assetsFolder.GetFolderAsync("Program");

            ProcessStartInfo processStart = new($@"{programFolder.Path}\cjxl.exe")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            string finalParametersString = ExpertsExpander.IsExpanded && ArgumentsTextBox.Text[0..2] == "--"
                ? $"{ArgumentsTextBox.Text} "
                : "";

            bool success = true;

            StorageFolder conversionsFolder = await localFolder.GetFolderAsync("Conversions1");

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
                            string newFilePath = $@"{conversionsFolder.Path}\{newFile.DisplayName}.jxl";
                            string arguments = $@"{finalParametersString}{path}\{newFile.Name} {newFilePath}";

                            processStart.Arguments = arguments;

                            using Process? process = Process.Start(processStart);
                            if (process is not null)
                            {
                                process.WaitForExit();

                                if (process.ExitCode == 0)
                                {
                                    StorageFile jxlFile = await conversionsFolder.GetFileAsync($"{newFile.DisplayName}.jxl");
                                    await jxlFile.MoveAsync(jxlFolder);
                                }
                                else if (process.ExitCode != 0)
                                {
                                    SetInfoBarTexts(false);
                                    success = false;
                                    break;
                                }

                                process.Close();
                            }

                            break;
                        }
                }
            }

            if (success)
            {
                SetInfoBarTexts(success);
            }

            foreach (StorageFile storageFile in await localFolder.GetFilesAsync())
            {
                await storageFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            foreach (StorageFile storageFile in await conversionsFolder.GetFilesAsync())
            {
                await storageFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }
    }

    private async void ConvertListOfImages_Click(object sender, RoutedEventArgs e)
    {
        LoadingRing2.IsActive = true;
        EnableOrDisableControls(false, false);
        await ConvertListOfImages();
        LoadingRing2.IsActive = false;
        EnableOrDisableControls(true, false);

        void EnableOrDisableControls(bool enable, bool enableConvertButton)
        {
            ConvertButton.IsEnabled = enableConvertButton;
            SaveImagesButton.IsEnabled =
                canDropImages = enable;
        }
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

        string finalParametersString = ExpertsExpander.IsExpanded && ArgumentsTextBox.Text[0..2] == "--"
            ? $"{ArgumentsTextBox.Text} "
            : "";

        bool success = true;

        StorageFolder conversionsFolder = await localFolder.GetFolderAsync("Conversions2");

        foreach (ImageInfo imageInfo in images)
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
                            await newFile.RenameAsync($"_{newName}");
                        }

                        string path = localFolder.Path;
                        string arguments = $@"{finalParametersString}{path}\{newFile.Name} {conversionsFolder.Path}\{newFile.DisplayName}.jxl";

                        processStart.Arguments = arguments;

                        using Process? process = Process.Start(processStart);
                        if (process is not null)
                        {
                            process.WaitForExit();

                            if (process.ExitCode != 0)
                            {
                                SetInfoBarTexts(false);
                                success = false;
                                ConvertButton.IsEnabled = true;
                                break;
                            }
                            else
                            {
                                imageInfo.ShowSuccess = true;
                                imageInfo.ShowDeleteButton = false;
                                imageInfo.Thickness = new(2);
                            }

                            process.Close();
                        }

                        break;
                    }
            }
        }

        if (success)
        {
            SetInfoBarTexts(success);
        }
    }

    private async void SaveImagesButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveImages();
        AppInfoBar.IsOpen = false;
    }

    private async Task SaveImages()
    {
        FolderPicker folderPicker = new();

        MainWindow mainWindow = (MainWindow)((App)Microsoft.UI.Xaml.Application.Current).MWindow!;
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;

        StorageFolder chosenFolder = await folderPicker.PickSingleFolderAsync();
        StorageFolder conversionsFolder = await localFolder.GetFolderAsync("Conversions2");
        IReadOnlyList<StorageFile> convertedFiles = await conversionsFolder.GetFilesAsync();
        List<StorageFile> storageFiles = [.. await chosenFolder.GetFilesAsync()];

        if (chosenFolder is not null)
        {
            foreach (StorageFile convertedFile in convertedFiles)
            {
                if (convertedFile.FileType == ".jxl")
                {
                    List<StorageFile> sameNameFiles = [.. storageFiles.Where(f => f.DisplayName == convertedFile.DisplayName)];
                    int count = sameNameFiles.Count;

                    if (count != 0)
                    {
                        while (storageFiles.Any(f => f.DisplayName == $"{convertedFile.DisplayName}{count}"))
                        {
                            count++;
                        }

                        await convertedFile.RenameAsync($"{convertedFile.DisplayName}{count}.jxl");
                    }

                    await convertedFile.MoveAsync(chosenFolder);
                }
            }
        }

        ConvertButton.IsEnabled =
            canDropImages = true;
        SaveImagesButton.IsEnabled = false;
        images.Clear();
    }

    private void PicturesView_DragOver(object sender, DragEventArgs e)
    {
        if (canDropImages)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
    }

    private async void PicturesView_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

            if (items.Count != 0)
            {
                foreach (IStorageItem storageItem in items)
                {
                    if (storageItem is StorageFile storageFile)
                    {
                        if (storageFile.ContentType.Contains("image"))
                        {
                            BitmapImage bitmapImage = new();

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
                }
            }

            if (images.Count != 0)
            {
                if (!ConvertButton.IsEnabled)
                {
                    ConvertButton.IsEnabled = true;
                }
            }
        }
    }

    private void AtachmentDataTemplateRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        ImageInfo imageInfo = (ImageInfo)((Button)sender).DataContext;

        images.Remove(imageInfo);

        ConvertButton.IsEnabled = images.Count != 0;
    }

    private void SetInfoBarTexts(bool success)
    {
        AppInfoBar.IsOpen = true;

        if (success)
        {
            AppInfoBar.Severity = InfoBarSeverity.Success;
            AppInfoBar.Title = resourceLoader.GetString("ConversionCompleteTitle");

            if (AppSelectorBar.SelectedItem == AppSelectorBar.Items[0])
            {
                AppInfoBar.Message = resourceLoader.GetString("ConversionCompleteContent");
            }
            else
            {
                AppInfoBar.Message = resourceLoader.GetString("ConversionCompleteContent2");
            }
        }
        else
        {
            AppInfoBar.Severity = InfoBarSeverity.Error;
            AppInfoBar.Title = resourceLoader.GetString("ConversionFailedTitle");
            AppInfoBar.Message = resourceLoader.GetString("ConversionFailedMessage");
        }
    }
}

public partial class ImageInfo(StorageFile storageFile, BitmapImage source) : INotifyPropertyChanged
{
    public BitmapImage Source => source;
    public StorageFile StorageFile => storageFile;
    public bool ShowSuccess
    {
        get => successful;
        set
        {
            if (successful != value)
            {
                successful = value;
                OnPropertyChanged();
            }
        }
    }
    public bool ShowDeleteButton
    {
        get => showDeleteButton;
        set
        {
            if (showDeleteButton != value)
            {
                showDeleteButton = value;
                OnPropertyChanged();
            }
        }
    }
    private bool showDeleteButton = true;
    private bool successful;
    public Thickness Thickness
    {
        get => thickness;
        set
        {
            if (thickness != value)
            {
                thickness = value;
                OnPropertyChanged();
            }
        }
    }
    private Thickness thickness;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}