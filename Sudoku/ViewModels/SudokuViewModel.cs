using System;
using System.IO;
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

        private RelayCommand newPuzzleCommand;
        public ICommand NewPuzzleCommand
        {
            get
            {
                if (newPuzzleCommand == null)
                {
                    newPuzzleCommand = new RelayCommand(
                        p => GameBoard.NewPuzzle()
                        );
                }
                return newPuzzleCommand;
            }
        }

        private RelayCommand saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (saveCommand == null)
                {
                    saveCommand = new RelayCommand(
                        p => Save(),
                        q => GameBoard.IsInProgress
                        );
                }
                return saveCommand;
            }
        }

        private RelayCommand restoreCommand;
        public ICommand RestoreCommand
        {
            get
            {
                if (restoreCommand == null)
                {
                    restoreCommand = new RelayCommand(
                        p => Restore(),
                        q => HasSessionFile()
                        );
                }
                return restoreCommand;
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
                case Key.S:
                    if (ctrl && GameBoard.IsInProgress)
                        Save();
                    break;
                case Key.Y:
                    if (ctrl)
                        GameBoard.Redo();
                    break;
                case Key.Z:
                    if (ctrl)
                        GameBoard.Undo();
                    break;
            }
            e.Handled = true;
        }

        private bool HasSessionFile()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return File.Exists(Path.Combine(path, "session.sdx"));
        }

        private void Save()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var file = Path.Combine(path, "session.sdx");
            string state = GameBoard.ToSdxString();
            File.WriteAllText(file, state);
        }

        private void Restore()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var file = Path.Combine(path, "session.sdx");
            string[] sdxData = File.ReadAllLines(file);
            GameBoard.Restore(sdxData);
        }
    }
}
