using System.Windows.Controls;
using System.Windows.Input;

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

            gameListBox.PreviewStylusDown += GameListBox_PreviewStylusDown;
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
