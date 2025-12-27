using System;
using System.Windows;
using System.Windows.Input;

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
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Native.CloakWindow(this, false);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e) => viewModel.KeyDown(e);
}
