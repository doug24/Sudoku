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
        InitializeComponent();

        viewModel = new SudokuViewModel();
        DataContext = viewModel;

        Application.Current.ThemeMode = viewModel.DarkMode ? ThemeMode.Dark : ThemeMode.Light;

        Loaded += (s, e) => viewModel.AppRunning = true;
        Closing += (s, e) => viewModel.SaveSettings();
        StateChanged += (s, e) => viewModel.GameBoard.OnStateChanged(WindowState);

        //ImageGenerator.CreateSectionImages(26, 27);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        // cloak and uncloak to fix the white flash when the window is first shown
        base.OnSourceInitialized(e);
        Native.CloakWindow(this, true);
        Native.ApplyAccentColorToTitleBar(this);

        // Listen for Windows accent color and setting changes
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Native.CloakWindow(this, false);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case Native.WM_DWMCOLORIZATIONCOLORCHANGED:
                Native.ApplyAccentColorToTitleBar(this);
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
