using WinRT;

namespace JustExtraLight.ViewModels;

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
    public bool ConvertToJXL
    {
        get => field;
        set
        {
            if (value != field)
            {
                field = value;
                NotifyPropertyChanged();
            }
        }
    } = true;
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
    public InfoBarSeverity Severity
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
    public string? InfoBarTitle
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
    public string? InfobarMessage
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
    public bool ShowListText
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
    public StorageFolder? TempFolder { get; set; }
    public ObservableCollection<StorageFile> ImagesList { get; set; }
    private readonly FrozenSet<string> fileTypes;
    private int failCount;
    private int successCount;
    private readonly ResourceLoader resourceLoader;
    public AddImagesCommand ImagesCommand { get; }
    public AddFolderCommand FolderCommand { get; }
    public ConvertImagesCommand ConvertImagesCommand { get; }
    public SaveImagesCommand SaveImagesCommand { get; }
    public ClearImagesCommand ClearImagesCommand { get; }
    public MainPageViewModel()
    {
        ImagesCommand = new(this);
        FolderCommand = new(this);
        ConvertImagesCommand = new(this);
        SaveImagesCommand = new(this);
        ClearImagesCommand = new(this);
        resourceLoader = new();
        Arguments = "";
        ImagesList = [];
        fileTypes = FrozenSet.Create(".exr", ".gif", ".jpg", ".jpeg", ".pam", ".pgm", ".ppm", ".pfm", ".pgx", ".png", ".apng");
        ImagesList.CollectionChanged += Images_CollectionChanged;
    }
    public async Task AddFolderImages()
    {
        FolderPicker folderPicker = new(App.MWindow!.AppWindow.Id);
        PickFolderResult result = await folderPicker.PickSingleFolderAsync();

        if (result is not null)
        {
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(result.Path);
            IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

            if (files.Count != 0)
            {
                foreach (StorageFile file in files)
                {
                    if (fileTypes.Contains(file.FileType.ToLower()))
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
    public async Task AddImages()
    {
        Microsoft.Windows.Storage.Pickers.FileOpenPicker fileOpenPicker = new(App.MWindow!.AppWindow.Id);

        foreach (string type in fileTypes)
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
    private void Images_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        EnableConvertButton = EnableClearButton = ImagesList.Count != 0;
        ShowListText = ImagesList.Count == 0;
    }
    public void ImageItemsView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }
    public async void ImageItemsView_Drop(object sender, DragEventArgs e)
    {
        await DropImages(e);
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
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

                    if (ConvertToJXL == true)
                    {
                        if (fileTypes.Contains(storageFile.FileType.ToLower()))
                        {
                            if (await TryToCopyImageToTempFolder(storageFile))
                            {
                                ImagesList.Add(storageFile);
                            }
                        }
                    }
                    else
                    {
                        if (storageFile.FileType.Equals(".jxl", StringComparison.CurrentCultureIgnoreCase))
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
    }
    private async Task<bool> TryToCopyImageToTempFolder(StorageFile file)
    {
        try
        {
            string fileType = file.FileType;
            string newName = FixFileName(file.DisplayName);
            string newPath = $@"{TempFolder!.Path}\{newName}{fileType}";

            File.Copy(file.Path, newPath);
            return true;
        }
        catch (COMException)
        {
            return false;
        }

        static string FixFileName(string displayName)
        {
            StringBuilder newName = new(displayName, displayName.Length);
            _ = newName.Replace(' ', '_')
                .Replace('-', '_')
                .Replace('.', '_');

            if (newName.Length >= 150)
            {
                int difference = newName.Length - 150;
                _ = newName.Remove(149, difference);
            }

            return $"{newName}";
        }
    }
    private void TryAddImageToList(StorageFile file)
    {
        ImagesList.Add(file);
    }
    public async Task ConvertImages()
    {
        string error = "";

        if (Arguments == "" || (Arguments != "" && Arguments[0..2] == "--"))
        {
            IsConversionInProgress = true;

            EnableAddButtons = EnableConvertButton = EnableSaveButton = EnableClearButton = false;

            //string fullPath = $@"{Windows.ApplicationModel.Package.Current.InstalledPath}\Assets\Program\cjxl.exe";
            StorageFolder appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            string fullPath = ConvertToJXL
                ? $@"{appFolder.Path}\Assets\Program\cjxl.exe"
                : $@"{appFolder.Path}\Assets\Program\djxl.exe";

            ProcessStartInfo processStart = new(fullPath)
            {
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            await Task.Run(async () =>
            {
                IReadOnlyList<StorageFile> files = await TempFolder!.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    string finalArguments = ConvertToJXL
                    ? $@"{Arguments} {file.Path} {TempFolder.Path}\{file.DisplayName}.jxl"
                    : $@"{Arguments} {file.Path} {TempFolder.Path}\{file.DisplayName}.jpg";
                    processStart.Arguments = finalArguments;

                    using Process? process = Process.Start(processStart);

                    if (process is null)
                    {
                        failCount++;
                        break;
                    }

                    error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        successCount++;
                    }
                    else
                    {
                        if (error.Contains("--allow_jpeg_reconstruction 0"))
                        {
                            string newArguments = $@"--allow_jpeg_reconstruction 0 {file.Path} {TempFolder.Path}\{file.DisplayName}.jxl";
                            processStart.Arguments = newArguments;

                            using Process? newProcess = Process.Start(processStart);


                            if (newProcess is null)
                            {
                                failCount++;
                                break;
                            }

                            await newProcess.WaitForExitAsync();

                            if (newProcess.ExitCode == 0)
                            {
                                successCount++;
                            }
                            else
                            {
                                failCount++;
                            }
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                }
            });

            IsConversionInProgress = false;
            EnableSaveButton = EnableClearButton = true;
            SetInfoBarProperties();
        }

        void SetInfoBarProperties()
        {
            if (successCount == ImagesList.Count)
            {
                InfoBarTitle = resourceLoader.GetString("SuccessDialogTitle");
                InfobarMessage = resourceLoader.GetString("SuccessDialogMessage");
                Severity = InfoBarSeverity.Success;
            }
            else if (failCount == ImagesList.Count)
            {
                InfoBarTitle = resourceLoader.GetString("ConversionFailedTitle");
                InfobarMessage = resourceLoader.GetString("ConversionFailedMessage");
                Severity = InfoBarSeverity.Error;
            }
            else
            {
                InfoBarTitle = resourceLoader.GetString("MildErrorDialogTitle");
                InfobarMessage = resourceLoader.GetString("MildErrorDialogMessage");
                Severity = InfoBarSeverity.Warning;
            }

            successCount = failCount = 0;
        }
    }
    public async Task DeleteFilesAfterConversion()
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
    public async Task SaveImages()
    {
        Microsoft.Windows.Storage.Pickers.FolderPicker folderPicker = new(App.MWindow!.AppWindow.Id);
        PickFolderResult result = await folderPicker.PickSingleFolderAsync();

        if (result is not null)
        {
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(result.Path);
            IReadOnlyList<StorageFile> files = await TempFolder!.GetFilesAsync();

            foreach (StorageFile file in files)
            {
                if (ConvertToJXL == true)
                {
                    if (file.FileType == ".jxl")
                    {
                        await file.MoveAsync(storageFolder, file.Name, NameCollisionOption.GenerateUniqueName);
                    }
                }
                else
                {
                    await file.MoveAsync(storageFolder, file.Name, NameCollisionOption.GenerateUniqueName);
                }
            }

            await DeleteFilesAfterConversion();
            EnableAddButtons = true;
            EnableConvertButton = EnableSaveButton = EnableClearButton = false;
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}