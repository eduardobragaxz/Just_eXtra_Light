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
    public StorageFolder? TempFolder { get; set; }
    readonly ImmutableArray<string> types;
    public ObservableCollection<StorageFile> ImagesList { get; set; }
    int failCount;
    int successCount;
    readonly ResourceLoader resourceLoader;
    public MainPageViewModel()
    {
        resourceLoader = new();
        Arguments = "";
        ImagesList = [];
        ImagesList.CollectionChanged += Images_CollectionChanged;
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
    public async void ImageItemsView_Drop(object sender, DragEventArgs e)
    {
        await DropImages(e);
    }
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
            string fileType = file.FileType;
            string newName = FixFileName(file.DisplayName);
            string newNewName = $"{newName}{fileType}";

            await file.CopyAsync(TempFolder, newNewName);
            return true;
        }
        catch (COMException)
        {
            return false;
        }

        static string FixFileName(string displayName)
        {
            StringBuilder newName = new(displayName, displayName.Length);
            newName.Replace(' ', '_')
                .Replace('-', '_')
                .Replace('.', '_');

            if (newName.Length >= 150)
            {
                int difference = newName.Length - 150;
                newName.Remove(149, difference);
            }

            return $"{newName}";
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

            //string fullPath = $@"{Windows.ApplicationModel.Package.Current.InstalledPath}\Assets\Program\cjxl.exe";
            StorageFolder appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            string fullPath = $@"{appFolder.Path}\Assets\Program\cjxl.exe";

            ProcessStartInfo processStart = new(fullPath)
            {
                CreateNoWindow = true
            };

            await Task.Run(async () =>
            {
                IReadOnlyList<StorageFile> files = await TempFolder!.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    string finalArguments = $@"{Arguments} {file.Path} {TempFolder.Path}\{file.DisplayName}.jxl";
                    processStart.Arguments = finalArguments;

                    using Process? process = Process.Start(processStart);

                    if (process is not null)
                    {
                        process.WaitForExit();

                        if (process.ExitCode == 0)
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
        Microsoft.Windows.Storage.Pickers.FolderPicker folderPicker = new(App.MWindow!.AppWindow.Id);
        PickFolderResult result = await folderPicker.PickSingleFolderAsync();

        if (result is not null)
        {
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(result.Path);
            IReadOnlyList<StorageFile> files = await TempFolder!.GetFilesAsync();

            foreach (StorageFile file in files)
            {
                if (file.FileType == ".jxl")
                {
                    await file.MoveAsync(storageFolder);
                }
            }

            await DeleteFilesAfterConversion();
            EnableAddButtons = true;
            EnableConvertButton = EnableSaveButton = EnableClearButton = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}