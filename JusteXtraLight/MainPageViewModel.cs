using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace JustExtraLight;

public sealed partial class MainPageViewModel : INotifyPropertyChanged
{
    public string Arguments
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                NotifyPropertyChanged();
            }
        }
    }
    public bool IsConversionInProgress
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                NotifyPropertyChanged();
            }
        }
    }
    public bool EnableAddButtons
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                NotifyPropertyChanged();
            }
        }
    } = true;
    public bool EnableConvertButton
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                NotifyPropertyChanged();
            }
        }
    }
    public bool EnableSaveButton
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                NotifyPropertyChanged();
            }
        }
    }
    public bool EnableClearButton
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                NotifyPropertyChanged();
            }
        }
    }
    public StorageFolder? TempFolder { get; set; }
    readonly ImmutableArray<string> types;
    readonly ResourceLoader resourceLoader;
    public ObservableCollection<StorageFile> ImagesList { get; set; }

    public MainPageViewModel()
    {
        Arguments = "";
        ImagesList = [];
        ImagesList.CollectionChanged += Images_CollectionChanged;
        resourceLoader = new();
        types = ImmutableArray.Create(".exr", ".gif", ".jpg", ".jpeg", ".pam", ".pgm", ".ppm", ".pfm", ".pgx", ".png", ".apng");
    }
    private void Images_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        EnableConvertButton = EnableClearButton = ImagesList.Count != 0;
    }
    public async void AddFolderButton_Click(object sender, RoutedEventArgs e)
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
    public async void AddImagesButton_Click(object sender, RoutedEventArgs e)
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
    public void ImageItemsView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public async void ImageItemsView_Drop(object sender, DragEventArgs e)
    {
        await DropImages(e);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(StorageFile))]
    private async Task DropImages(DragEventArgs e)
    {
        if (EnableAddButtons == true)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

                foreach (IStorageItem item in items)
                {
                    StorageFile storageFile = (StorageFile)item;

                    if (types.Contains(storageFile.FileType))
                    {
                        if (await TryToCopyImageToTempFolder(storageFile))
                        {
                            ImagesList.Add(storageFile);
                        }
                    }
                }
            }
        }
    }
    private async Task<bool> TryToCopyImageToTempFolder(StorageFile file)
    {
        try
        {
            await file.CopyAsync(TempFolder);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }
    private void TryAddImageToList(StorageFile file)
    {
        ImagesList.Add(file);
    }
    public async void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        await ConvertImages();
    }
    private async Task ConvertImages()
    {
        if (Arguments == "" || Arguments != "" && Arguments[0..2] == "--")
        {
            IsConversionInProgress = true;

            EnableAddButtons = EnableConvertButton = EnableSaveButton = EnableClearButton = false;

            string fullPath = $@"{Windows.ApplicationModel.Package.Current.InstalledPath}\Assets\Program\cjxl.exe";
            ProcessStartInfo processStart = new(fullPath)
            {
                CreateNoWindow = true
            };

            StorageFolder convertedImagesFolder = await TempFolder!.CreateFolderAsync("ConvertedImages", CreationCollisionOption.ReplaceExisting);

            await Task.Run(async () =>
            {
                ImmutableArray<StorageFile> files = [.. await TempFolder.GetFilesAsync()];

                foreach (StorageFile file in files)
                {
                    string newName = FixFileName(file.DisplayName);

                    await file.RenameAsync(newName);
                    string finalArguments = $@"{Arguments} {file.Path}{file.FileType} {convertedImagesFolder.Path}\{newName}.jxl";
                    processStart.Arguments = finalArguments;

                    using Process? process = Process.Start(processStart);
                    if (process is not null)
                    {
                        process.WaitForExit();
                        process.Dispose();
                    }
                    else
                    {

                    }

                    //int count = 0;

                    ////can't rely on automatic name collision handling because it adds spaces in the file name
                    ////while (localFolderImages.Any(file => file.DisplayName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                    ////{
                    ////    count++;
                    ////}

                    //if (count != 0)
                    //{
                    //    newName += $"{count}";
                    //}
                }
            });

            IsConversionInProgress = false;
            EnableSaveButton = EnableClearButton = true;
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

            if (displayName.Contains('.'))
            {
                newName.Replace('.', '_');
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
    }
    public async void ClearListButton_Click(object sender, RoutedEventArgs e)
    {
        await DeleteFilesAfterConversion();
    }
    private async Task DeleteFilesAfterConversion()
    {
        IReadOnlyList<StorageFile> files = await TempFolder!.GetFilesAsync();
        foreach (StorageFile file in files)
        {
            await file.DeleteAsync();
        }

        EnableAddButtons = true;
        EnableConvertButton = EnableSaveButton = EnableClearButton = false;
        ImagesList.Clear();
    }
    public async void SaveImagesButton_Click(object sender, RoutedEventArgs e)
    {
        StorageFolder convertedImagesFolder = await TempFolder!.GetFolderAsync("ConvertedImages");

        Microsoft.Windows.Storage.Pickers.FolderPicker folderPicker = new(App.MWindow!.AppWindow.Id);
        PickFolderResult result = await folderPicker.PickSingleFolderAsync();

        if (result is not null)
        {
            EnableAddButtons = true;
            EnableConvertButton = EnableSaveButton = EnableClearButton = false;

            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(result.Path);
            IReadOnlyList<StorageFile> files = await convertedImagesFolder.GetFilesAsync();
            IReadOnlyList<StorageFile> filesInNewFolder = await storageFolder.GetFilesAsync();

            foreach (StorageFile file in files)
            {
                int count = 0;
                string displayName = file.DisplayName;

                foreach (StorageFile storageFile in filesInNewFolder)
                {
                    if (storageFile.DisplayName == displayName)
                    {
                        count++;
                    }
                }

                await file.MoveAsync(storageFolder, count != 0 ? $"{displayName}_{count}.jxl" : file.Name);
            }

            await DeleteFilesAfterConversion();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
