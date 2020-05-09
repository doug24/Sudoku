using System.Windows;

namespace Sudoku
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GameBoardViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();

            viewModel = new GameBoardViewModel();
            DataContext = viewModel;
        }
    }
}
