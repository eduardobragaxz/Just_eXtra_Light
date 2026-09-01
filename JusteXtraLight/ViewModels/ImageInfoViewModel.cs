namespace JustExtraLight.ViewModels;

public sealed partial class ImageInfoViewModel(string originalName, string temporaryName, string temporaryPath, string originalFileType) : INotifyPropertyChanged
{
    public string OriginalName { get; } = originalName;
    public string TemporaryName { get; } = temporaryName;
    public string TemporaryPath { get; } = temporaryPath;
    public string OriginalFileType { get; } = originalFileType;
    public string ConvertedPath { get; set; } = "";
    public bool IsConversionCompleted
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
    public bool IsConversionSuccessful
    {
        get;
        set
        {
            field = value;

            if (value == true)
            {
                BorderColor = new(Microsoft.UI.Colors.LightGreen);
                Icon = "\uE73E";
            }
            else
            {
                BorderColor = new(Microsoft.UI.Colors.IndianRed);
                Icon = "\uEDAE";
            }

            NotifyPropertyChanged();
        }
    }
    public SolidColorBrush? BorderColor
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
    public string? Icon
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

    private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}