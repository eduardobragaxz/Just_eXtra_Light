namespace JustExtraLight.Views.Windows;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Content = new MainPage();
        InitliazeWindow();
    }

    [DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
    private void InitliazeWindow()
    {
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.Base };
        }

        AppWindow.SetIcon("Assets/Icons/JXLIco.ico");
        ExtendsContentIntoTitleBar = true;

        uint dpi = GetDpiForWindow((nint)AppWindow.Id.Value);
        int calculatedWidth = DipToPhysical(900, dpi);
        int calculatedHeight = DipToPhysical(760, dpi);
        AppWindow.Resize(new SizeInt32(calculatedWidth, calculatedHeight));
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        OverlappedPresenter overlappedPresenter = (OverlappedPresenter)AppWindow.Presenter;
        overlappedPresenter.PreferredMinimumWidth = DipToPhysical(640, dpi);
        overlappedPresenter.PreferredMinimumHeight = DipToPhysical(600, dpi);

        DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.OwnerWindowId, DisplayAreaFallback.Nearest);
        if (displayArea is not null)
        {
            PointInt32 CenteredPosition = AppWindow.Position;
            CenteredPosition.X = (displayArea.WorkArea.Width - calculatedWidth) / 2;
            CenteredPosition.Y = (displayArea.WorkArea.Height - calculatedHeight) / 2;
            AppWindow.Move(CenteredPosition);
        }
    }

    private static int DipToPhysical(double dip, uint dpi)
    {
        double scaleFactor = (double)dpi / 96;
        return (int)(dip * scaleFactor);
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint GetDpiForWindow(nint hwnd);
}