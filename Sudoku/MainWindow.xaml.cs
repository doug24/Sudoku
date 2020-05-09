using System.Windows;

namespace Sudoku
{
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

            Loaded += (s, e) =>
            {
                viewModel.GameBoard.NewPuzzle();
            };
        }
    }
}
