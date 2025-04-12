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
using System.Collections.ObjectModel;
using Windows.Storage.FileProperties;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Windows.ApplicationModel.Resources;
using MApplicationData = Microsoft.Windows.Storage.ApplicationData;

namespace JustExtraLight;

public sealed partial class MainPage : Page
{
    readonly StorageFolder localFolder;
    StorageFolder? programFolder;
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
        StorageFolder locationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
        StorageFolder assetsFolder = await locationFolder.GetFolderAsync("Assets");
        programFolder = await assetsFolder.GetFolderAsync("Program");

        await DeleteFiles();
        await CreateFolders();
    }

    private async Task DeleteFiles(StorageFolder? storageFolder = null)
    {
        if (storageFolder is null)
        {
            IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();

            foreach (StorageFile file in files)
            {
                await file.DeleteAsync();
            }
        }
        else
        {
            IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

            foreach (StorageFile file in files)
            {
                await file.DeleteAsync();
            }
        }
    }

    private async Task CreateFolders()
    {
        await localFolder.CreateFolderAsync("Conversions1", CreationCollisionOption.ReplaceExisting);
        await localFolder.CreateFolderAsync("Conversions2", CreationCollisionOption.ReplaceExisting);
    }

    private async void ConvertFolderButton_Click(object sender, RoutedEventArgs e)
    {
        EnableOrDisableControls(false);
        FolderPicker folderPicker = new();

        MainWindow mainWindow = (MainWindow)((App)Microsoft.UI.Xaml.Application.Current).MWindow!;
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;

        StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();

        if (storageFolder is not null)
        {
            LoadingRing.IsActive = true;
            if (await ConvertFolderImages(storageFolder))
            {
                SetInfoBarTexts(true);
            }
            else
            {
                SetInfoBarTexts(false);
            }
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

    private async Task<bool> ConvertFolderImages(StorageFolder folder)
    {
        if (IncludeSubFoldersCheckBox.IsChecked is not null and true)
        {
            await CheckInnerFolders();
        }


        IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

        bool success = true;

        if (files.Count != 0)
        {
            StorageFolder jxlFolder = await CreateJXLFolder();

            ProcessStartInfo processStart = new($@"{programFolder!.Path}\cjxl.exe")
            {
                CreateNoWindow = true
            };

            string finalParametersString = ExpertsExpander.IsExpanded && ArgumentsTextBox.Text[0..2] == "--"
                ? $"{ArgumentsTextBox.Text} "
                : "";

            StorageFolder conversionsFolder = await localFolder.GetFolderAsync("Conversions1");

            foreach (StorageFile file in files)
            {
                await Task.Run(async () =>
                {
                    switch (file.FileType.ToLower())
                    {
                        case ".exr":
                        case ".gif":
                        case ".jpg" or ".jpeg":
                        case ".pam" or ".pgm" or ".ppm":
                        case ".pfm":
                        case ".pgx":
                        case ".png" or "apng":
                            {
                                string newName = FixFileName(file.DisplayName);
                                StorageFile newFile = await file.CopyAsync(localFolder, newName, NameCollisionOption.ReplaceExisting);

                                //string path = localFolder.Path;
                                //string newFilePath = $@"{conversionsFolder.Path}\{newFile.DisplayName}.jxl";
                                string arguments = $@"{finalParametersString}{localFolder.Path}\{newFile.Name} {conversionsFolder.Path}\{newName}.jxl";

                                processStart.Arguments = arguments;

                                using Process? process = Process.Start(processStart);
                                if (process is not null)
                                {
                                    process.WaitForExit();

                                    if (process.ExitCode == 0)
                                    {
                                        StorageFile jxlFile = await conversionsFolder.GetFileAsync($"{newName}.jxl");
                                        await jxlFile.MoveAsync(jxlFolder, $"{newName}.jxl", NameCollisionOption.GenerateUniqueName);
                                    }
                                    else
                                    {
                                        success = false;
                                    }

                                    process.Close();
                                }

                                break;
                            }
                    }
                });
            }

            await DeleteFiles();

            foreach (StorageFile storageFile in await conversionsFolder.GetFilesAsync())
            {
                await storageFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }

        return success;

        async Task CheckInnerFolders()
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
        async Task<StorageFolder> CreateJXLFolder()
        {
            const string folderNameConst = "ConvertedImages";
            return await folder.CreateFolderAsync(folderNameConst, CreationCollisionOption.GenerateUniqueName);
            //IReadOnlyList<StorageFolder> items = await folder.GetFoldersAsync();
            //IEnumerable<StorageFolder> foldersSameName = items.Where(f => f.DisplayName == folderNameConst);

            //int foldersCount = foldersSameName.Count();

            //if (foldersCount != 0)
            //{
            //    while (items.Any(f => f.DisplayName == $"{folderNameConst}{foldersCount}"))
            //    {
            //        foldersCount++;
            //    }

            //    return await folder.CreateFolderAsync($"{folderNameConst}{foldersCount}");
            //}
            //else
            //{
            //    return await folder.CreateFolderAsync($"{folderNameConst}");
            //}
        }
    }

    private async void ConvertListOfImages_Click(object sender, RoutedEventArgs e)
    {
        LoadingRing2.IsActive = true;
        ConvertButton.IsEnabled = false;
        await ConvertListOfImages();
        SaveImagesButton.IsEnabled = true;
        LoadingRing2.IsActive = false;
    }

    private async Task ConvertListOfImages()
    {
        ProcessStartInfo processStart = new($@"{programFolder!.Path}\cjxl.exe")
        {
            CreateNoWindow = true
        };

        string finalParametersString = ExpertsExpander.IsExpanded && ArgumentsTextBox.Text[0..2] == "--"
            ? $"{ArgumentsTextBox.Text} "
            : "";

        StorageFolder conversionsFolder = await localFolder.GetFolderAsync("Conversions2");

        foreach (ImageInfo imageInfo in images)
        {
            if (imageInfo.ConversionFinished == false)
            {
                switch (imageInfo.StorageFile.FileType.ToLower())
                {
                    case ".exr":
                    case ".gif":
                    case ".jpg" or ".jpeg":
                    case ".pam" or ".pgm" or ".ppm":
                    case ".pfm":
                    case ".pgx":
                    case ".png" or "apng":
                        {
                            string newName = FixFileName(imageInfo.StorageFile.DisplayName);

                            IReadOnlyList<StorageFile> localFolderImages = await localFolder.GetFilesAsync();

                            int count = 0;

                            while (localFolderImages.Any(file => file.DisplayName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                            {
                                count++;
                                newName += $"{count}";
                            }

                            StorageFile newFile = await imageInfo.StorageFile.CopyAsync(localFolder, newName);
                            string path = localFolder.Path;

                            string arguments = $@"{finalParametersString}{path}\{newFile.Name} {conversionsFolder.Path}\{newFile.DisplayName}.jxl";

                            processStart.Arguments = arguments;

                            using Process? process = Process.Start(processStart);
                            if (process is not null)
                            {
                                process.WaitForExit();

                                if (process.ExitCode == 0)
                                {
                                    imageInfo.ConversionSuccessful = true;
                                    imageInfo.StatusFontIcon = "\uE8FB";
                                    imageInfo.ConversionFinished = true;
                                    imageInfo.StatusSolidColorBrush = new(Colors.LightGreen);
                                }
                                else
                                {
                                    ConvertButton.IsEnabled = true;
                                    imageInfo.StatusFontIcon = "\uEA39";
                                    imageInfo.ConversionFinished = true;
                                    imageInfo.StatusSolidColorBrush = new(Colors.IndianRed);
                                }

                                imageInfo.ShowDeleteButton = false;
                                imageInfo.ImageBorderThickness = new(2);

                                process.Close();
                            }

                            break;
                        }
                }
            }
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

        if (chosenFolder is not null)
        {
            StorageFolder conversionsFolder = await localFolder.GetFolderAsync("Conversions2");
            IReadOnlyList<StorageFile> convertedFiles = await conversionsFolder.GetFilesAsync();

            foreach (StorageFile convertedFile in convertedFiles)
            {
                if (convertedFile.FileType == ".jxl")
                {
                    await convertedFile.MoveAsync(chosenFolder, convertedFile.Name, NameCollisionOption.GenerateUniqueName);
                }
            }

            images.Clear();
            SaveImagesButton.IsEnabled = false;
            await DeleteFiles(conversionsFolder);
        }
        else
        {
            SaveImagesButton.IsEnabled = true;
        }

        await DeleteFiles();
        canDropImages = true;
    }

    private static string FixFileName(string displayName)
    {
        StringBuilder newName = new(displayName, displayName.Length);

        if (displayName.Contains(' '))
        {
            newName.Replace(' ', '_');
        }

        if (displayName.Contains('-'))
        {
            newName.Replace('-', '_');
        }

        if (newName.Length >= 150)
        {
            int difference = newName.Length - 150;
            newName.Remove(149, difference);
        }

        while (newName[^1] == '.' || newName[^1] == '-' || newName[^1] == '_')
        {
            newName.Remove(newName.Length - 1, 1);
        }

        return $"{newName}";
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

        ConvertButton.IsEnabled = images.Any(i => i.ConversionSuccessful == false);
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

    private async void ClearListButton_Click(object sender, RoutedEventArgs e)
    {
        if (images.Count != 0)
        {
            ConvertButton.IsEnabled = SaveImagesButton.IsEnabled = false;

            images.Clear();
            StorageFolder conversionFolder2 = await localFolder.GetFolderAsync("Conversions2");
            await DeleteFiles();
            await DeleteFiles(conversionFolder2);
        }
    }
}

public partial class ImageInfo(StorageFile storageFile, BitmapImage source) : INotifyPropertyChanged
{
    private bool conversionSuccessful;
    public bool ConversionSuccessful
    {
        get => conversionSuccessful;
        set
        {
            if (conversionSuccessful != value)
            {
                conversionSuccessful = value;
                OnPropertyChanged();
            }
        }
    }
    public BitmapImage Source => source;
    public StorageFile StorageFile => storageFile;
    private bool conversionFinished;
    public bool ConversionFinished
    {
        get => conversionFinished;
        set
        {
            if (conversionFinished != value)
            {
                conversionFinished = value;
                OnPropertyChanged();
            }
        }
    }
    private bool showDeleteButton = true;
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
    private Thickness imageBorderThickness;
    public Thickness ImageBorderThickness
    {
        get => imageBorderThickness;
        set
        {
            if (imageBorderThickness != value)
            {
                imageBorderThickness = value;
                OnPropertyChanged();
            }
        }
    }
    private SolidColorBrush? statusSolidColorBrush;
    public SolidColorBrush? StatusSolidColorBrush
    {
        get => statusSolidColorBrush;
        set
        {
            if (statusSolidColorBrush != value)
            {
                statusSolidColorBrush = value;
                OnPropertyChanged();
            }
        }
    }
    private string? statusFontIcon;
    public string? StatusFontIcon
    {
        get => statusFontIcon;
        set
        {
            if (statusFontIcon != value)
            {
                statusFontIcon = value;
                OnPropertyChanged();
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}