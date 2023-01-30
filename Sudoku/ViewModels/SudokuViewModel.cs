using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using QQWingLib;

namespace Sudoku
{
    public partial class SudokuViewModel : ObservableObject
    {
        private readonly static Random rand = new Random();

        public SudokuViewModel()
        {
            SectionLayout = Properties.Settings.Default.SectionLayout;

            if (Enum.TryParse(Properties.Settings.Default.PuzzleDifficulty, out Difficulty diff))
            {
                PuzzleDifficulty = diff;
            }
            if (Enum.TryParse(Properties.Settings.Default.PuzzleSymmetry, out Symmetry symm))
            {
                PuzzleSymmetry = symm;
            }

            IrregularLayout layout = new();
            LayoutMenuItems.Add(new("Classic", -1, SectionLayout == -1));
            LayoutMenuItems.Add(new("Random", 999, SectionLayout == 999));
            for (int idx = 0; idx < IrregularLayout.LayoutCount; idx++)
            {
                LayoutMenuItems.Add(new($"Irregular {idx + 1}", idx, SectionLayout == idx));
            }

            WeakReferenceMessenger.Default.Register<SectionLayoutChangedMessage>(this, (r, m) =>
            {
                int index = m.Value;
                if (m.Value == 999)
                {
                    index = rand.Next(0, IrregularLayout.LayoutCount);
                }

                SectionLayout = index;
                LayoutMenuItems.ForEach(menu => menu.IsChecked = menu.LayoutId == index);
            });
        }


        internal void SaveSettings()
        {
            Properties.Settings.Default.SectionLayout = SectionLayout;
            Properties.Settings.Default.PuzzleDifficulty = PuzzleDifficulty.ToString();
            Properties.Settings.Default.PuzzleSymmetry = PuzzleSymmetry.ToString();

            GameBoard.SaveSettings();

            Properties.Settings.Default.Save();
        }

        [ObservableProperty]
        private GameBoardViewModel gameBoard = new();

        [ObservableProperty]
        private int sectionLayout = QQWing.ClassicLayout;

        [ObservableProperty]
        private Symmetry puzzleSymmetry = Symmetry.MIRROR;

        [ObservableProperty]
        private Difficulty puzzleDifficulty = Difficulty.INTERMEDIATE;

        [ObservableProperty]
        private KeyPadMode inputMode = KeyPadMode.Pen;

        [ObservableProperty]
        private bool isEraser;

        public ObservableCollection<LayoutMenuItemViewModel> LayoutMenuItems { get; set; } = new();

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(InputMode))
            {
                IsEraser = false;
            }
            else if (e.PropertyName is nameof(IsEraser) && IsEraser)
            {
                InputMode = KeyPadMode.Pencil;
            }
            else if (e.PropertyName is nameof(SectionLayout))
            {
                GameBoard.ChangeLayout(SectionLayout);
            }

            base.OnPropertyChanged(e);
        }

        public ICommand NewPuzzleCommand => new RelayCommand(
            p => GameBoard.NewPuzzle(PuzzleDifficulty, PuzzleSymmetry));

        public ICommand NewRandomPuzzleCommand => new RelayCommand(
            p =>
            {
                int index = rand.Next(0, IrregularLayout.LayoutCount);
                WeakReferenceMessenger.Default.Send(new SectionLayoutChangedMessage(index));
                GameBoard.NewPuzzle(PuzzleDifficulty, PuzzleSymmetry);
            });

        public ICommand SnapshotCommand => new RelayCommand(
            p => Snapshot(),
            q => GameBoard.IsInProgress);

        public ICommand EnterDesignModeCommand => new RelayCommand(
            p => GameBoard.EnterDesignMode(),
            q => !GameBoard.IsDesignMode);

        public ICommand ExitDesignModeCommand => new RelayCommand(
            p => GameBoard.ExitDesignMode(),
            q => GameBoard.IsDesignMode);

        public ICommand ClearBoardCommand => new RelayCommand(
            p => GameBoard.ClearBoard(),
            q => GameBoard.IsDesignMode);

        public ICommand NumberKeyCommand => new RelayCommand(
            p => NumberKey(p));

        public ICommand ColorKeyCommand => new RelayCommand(
            p => ColorKey(p));

        public ICommand OpenFileCommand => new RelayCommand(
            p => OpenFile());

        public ICommand SaveAsCommand => new RelayCommand(
            p => SaveAs());

        public ICommand RestoreCommand => new RelayCommand(
            p => Restore(),
            q => HasSessionFile());

        public ICommand UndoCommand => new RelayCommand(
            p => GameBoard.Undo(),
            q => GameBoard.CanUndo);

        public ICommand RedoCommand => new RelayCommand(
            p => GameBoard.Redo(),
            q => GameBoard.CanRedo);

        public ICommand ClearCommand => new RelayCommand(
            p => GameBoard.ClearColors());

        private void NumberKey(object p)
        {
            if (p is string num && int.TryParse(num, out int value))
            {
                GameBoard.KeyDown(value, InputMode);
            }
        }

        private void ColorKey(object p)
        {
            if (p is Brush br)
            {
                GameBoard.SetColor(br, InputMode);
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
            OpenFileDialog dlg = new()
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
            SaveFileDialog dlg = new()
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

        private static bool HasSessionFile()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return File.Exists(Path.Combine(path, "session.sudoku"));
        }
    }
}
