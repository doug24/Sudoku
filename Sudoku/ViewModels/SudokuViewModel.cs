using System.Windows.Input;

namespace Sudoku
{
    public class SudokuViewModel : ViewModelBase
    {
        public SudokuViewModel()
        {
        }

        private GameBoardViewModel gameBoard = new GameBoardViewModel();

        public GameBoardViewModel GameBoard
        {
            get { return gameBoard; }
            set
            {
                if (value == gameBoard)
                    return;

                gameBoard = value;
                OnPropertyChanged(nameof(GameBoard));
            }
        }

        internal void KeyDown(KeyEventArgs e)
        {
            bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            switch (key)
            {
                case Key.D1:
                case Key.NumPad1:
                    GameBoard.KeyDown(1); break;
                case Key.D2:
                case Key.NumPad2:
                    GameBoard.KeyDown(2); break;
                case Key.D3:
                case Key.NumPad3:
                    GameBoard.KeyDown(3); break;
                case Key.D4:
                case Key.NumPad4:
                    GameBoard.KeyDown(4); break;
                case Key.D5:
                case Key.NumPad5:
                    GameBoard.KeyDown(5); break;
                case Key.D6:
                case Key.NumPad6:
                    GameBoard.KeyDown(6); break;
                case Key.D7:
                case Key.NumPad7:
                    GameBoard.KeyDown(7); break;
                case Key.D8:
                case Key.NumPad8:
                    GameBoard.KeyDown(8); break;
                case Key.D9:
                case Key.NumPad9:
                    GameBoard.KeyDown(9); break;
                case Key.Z:
                    if (ctrl)
                        GameBoard.Undo();
                    break;
                case Key.Y:
                    if (ctrl)
                        GameBoard.Redo();
                    break;
            }
            e.Handled = true;
        }
    }
}
