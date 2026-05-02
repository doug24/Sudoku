using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Sudoku;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly SudokuViewModel viewModel;
    public MainWindow()
    {
        Application.Current.ThemeMode = Properties.Settings.Default.DarkMode ? ThemeMode.Dark : ThemeMode.Light;

        InitializeComponent();

        viewModel = new SudokuViewModel();
        DataContext = viewModel;

        Loaded += (s, e) => viewModel.AppRunning = true;
        Closing += (s, e) => viewModel.SaveSettings();
        StateChanged += (s, e) => viewModel.GameBoard.OnStateChanged(WindowState);
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SudokuViewModel.ShowKillerCalculator))
            {
                double centerX = Left + Width / 2.0;
                SizeToContent = SizeToContent.Width;
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
                {
                    var source = PresentationSource.FromVisual(this);
                    double dpiScaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                    var monitorHandle = Native.MonitorFromWindow(new WindowInteropHelper(this).Handle);
                    var workArea = Native.GetMonitorWorkArea(monitorHandle, dpiScaleX);
                    double newLeft = centerX - Width / 2.0;
                    newLeft = Math.Max(workArea.Left, Math.Min(newLeft, workArea.Right - Width));
                    Left = newLeft;
                });
            }
        };

        //ImageGenerator.CreateSectionImages(26, 27);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        // cloak and uncloak to fix the white flash when the window is first shown
        base.OnSourceInitialized(e);
        Native.CloakWindow(this, true);

        // Listen for Windows accent color and setting changes
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Apply accent color after WPF's ThemeMode has finished its DWM setup
        Native.ApplyAccentColorToTitleBar(this);
        Native.CloakWindow(this, false);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case Native.WM_DWMCOLORIZATIONCOLORCHANGED:
                // wParam contains the new ARGB colorization color
                Native.ApplyAccentColorToTitleBar(this, (int)wParam);
                break;

            case Native.WM_SETTINGCHANGE:
                string? setting = lParam != IntPtr.Zero ? Marshal.PtrToStringUni(lParam) : null;
                if (string.Equals(setting, "ImmersiveColorSet", StringComparison.Ordinal))
                {
                    Native.ApplyAccentColorToTitleBar(this);
                }
                break;
        }

        return IntPtr.Zero;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e) => viewModel.KeyDown(e);
}
