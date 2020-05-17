using System;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;

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

        private RelayCommand openFileCommand;
        public ICommand OpenFileCommand
        {
            get
            {
                if (openFileCommand == null)
                {
                    openFileCommand = new RelayCommand(
                        p => OpenFile()
                        );
                }
                return openFileCommand;
            }
        }

        private RelayCommand saveAsCommand;
        public ICommand SaveAsCommand
        {
            get
            {
                if (saveAsCommand == null)
                {
                    saveAsCommand = new RelayCommand(
                        p => SaveAs()
                        );
                }
                return saveAsCommand;
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

        private RelayCommand snapshotCommand;
        public ICommand SnapshotCommand
        {
            get
            {
                if (snapshotCommand == null)
                {
                    snapshotCommand = new RelayCommand(
                        p => Snapshot(),
                        q => GameBoard.IsInProgress
                        );
                }
                return snapshotCommand;
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

        private RelayCommand enterDesignModeCommand;
        public ICommand EnterDesignModeCommand
        {
            get
            {
                if (enterDesignModeCommand == null)
                {
                    enterDesignModeCommand = new RelayCommand(
                        p => GameBoard.EnterDesignMode(),
                        q => !GameBoard.IsDesignMode
                        );
                }
                return enterDesignModeCommand;
            }
        }

        private RelayCommand exitDesignModeCommand;
        public ICommand ExitDesignModeCommand
        {
            get
            {
                if (exitDesignModeCommand == null)
                {
                    exitDesignModeCommand = new RelayCommand(
                        p => GameBoard.ExitDesignMode(),
                        q => GameBoard.IsDesignMode
                        );
                }
                return exitDesignModeCommand;
            }
        }

        private RelayCommand clearBoardCommand;
        public ICommand ClearBoardCommand
        {
            get
            {
                if (clearBoardCommand == null)
                {
                    clearBoardCommand = new RelayCommand(
                        p => GameBoard.ClearBoard(),
                        q => GameBoard.IsDesignMode
                        );
                }
                return clearBoardCommand;
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
                        Snapshot();
                    break;
                case Key.Y:
                    if (ctrl)
                        GameBoard.Redo();
                    break;
                case Key.Z:
                    if (ctrl)
                        GameBoard.Undo();
                    break;
                case Key.Delete:
                    GameBoard.Clear();
                    break;
            }
            e.Handled = true;
        }

        private void OpenFile()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ".ss",
                Filter = "Sudoku Files (.ss)|*.ss"
            };
            var result = dlg.ShowDialog();
            if (result == true)
            {
                string[] lines = File.ReadAllLines(dlg.FileName);
                GameBoard.OpenSimpleSudoku(lines);
            }
        }

        private void SaveAs()
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                DefaultExt = ".ss",
                Filter = "Sudoku Files (.ss)|*.ss"
            };
            var result = dlg.ShowDialog();
            if (result == true)
            {
                string puzzle = GameBoard.ToSimpleSudokuString();
                File.WriteAllText(dlg.FileName, puzzle);
            }
        }

        private bool HasSessionFile()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return File.Exists(Path.Combine(path, "session.sudoku"));
        }

        private void Snapshot()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var file = Path.Combine(path, "session.sudoku");
            string state = GameBoard.ToSnapshotString();
            File.WriteAllText(file, state);
        }

        private void Restore()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var file = Path.Combine(path, "session.sudoku");
            string[] ssData = File.ReadAllLines(file);
            GameBoard.Restore(ssData);
        }
    }
}
