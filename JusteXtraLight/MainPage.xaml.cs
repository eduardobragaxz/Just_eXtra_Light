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
using Microsoft.Windows.AppNotifications;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.ApplicationModel.Resources;
using MApplicationData = Microsoft.Windows.Storage.ApplicationData;

namespace JustExtraLight;

public sealed partial class MainPage : Page
{
    bool canDropImages;
    int countOfImagesToConvert;
    StorageFolder? programFolder;
    ProcessStartInfo? processStart;
    int countOfAlreadyConvertedImages;
    readonly StorageFolder localFolder;
    readonly ResourceLoader resourceLoader;
    readonly ObservableCollection<ImageInfo> images;
    readonly ObservableCollection<ImageInfo> imagesNotConverted;
    public MainPage()
    {
        InitializeComponent();

        images = [];
        canDropImages = true;
        resourceLoader = new();
        imagesNotConverted = [];
        localFolder = MApplicationData.GetDefault().LocalFolder;
    }
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        StorageFolder locationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
        StorageFolder assetsFolder = await locationFolder.GetFolderAsync("Assets");
        programFolder = await assetsFolder.GetFolderAsync("Program");

        processStart = new($@"{programFolder.Path}\cjxl.exe")
        {
            CreateNoWindow = true
        };

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

        MainWindow? mainWindow = (MainWindow?)((App)Microsoft.UI.Xaml.Application.Current).MWindow;

        if (mainWindow is not null)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

            folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;

            StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();

            if (storageFolder is not null)
            {
                ViewErrorImagesButton.Visibility = Visibility.Collapsed;
                imagesNotConverted.Clear();
                LoadingRing.IsActive = true;
                countOfAlreadyConvertedImages = 0;
                countOfImagesToConvert = 0;
                await ConvertFolderImages(storageFolder);
                if (imagesNotConverted.Count != 0)
                {
                    ViewErrorImagesButton.Visibility = Visibility.Visible;
                }
                LoadingRing.IsActive = false;
                SendPostConversionNotification();
                await ShowPostConversionMessage();
            }

            LoadingRing.IsActive = false;
            EnableOrDisableControls(true);
        }

        void EnableOrDisableControls(bool enable)
        {
            ArgumentsTextBox.IsEnabled =
                IncludeSubFoldersCheckBox.IsEnabled =
                ConvertFolderButton.IsEnabled = enable;
        }
    }

    private async Task ConvertFolderImages(StorageFolder folder)
    {
        if (programFolder is not null)
        {
            if (ArgumentsTextBox.Text == "" || (ArgumentsTextBox.Text != "" && ArgumentsTextBox.Text.Length > 1 && ArgumentsTextBox.Text[0..2] == "--"))
            {
                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

                if (IncludeSubFoldersCheckBox.IsChecked is not null and true)
                {
                    await CheckInnerFolders();
                }

                if (files.Count != 0)
                {
                    StorageFolder jxlFolder = await CreateJXLFolder();

                    string finalParametersString = $"{ArgumentsTextBox.Text} ";

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
                                        countOfImagesToConvert++;
                                        string newName = FixFileName(file.DisplayName);
                                        StorageFile newFile = await file.CopyAsync(localFolder, newName, NameCollisionOption.ReplaceExisting);

                                        string arguments = $@"{finalParametersString}{localFolder.Path}\{newFile.Name} {conversionsFolder.Path}\{newName}.jxl";

                                        processStart!.Arguments = arguments;

                                        using Process? process = Process.Start(processStart);
                                        if (process is not null)
                                        {
                                            process.WaitForExit();

                                            if (process.ExitCode == 0)
                                            {
                                                StorageFile jxlFile = await conversionsFolder.GetFileAsync($"{newName}.jxl");
                                                await jxlFile.MoveAsync(jxlFolder, $"{newName}.jxl", NameCollisionOption.GenerateUniqueName);
                                                countOfAlreadyConvertedImages++;
                                            }
                                            else
                                            {
                                                this.DispatcherQueue.TryEnqueue(() =>
                                                {
                                                    BitmapImage bitmapImage = new()
                                                    {
                                                        DecodePixelHeight = 200,
                                                        DecodePixelType = DecodePixelType.Logical,
                                                        DecodePixelWidth = 200,
                                                        UriSource = new(file.Path)
                                                    };
                                                    ImageInfo imageInfo = new(file, bitmapImage)
                                                    {
                                                        ConversionSuccessful = false,
                                                        ConversionFinished = true,
                                                        ImageBorderThickness = new(2),
                                                        ShowDeleteButton = false,
                                                        StatusFontIcon = "\uEA39",
                                                        StatusSolidColorBrush = new(Colors.IndianRed)
                                                    };

                                                    imagesNotConverted.Add(imageInfo);
                                                });
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
            }
            else
            {
                ContentDialog malformedStringContentDialog = new()
                {
                    CloseButtonText = resourceLoader.GetString("ContentDialogCloseButton"),
                    Content = resourceLoader.GetString("MalformedStringDialogContent"),
                    DefaultButton = ContentDialogButton.Close,
                    Title = resourceLoader.GetString("MalformedStringDialogTitle"),
                    XamlRoot = XamlRoot
                };

                await malformedStringContentDialog.ShowAsync();
            }
        }

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
        }
    }

    private async Task ShowPostConversionMessage()
    {
        if (imagesNotConverted.Count == 0 && (countOfImagesToConvert == countOfAlreadyConvertedImages))
        {
            ContentDialog successContentDialog = new()
            {
                CloseButtonText = resourceLoader.GetString("ContentDialogCloseButton"),
                Content = resourceLoader.GetString("SuccessDialogContent"),
                DefaultButton = ContentDialogButton.Close,
                Title = resourceLoader.GetString("SuccessDialogTitle"),
                XamlRoot = XamlRoot
            };

            await successContentDialog.ShowAsync();
        }
        else
        {
            ContentDialog mildErrorContentDialog = new()
            {
                CloseButtonText = resourceLoader.GetString("ContentDialogCloseButton"),
                Content = resourceLoader.GetString("MildErrorDialogContent"),
                DefaultButton = ContentDialogButton.Close,
                Title = resourceLoader.GetString("MildErrorDialogTitle"),
                XamlRoot = XamlRoot
            };

            await mildErrorContentDialog.ShowAsync();
        }
    }

    private void SendPostConversionNotification()
    {
        if (AppNotificationManager.IsSupported())
        {
            AppNotificationBuilder appNotification;

            if (imagesNotConverted.Count != 0)
            {
                appNotification = new AppNotificationBuilder()
                    .AddText(resourceLoader.GetString("AppNotificationSuccessContent"));
            }
            else
            {
                appNotification = new AppNotificationBuilder()
                    .AddText(resourceLoader.GetString("AppNotificationSuccessContent"));
            }

            AppNotificationManager.Default.Show(appNotification.BuildNotification());
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
        if (programFolder is not null)
        {
            if (ArgumentsTextBox.Text == "" || (ArgumentsTextBox.Text != "" && ArgumentsTextBox.Text[0..2] == "--"))
            {
                string finalParametersString = $"{ArgumentsTextBox.Text} ";

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
                                    await Task.Run(async () =>
                                    {
                                        string newName = FixFileName(imageInfo.StorageFile.DisplayName);

                                        IReadOnlyList<StorageFile> localFolderImages = await localFolder.GetFilesAsync();

                                        int count = 0;

                                        //can't rely on automatic name collision handling because it adds spaces in the file name
                                        while (localFolderImages.Any(file => file.DisplayName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            count++;
                                        }

                                        newName += $"{count}";

                                        StorageFile newFile = await imageInfo.StorageFile.CopyAsync(localFolder, newName);
                                        string path = localFolder.Path;

                                        string arguments = $@"{finalParametersString}{path}\{newFile.Name} {conversionsFolder.Path}\{newFile.DisplayName}.jxl";

                                        processStart!.Arguments = arguments;

                                        using Process? process = Process.Start(processStart);
                                        if (process is not null)
                                        {
                                            process.WaitForExit();

                                            if (process.ExitCode == 0)
                                            {
                                                this.DispatcherQueue.TryEnqueue(() =>
                                                {
                                                    imageInfo.ConversionSuccessful = true;
                                                    imageInfo.StatusFontIcon = "\uE8FB";
                                                    imageInfo.ConversionFinished = true;
                                                    imageInfo.StatusSolidColorBrush = new(Colors.LightGreen);
                                                });
                                            }
                                            else
                                            {
                                                this.DispatcherQueue.TryEnqueue(() =>
                                                {
                                                    ConvertButton.IsEnabled = true;
                                                    imageInfo.StatusFontIcon = "\uEA39";
                                                    imageInfo.ConversionFinished = true;
                                                    imageInfo.StatusSolidColorBrush = new(Colors.IndianRed);
                                                });
                                            }

                                            this.DispatcherQueue.TryEnqueue(() =>
                                            {
                                                imageInfo.ShowDeleteButton = false;
                                                imageInfo.ImageBorderThickness = new(2);
                                            });

                                            process.Close();
                                        }
                                    });

                                        break;
                                    }
                        }
                    }
                }
            }
        }
    }

    private async void SaveImagesButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveImages();
    }

    private async Task SaveImages()
    {
        FolderPicker folderPicker = new();

        MainWindow? mainWindow = (MainWindow?)((App)Microsoft.UI.Xaml.Application.Current).MWindow;

        if (mainWindow is not null)
        {
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