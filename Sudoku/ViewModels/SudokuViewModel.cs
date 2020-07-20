using System;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using QQWingLib;

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

        private Symmetry puzzleSymmetry = Symmetry.RANDOM;
        public Symmetry PuzzleSymmetry
        {
            get { return puzzleSymmetry; }
            set
            {
                if (value == puzzleSymmetry)
                    return;

                puzzleSymmetry = value;
                OnPropertyChanged(nameof(PuzzleSymmetry));
            }
        }

        private Difficulty puzzleDifficulty = Difficulty.INTERMEDIATE;
        public Difficulty PuzzleDifficulty
        {
            get { return puzzleDifficulty; }
            set
            {
                if (value == puzzleDifficulty)
                    return;

                puzzleDifficulty = value;
                OnPropertyChanged(nameof(PuzzleDifficulty));
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
                        p => GameBoard.NewPuzzle(PuzzleDifficulty, PuzzleSymmetry)
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

        private RelayCommand numberKeyCommand;
        public ICommand NumberKeyCommand
        {
            get
            {
                if (numberKeyCommand == null)
                {
                    numberKeyCommand = new RelayCommand(
                        p => NumberKeyClick(p)
                        );
                }
                return numberKeyCommand;
            }
        }

        private void NumberKeyClick(object p)
        {
            if (p is string num && int.TryParse(num, out int value))
            {
                GameBoard.KeyDown(value, InputMode);
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
                    GameBoard.KeyDown(1, InputMode); break;
                case Key.D2:
                case Key.NumPad2:
                    GameBoard.KeyDown(2, InputMode); break;
                case Key.D3:
                case Key.NumPad3:
                    GameBoard.KeyDown(3, InputMode); break;
                case Key.D4:
                case Key.NumPad4:
                    GameBoard.KeyDown(4, InputMode); break;
                case Key.D5:
                case Key.NumPad5:
                    GameBoard.KeyDown(5, InputMode); break;
                case Key.D6:
                case Key.NumPad6:
                    GameBoard.KeyDown(6, InputMode); break;
                case Key.D7:
                case Key.NumPad7:
                    GameBoard.KeyDown(7, InputMode); break;
                case Key.D8:
                case Key.NumPad8:
                    GameBoard.KeyDown(8, InputMode); break;
                case Key.D9:
                case Key.NumPad9:
                    GameBoard.KeyDown(9, InputMode); break;
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
                    GameBoard.ClearColors();
                    break;
                case Key.E:
                    InputMode = KeyPadMode.Eraser;
                    break;
                case Key.Q:
                    InputMode = KeyPadMode.Pencil;
                    break;
                case Key.A:
                    InputMode = KeyPadMode.Pen;
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

        private KeyPadMode inputMode = KeyPadMode.Pen;
        public KeyPadMode InputMode
        {
            get { return inputMode; }
            set
            {
                if (value == InputMode)
                    return;

                inputMode = value;
                OnPropertyChanged(nameof(InputMode));
                IsEraser = false;
            }
        }

        private bool isEraser;
        public bool IsEraser
        {
            get { return isEraser; }
            set
            {
                if (value == isEraser)
                    return;

                isEraser = value;
                OnPropertyChanged(nameof(IsEraser));
                if (isEraser)
                    InputMode = KeyPadMode.Pencil;
            }
        }

        private RelayCommand colorKeyCommand;
        public ICommand ColorKeyCommand
        {
            get
            {
                if (colorKeyCommand == null)
                {
                    colorKeyCommand = new RelayCommand(
                        p => ColorKeyClick(p)
                        );
                }
                return colorKeyCommand;
            }
        }

        private void ColorKeyClick(object p)
        {
            if (p is Brush br)
            {
                GameBoard.SetColor(br, InputMode);
            }
        }

        private RelayCommand undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                if (undoCommand == null)
                {
                    undoCommand = new RelayCommand(
                        p => GameBoard.Undo(),
                        q => GameBoard.CanUndo
                        );
                }
                return undoCommand;
            }
        }

        private RelayCommand redoCommand;
        public ICommand RedoCommand
        {
            get
            {
                if (redoCommand == null)
                {
                    redoCommand = new RelayCommand(
                        p => GameBoard.Redo(),
                        q => GameBoard.CanRedo
                        );
                }
                return redoCommand;
            }
        }

        private RelayCommand clearCommand;
        public ICommand ClearCommand
        {
            get
            {
                if (clearCommand == null)
                {
                    clearCommand = new RelayCommand(
                        p => GameBoard.ClearColors()
                        );
                }
                return clearCommand;
            }
        }
    }
}
