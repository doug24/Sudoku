using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QQWingLib;

namespace Sudoku
{
    public class GameBoardViewModel : ViewModelBase
    {
        private readonly Stack<List<CellState>> moves = new Stack<List<CellState>>();

        private readonly ObservableCollection<CellViewModel> list = new ObservableCollection<CellViewModel>();
        public MultiSelectCollectionView<CellViewModel> Cells { get; private set; }

        private readonly Dictionary<int, List<CellViewModel>> rows;
        private readonly Dictionary<int, List<CellViewModel>> cols;
        private readonly Dictionary<int, List<CellViewModel>> sqrs;
        private readonly List<CellViewModel> allCells;

        public GameBoardViewModel()
        {
            Cells = new MultiSelectCollectionView<CellViewModel>(list);
            Cells.SelectionChanged += Cells_SelectionChanged;

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    list.Add(new CellViewModel(row, col, GetSquare(row, col)));
                }
            }

            rows = list.GroupBy(cell => cell.Row)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Col).ToList());

            cols = list.GroupBy(cell => cell.Col)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Row).ToList());

            sqrs = list.GroupBy(cell => cell.Square)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Row).ThenBy(c => c.Col).ToList());

            allCells = list
                .OrderBy(cell => cell.Row)
                .ThenBy(cell => cell.Col)
                .ToList();
        }

        internal void KeyDown(int value)
        {
            bool ctl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

            List<CellState> list = new List<CellState>();

            foreach (var cell in Cells.SelectedItems)
            {
                int cellIndex = QQWing.RowColumnToCell(cell.Row, cell.Col);

                var oldState = GetCurrentCellState(cellIndex);
                CellState newState = null;
                if (alt)
                {
                    if (oldState.HasCandidate(value))
                        newState = oldState.RemoveCandidate(value);
                    else
                        newState = oldState.AddCandidate(value);
                }
                if (!ctl && !alt)
                {
                    if (oldState.HasValue(value))
                        newState = oldState.UnsetValue();
                    else
                        newState = oldState.SetValue(value);
                }

                if (newState != null && newState != oldState)
                {
                    cell.SetState(newState);
                    list.Add(newState);
                }
            }

            if (list.Count > 0)
            {
                moves.Push(list);
            }
        }

        private CellState GetCurrentCellState(int cellIndex)
        {
            foreach (var move in moves)
            {
                foreach (var state in move)
                {
                    if (state.CellIndex == cellIndex)
                    {
                        return state;
                    }
                }
            }
            return null;
        }

        private void Cells_SelectionChanged(object sender, EventArgs e)
        {
            ResetCellBackground();

            if (Cells.SelectedItems.Count == 1)
            {
                var cell = Cells.SelectedItems.First();
                if (rows.TryGetValue(cell.Row, out List<CellViewModel> cellsInRow))
                {
                    foreach (var c in cellsInRow)
                        c.Background = Brushes.LightYellow;
                }
                if (cols.TryGetValue(cell.Col, out List<CellViewModel> cellsInCol))
                {
                    foreach (var c in cellsInCol)
                        c.Background = Brushes.LightYellow;
                }
                if (sqrs.TryGetValue(cell.Square, out List<CellViewModel> cellsInSqr))
                {
                    foreach (var c in cellsInSqr)
                        c.Background = Brushes.LightYellow;
                }
            }
        }

        private void ResetCellBackground()
        {
            foreach (var c in allCells)
            {
                c.Background = Brushes.White;
            }
        }

        private RelayCommand showCountCommand;
        public ICommand ShowCountCommand
        {
            get
            {
                if (showCountCommand == null)
                {
                    showCountCommand = new RelayCommand(
                        p => MessageBox.Show($"{Cells.SelectedItems.Count} items selected")
                        );
                }
                return showCountCommand;
            }
        }

        private int GetSquare(int row, int col)
        {
            int cell = QQWing.RowColumnToCell(row, col);
            return QQWing.CellToSection(cell);
        }

        internal async void NewPuzzle()
        {
            Puzzle puz = new Puzzle();
            await puz.Generate();

            if (puz.Initial.Length == allCells.Count)
            {
                moves.Clear();
                List<CellState> list = new List<CellState>();

                int idx = 0;
                foreach (var cell in allCells)
                {
                    int given = puz.Initial[idx] - '0';
                    int answer = puz.Solution[idx] - '0';

                    var cellState = new CellState(idx, given > 0, Math.Max(0, given));
                    cell.Initialize(cellState, answer);

                    list.Add(cellState);

                    idx++;
                }

                moves.Push(list);
            }
        }
    }
}
