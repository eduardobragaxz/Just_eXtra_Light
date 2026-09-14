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
    public int ImagesCount
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
        get;
        set
        {
            if (value != field)
            {
                field = value;
                DeleteFilesAfterConversion();
                NotifyPropertyChanged();
            }
        }
    } = true;
    public bool AreRadioButtonsEnabled
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
    } = InfoBarSeverity.Informational;
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
    public Visibility ShowInfoBarContent
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
    } = Visibility.Visible;
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
    public ObservableCollection<ImageInfoViewModel> ImagesList { get; set; }
    public AddImagesCommand ImagesCommand { get; }
    public AddFolderCommand FolderCommand { get; }
    public ConvertImagesCommand ConvertImagesCommand { get; }
    public SaveImagesCommand SaveImagesCommand { get; }
    public ClearImagesCommand ClearImagesCommand { get; }
    private readonly FrozenSet<string> fileTypes;
    private int failCount;
    private int successCount;
    private readonly ResourceLoader resourceLoader;
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
    public async Task AddFolderPickerImages()
    {
        FolderPicker folderPicker = new(App.MWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };

        PickFolderResult result = await folderPicker.PickSingleFolderAsync();

        if (result is not null)
        {
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(result.Path);
            IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

            if (files.Count != 0)
            {
                await AddImages(files);
            }
        }
    }
    public async Task AddFilePickerImages()
    {
        FileOpenPicker fileOpenPicker = new(App.MWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };

        if (ConvertToJXL == true)
        {
            fileOpenPicker.FileTypeChoices.Add(string.Join(", ", fileTypes), [.. fileTypes.Items]);
        }
        else
        {
            fileOpenPicker.FileTypeChoices.Add(".jxl", []);
        }

        IReadOnlyList<PickFileResult> results = await fileOpenPicker.PickMultipleFilesAsync();

        if (results.Count != 0)
        {
            await AddImages(results);
        }
    }
    private void Images_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        ImagesCount = ImagesList.Count;

        if (ImagesCount != 0)
        {
            EnableConvertButton = EnableClearButton = true;
            AreRadioButtonsEnabled = ShowListText = false;
        }
        else
        {
            ShowListText = true;
            AreRadioButtonsEnabled = true;
        }
    }
    public void ImageItemsView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }
    public async void ImageItemsView_Drop(object sender, DragEventArgs e)
    {
        if (EnableAddButtons == true)
        {
            await DropImages(e);
        }
    }

    private async Task DropImages(DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

            await AddImages(items);
        }
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    private async Task AddImages(IReadOnlyList<IStorageItem> items)
    {
        foreach (IStorageItem item in items)
        {
            StorageFile storageFile = (StorageFile)item;

            if (ImagesList.FirstOrDefault(i => i.OriginalName == storageFile.DisplayName) is not null)
            {
                continue;
            }

            if (ConvertToJXL == true)
            {
                if (fileTypes.Contains(storageFile.FileType.ToLower()))
                {
                    ImageInfoViewModel imageInfo = await TryToCopyImageToTempFolder(storageFile);
                    ImagesList.Add(imageInfo);
                }
            }
            else if (storageFile.FileType.Equals(".jxl", StringComparison.CurrentCultureIgnoreCase))
            {
                ImageInfoViewModel imageInfo = await TryToCopyImageToTempFolder(storageFile);
                ImagesList.Add(imageInfo);
            }
        }
    }
    private async Task AddImages(IReadOnlyList<PickFileResult> results)
    {
        if (results is not null && results.Count != 0)
        {
            foreach (PickFileResult result in results)
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(result.Path);

                if (ImagesList.FirstOrDefault(i => i.OriginalName == file.DisplayName) is not null)
                {
                    continue;
                }

                ImageInfoViewModel imageInfo = await TryToCopyImageToTempFolder(file);
                ImagesList.Add(imageInfo);
            }
        }
    }
    private async Task<ImageInfoViewModel> TryToCopyImageToTempFolder(StorageFile file)
    {
        string originalFileType = file.FileType;
        string temporaryName = FixFileName();
        string temporaryPath = $@"{TempFolder!.Path}\{temporaryName}{originalFileType}";

        ImageInfoViewModel imageInfo = new(file.DisplayName, temporaryName, temporaryPath, originalFileType);
        File.Copy(file.Path, temporaryPath, true);
        return imageInfo;

        string FixFileName()
        {
            StringBuilder newName = new(file.DisplayName, file.DisplayName.Length);
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
    public async Task ConvertImages()
    {
        if (App.MWindow is null)
        {
            return;
        }
        if (Arguments == "" || (Arguments != "" && Arguments[0..2] == "--"))
        {
            IsConversionInProgress = true;
            EnableAddButtons =
                EnableConvertButton =
                EnableSaveButton =
                EnableClearButton = false;

            string executablePath = GetCorrectExecutablePath();
            ProcessStartInfo processStart = new(executablePath)
            {
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            await Task.Run(async () =>
            {
                foreach (ImageInfoViewModel imageInfo in ImagesList)
                {
                    imageInfo.ConvertedPath = ConvertToJXL
                    ? $@"{TempFolder!.Path}\{imageInfo.TemporaryName}.jxl"
                    : $@"{TempFolder!.Path}\{imageInfo.TemporaryName}.jpg";

                    string finalArguments = $"{Arguments} {imageInfo.TemporaryPath} {imageInfo.ConvertedPath}";
                    processStart.Arguments = finalArguments;

                    using Process? process = Process.Start(processStart);

                    if (process is null)
                    {
                        _ = (App.MWindow.DispatcherQueue!.TryEnqueue(() =>
                        {
                            imageInfo.IsConversionCompleted = true;
                            imageInfo.IsConversionSuccessful = false;
                        }));
                        failCount++;
                        continue;
                    }

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        _ = (App.MWindow.DispatcherQueue!.TryEnqueue(() =>
                        {
                            imageInfo.IsConversionSuccessful =
                                imageInfo.IsConversionCompleted = true;
                        }));
                        successCount++;
                    }
                    else
                    {
                        string error = await process.StandardError.ReadToEndAsync();

                        if (error.Contains("--allow_jpeg_reconstruction 0"))
                        {
                            string newArguments = $@"--allow_jpeg_reconstruction 0 {Arguments} {imageInfo.TemporaryPath} {TempFolder.Path}\{imageInfo.TemporaryName}.jxl";
                            processStart.Arguments = newArguments;

                            using Process? newProcess = Process.Start(processStart);

                            if (newProcess is null)
                            {
                                _ = (App.MWindow.DispatcherQueue!.TryEnqueue(() =>
                                {
                                    imageInfo.IsConversionCompleted = true;
                                    imageInfo.IsConversionSuccessful = false;

                                    imageInfo.Error = error;
                                }));
                                failCount++;
                                continue;
                            }

                            await newProcess.WaitForExitAsync();

                            if (newProcess.ExitCode == 0)
                            {
                                _ = (App.MWindow.DispatcherQueue!.TryEnqueue(() =>
                                {
                                    imageInfo.IsConversionSuccessful =
                                        imageInfo.IsConversionCompleted = true;
                                }));
                                successCount++;
                            }
                            else
                            {
                                _ = (App.MWindow.DispatcherQueue!.TryEnqueue(() =>
                                {
                                    imageInfo.IsConversionCompleted = true;
                                    imageInfo.IsConversionSuccessful = false;

                                    imageInfo.Error = error;
                                }));
                                failCount++;
                            }
                        }
                        else
                        {
                            _ = (App.MWindow.DispatcherQueue!.TryEnqueue(() =>
                            {
                                imageInfo.IsConversionCompleted = true;
                                imageInfo.IsConversionSuccessful = false;

                                imageInfo.Error = error;
                            }));
                            failCount++;
                        }
                    }
                }
            });

            IsConversionInProgress = false;
            EnableSaveButton = failCount != ImagesList.Count;
            EnableClearButton = true;
            SetInfoBarProperties();
        }

        void SetInfoBarProperties()
        {
            ShowInfoBarContent = Visibility.Collapsed;

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
        string GetCorrectExecutablePath()
        {
            StorageFolder appFolder = Package.Current.InstalledLocation;

            return Environment.Is64BitOperatingSystem
                ? ConvertToJXL
                    ? $@"{appFolder.Path}\Assets\Program\x64-windows-static\bin\cjxl.exe"
                    : $@"{appFolder.Path}\Assets\Program\x64-windows-static\bin\djxl.exe"
                : ConvertToJXL
                    ? $@"{appFolder.Path}\Assets\Program\x86-windows-static\bin\cjxl.exe"
                    : $@"{appFolder.Path}\Assets\Program\x86-windows-static\bin\djxl.exe";
        }
    }
    private async void DeleteFilesAfterConversion()
    {
        await DeleteFilesAfterConversionAsync();
    }
    public async Task DeleteFilesAfterConversionAsync()
    {
        IReadOnlyList<StorageFile> files = await TempFolder!.GetFilesAsync();

        foreach (StorageFile file in files)
        {
            File.Delete(file.Path);
        }

        ImagesList.Clear();

        InfoBarTitle = null;
        InfobarMessage = null;

        ShowInfoBarContent = Visibility.Visible;
        Severity = InfoBarSeverity.Informational;
        EnableAddButtons = true;
        EnableConvertButton =
            EnableSaveButton =
            EnableClearButton = false;
    }
    public async Task SaveImages()
    {
        FolderPicker folderPicker = new(App.MWindow!.AppWindow.Id);
        PickFolderResult result = await folderPicker.PickSingleFolderAsync();

        if (result is not null)
        {
            string pickedPath = result.Path;
            string fileType = ConvertToJXL == true ? ".jxl" : ".jpg";

            foreach (ImageInfoViewModel imageInfo in ImagesList)
            {
                if (imageInfo.IsConversionSuccessful == true)
                {
                    File.Move(imageInfo.ConvertedPath, $@"{pickedPath}\{imageInfo.OriginalName}{fileType}", false);
                }
            }

            await DeleteFilesAfterConversionAsync();
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}