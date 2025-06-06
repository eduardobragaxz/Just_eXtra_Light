using System;
using System.IO;
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
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.ApplicationModel.Resources;
using MApplicationData = Microsoft.Windows.Storage.ApplicationData;

namespace JustExtraLight;

public sealed partial class MainPage : Page
{
    bool canDropImages;
    MainWindow? mainWindow;
    bool conversionSuccessful;
    StorageFolder? conversionsFolder;
    readonly StorageFolder localFolder;
    readonly ImmutableArray<string> types;
    readonly ResourceLoader resourceLoader;
    readonly ObservableCollection<ImageInfo> images;
    public MainPage()
    {
        InitializeComponent();

        images = [];
        canDropImages = true;
        resourceLoader = new();
        conversionSuccessful = true;
        localFolder = MApplicationData.GetDefault().LocalFolder;
        types = ImmutableArray.Create(".exr", ".gif", ".jpg", ".jpeg", ".pam", ".pgm", ".ppm", ".pfm", ".pgx", ".png", ".apng");
    }
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        mainWindow = (MainWindow)((App)Microsoft.UI.Xaml.Application.Current).MWindow!;
        mainWindow.SetTitleBar(AppTitleBar);
        await AppNotificationManager.Default.RemoveAllAsync();

        await DeleteFiles();
        await CreateFolders();
    }

    private async Task DeleteFiles(StorageFolder? storageFolder = null)
    {
        if (storageFolder is null)
        {
            IReadOnlyList<StorageFile> files = await localFolder.GetFilesAsync();

            if (files.Count != 0)
            {
                foreach (StorageFile file in files)
                {
                    await file.DeleteAsync();
                }
            }
        }
        else
        {
            IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

            if (files.Count != 0)
            {
                foreach (StorageFile file in files)
                {
                    await file.DeleteAsync();
                }
            }
        }
    }

    private async Task CreateFolders()
    {
        IReadOnlyList<StorageFolder> localFolderImages = await localFolder.GetFoldersAsync();
        int count = 0;

        string newName = "Conversions";
        //can't rely on automatic name collision handling because it adds spaces in the file name

        while (localFolderImages.Any(file => file.DisplayName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
        {
            count++;
            newName += $"{count}";
        }

        conversionsFolder = await localFolder.CreateFolderAsync(newName);
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        FolderPicker folderPicker = new();

        nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        StorageFolder? storageFolder = await folderPicker.PickSingleFolderAsync();

        if (storageFolder is not null)
        {
            IReadOnlyList<StorageFile> items = await storageFolder.GetFilesAsync();
            await AddImages(items);
        }
    }

    private async void ChooseImages_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker fileOpenPicker = new();

        nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hWnd);

        fileOpenPicker.SuggestedStartLocation = PickerLocationId.Downloads;
        fileOpenPicker.FileTypeFilter.Add(".exr");
        fileOpenPicker.FileTypeFilter.Add(".gif");
        fileOpenPicker.FileTypeFilter.Add(".jpg");
        fileOpenPicker.FileTypeFilter.Add(".jpeg");
        fileOpenPicker.FileTypeFilter.Add(".pam");
        fileOpenPicker.FileTypeFilter.Add(".pgm");
        fileOpenPicker.FileTypeFilter.Add(".ppm");
        fileOpenPicker.FileTypeFilter.Add(".pfm");
        fileOpenPicker.FileTypeFilter.Add(".pgx");
        fileOpenPicker.FileTypeFilter.Add(".png");
        fileOpenPicker.FileTypeFilter.Add(".apng");
        fileOpenPicker.FileTypeFilter.Add(".jxl");
        IReadOnlyList<StorageFile> files = await fileOpenPicker.PickMultipleFilesAsync();
        await AddImages(files);
    }

    private async Task AddImages(IReadOnlyList<IStorageItem> items)
    {
        if (items.Count != 0)
        {
            foreach (IStorageItem storageItem in items)
            {
                if (storageItem is StorageFile storageFile)
                {
                    if ((bool)ToJpegXLOption.IsChecked!)
                    {
                        if (types.Any(fileType => fileType.Equals(storageFile.FileType, StringComparison.OrdinalIgnoreCase)))
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
                    else
                    {
                        if (storageFile.FileType.Equals(".jxl", StringComparison.OrdinalIgnoreCase))
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
        }

        if (images.Count != 0)
        {
            if (!ConvertButton.IsEnabled)
            {
                ConvertButton.IsEnabled = ClearListButton.IsEnabled = true;
                ConversionOptions.IsEnabled = false;
            }

            DragAndDropText.Visibility = Visibility.Collapsed;
        }
    }

    private async void ConvertListOfImages_Click(object sender, RoutedEventArgs e)
    {
        LoadingRing.IsActive = true;
        ConvertButton.IsEnabled = SaveImagesButton.IsEnabled = ClearListButton.IsEnabled = false;
        await ConvertListOfImages();
        SaveImagesButton.IsEnabled = ClearListButton.IsEnabled = true;
        LoadingRing.IsActive = false;

        SendPostConversionNotification();
        await ShowPostConversionMessage();
    }

    private async Task ConvertListOfImages()
    {
        StorageFolder locationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
        StorageFolder assetsFolder = await locationFolder.GetFolderAsync("Assets");
        StorageFolder programFolder = await assetsFolder.GetFolderAsync("Program");

        //convert from or to jxl
        string conversionOption = (bool)ToJpegXLOption.IsChecked! ? "cjxl.exe" : "djxl.exe";

        ProcessStartInfo processStart = new($@"{programFolder.Path}\{conversionOption}")
        {
            CreateNoWindow = true
        };

        if (ArgumentsTextBox.Text == "" || (ArgumentsTextBox.Text != "" && ArgumentsTextBox.Text[0..2] == "--"))
        {
            canDropImages = false;
            string finalParametersString = $"{ArgumentsTextBox.Text} ";

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
                        case ".png" or ".apng":
                        case ".jxl":
                            {
                                string newName = FixFileName(imageInfo.StorageFile.DisplayName);

                                IReadOnlyList<StorageFile> localFolderImages = await localFolder.GetFilesAsync();

                                int count = 0;

                                //can't rely on automatic name collision handling because it adds spaces in the file name
                                while (localFolderImages.Any(file => file.DisplayName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    count++;
                                }

                                if (count != 0)
                                {
                                    newName += $"{count}";
                                }

                                StorageFile newFile = await imageInfo.StorageFile.CopyAsync(localFolder, newName);
                                string path = localFolder.Path;

                                string arguments;

                                if ((bool)ToJpegXLOption.IsChecked!)
                                {
                                    arguments = $@"{finalParametersString}{path}\{newFile.Name} {conversionsFolder!.Path}\{newName}.jxl";
                                }
                                else
                                {
                                    arguments = $@"{finalParametersString}{path}\{newFile.Name} {conversionsFolder!.Path}\{newName}.jpg";
                                }

                                processStart.Arguments = arguments;

                                Process? process = null;

                                await Task.Run(() =>
                                    {
                                        process = Process.Start(processStart);
                                    });

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
                                        imageInfo.StatusFontIcon = "\uEA39";
                                        imageInfo.ConversionFinished = true;
                                        imageInfo.StatusSolidColorBrush = new(Colors.IndianRed);
                                        conversionSuccessful = false;
                                    }

                                    imageInfo.ShowDeleteButton = false;
                                    imageInfo.ImageBorderThickness = new(2);

                                    process.Close();
                                    process.Dispose();
                                }

                                break;
                            }
                    }
                }
            }
        }

        DisableControlsPostConversion();
    }
    static string FixFileName(string displayName)
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

    private async void SaveImagesButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveImages();
    }

    private async Task SaveImages()
    {
        FolderPicker folderPicker = new();

        if (mainWindow is not null)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

            folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;

            StorageFolder chosenFolder = await folderPicker.PickSingleFolderAsync();

            if (chosenFolder is not null)
            {
                IReadOnlyList<StorageFile> convertedFiles = await conversionsFolder!.GetFilesAsync();

                foreach (StorageFile convertedFile in convertedFiles)
                {
                    await convertedFile.MoveAsync(chosenFolder, convertedFile.Name, NameCollisionOption.GenerateUniqueName);
                }

                await PostSave(false);
            }
            else
            {
                SaveImagesButton.IsEnabled = true;
            }

            canDropImages = true;
        }
    }

    private async Task ShowPostConversionMessage()
    {
        if (conversionSuccessful)
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

            if (conversionSuccessful)
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

            await AddImages(items);
        }
    }

    private void AtachmentDataTemplateRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        ImageInfo imageInfo = (ImageInfo)((Button)sender).DataContext;

        images.Remove(imageInfo);

        ConversionOptions.IsEnabled = images.Count == 0;
        ConvertButton.IsEnabled = ClearListButton.IsEnabled = images.Any(i => i.ConversionSuccessful == false);
    }

    private async void ClearListButton_Click(object sender, RoutedEventArgs e)
    {
        await PostSave(true);
    }

    private void DisableControlsPostConversion()
    {
        ConvertButton.IsEnabled = SaveImagesButton.IsEnabled = ClearListButton.IsEnabled = AddFolderButton.IsEnabled = ChooseImagesButton.IsEnabled = false;
    }

    private async Task PostSave(bool cleanFromButton)
    {
        if (cleanFromButton)
        {
            images.Clear();
        }
        else
        {
            if (conversionSuccessful)
            {
                images.Clear();
            }
            else
            {
                List<ImageInfo> removeThese = [];

                for (int index = 0; images.Count > index; index++)
                {
                    ImageInfo imageInfo = images[index];

                    if (imageInfo.ConversionSuccessful && imageInfo.ConversionFinished)
                    {
                        removeThese.Add(imageInfo);
                    }
                }

                for (int index = 0; removeThese.Count > index; index++)
                {
                    ImageInfo imageInfo = images[index];

                    images.Remove(imageInfo);
                }

                conversionSuccessful = true;
            }
        }

        ConvertButton.IsEnabled = SaveImagesButton.IsEnabled = ClearListButton.IsEnabled = false;
        ConversionOptions.IsEnabled = AddFolderButton.IsEnabled = ChooseImagesButton.IsEnabled = canDropImages = true;
        DragAndDropText.Visibility = Visibility.Visible;
        StorageFolder conversionFolder = await localFolder.GetFolderAsync("Conversions");
        await DeleteFiles();
        await DeleteFiles(conversionFolder);
        await CreateFolders();
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