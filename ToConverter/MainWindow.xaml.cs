using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace ToConverter;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Content = new MainPage();
        InitliazeWindow();
    }

    private void InitliazeWindow()
    {
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.Base };
        }

        AppWindow.SetIcon("Assets/Icons/TitleBarIco.ico");
        ExtendsContentIntoTitleBar = true;

        double scale = ((MainPage)Content).RasterizationScale;
        AppWindow.Resize(new SizeInt32((int)(1000 * scale), (int)(900 * scale)));
        OverlappedPresenter overlappedPresenter = (OverlappedPresenter)AppWindow.Presenter;

        overlappedPresenter.PreferredMinimumWidth = 1000;
        overlappedPresenter.PreferredMinimumHeight = 900;


        DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.OwnerWindowId, DisplayAreaFallback.Nearest);
        if (displayArea is not null)
        {
            PointInt32 CenteredPosition = AppWindow.Position;
            CenteredPosition.X = ((displayArea.WorkArea.Width - AppWindow.Size.Width) / 2);
            CenteredPosition.Y = ((displayArea.WorkArea.Height - AppWindow.Size.Height) / 2);
            AppWindow.Move(CenteredPosition);
        }
    }
}
