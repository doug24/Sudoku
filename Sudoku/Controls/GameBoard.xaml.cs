using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Sudoku
{
    /// <summary>
    /// Interaction logic for GameBoard.xaml
    /// </summary>
    public partial class GameBoard : UserControl
    {
        public GameBoard()
        {
            InitializeComponent();

            gameListBox.PreviewMouseDown += GameListBox_PreviewMouseDown;
            gameListBox.PreviewStylusDown += GameListBox_PreviewStylusDown;
        }

        private void GameListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            CellViewModel? cell = GetCellAtPoint(e.GetPosition(gameListBox));
            if (DataContext is GameBoardViewModel viewModel && cell != null)
            {
                viewModel.CellMouseDown(cell, e);
            }
        }

        private CellViewModel? GetCellAtPoint(Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(gameListBox, point);
            DependencyObject obj = result.VisualHit;

            while (VisualTreeHelper.GetParent(obj) != null && obj is not CellControl)
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            // Will return null if not found
            return (obj as FrameworkElement)?.DataContext as CellViewModel;
        }


        // ListBox selection events are inconsistent from touchscreen, convert the
        // touch events to mouse left button down events
        // https://stackoverflow.com/questions/50488973/wpf-listviewitem-events-not-firing-properly-on-touchscreen
        private void GameListBox_PreviewStylusDown(object sender, StylusDownEventArgs e)
        {
            MouseButtonEventArgs arg = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = PreviewMouseLeftButtonDownEvent
            };
            gameListBox.RaiseEvent(arg);
            e.Handled = true;
        }
    }
}
