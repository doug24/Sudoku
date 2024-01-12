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

        Closing += (s, e) => viewModel.SaveSettings();
        StateChanged += (s, e) => viewModel.GameBoard.OnStateChanged(WindowState);

        //ImageGenerator.CreateSectionImages(26, 27);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e) => viewModel.KeyDown(e);
}
