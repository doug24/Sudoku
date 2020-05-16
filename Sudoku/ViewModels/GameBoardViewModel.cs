using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QQWingLib;

namespace Sudoku
{
    public class GameBoardViewModel : ViewModelBase
    {
        private readonly Stack<List<CellState>> undoStack = new Stack<List<CellState>>();
        private readonly Stack<List<CellState>> redoStack = new Stack<List<CellState>>();

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

        public bool IsInProgress { get; private set; }

        internal string ToSnapshotString()
        {
            StringBuilder sb = new StringBuilder();
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int cellIndex = QQWing.RowColumnToCell(row, col);
                    var state = GetCurrentCellState(cellIndex);
                    if (col < 8)
                        sb.Append(state.ToSnapshotString()).Append(",");
                    else
                        sb.AppendLine(state.ToSnapshotString());
                }
            }
            return sb.ToString();
        }

        internal string ToSimpleSudokuString()
        {
            StringBuilder sb = new StringBuilder();
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int cellIndex = QQWing.RowColumnToCell(row, col);
                    var cell = allCells[cellIndex];
                    sb.Append(cell.Given ? cell.Value.ToString() : ".");
                }
                if (row < 8)
                    sb.AppendLine();
            }
            return sb.ToString();
        }

        internal void Undo()
        {
            if (undoStack.Count > 1)
            {
                Debug.WriteLine($"Undo before - undo stack count: {undoStack.Count}; redo stack count {redoStack.Count}");

                var list = undoStack.Pop();
                redoStack.Push(list);

                foreach (var c in list)
                {
                    var state = GetCurrentCellState(c.CellIndex);
                    var cell = allCells[c.CellIndex];
                    cell.SetState(state);
                    Debug.WriteLine($"Set {state}");
                }

                Debug.WriteLine($"Undo after - undo stack count: {undoStack.Count}; redo stack count {redoStack.Count}");

                IsInProgress = true;
            }
        }

        internal void Redo()
        {
            if (redoStack.Count > 0)
            {
                Debug.WriteLine($"Redo before - undo stack count: {undoStack.Count}; redo stack count {redoStack.Count}");

                var list = redoStack.Pop();
                undoStack.Push(list);

                foreach (var c in list)
                {
                    var state = GetCurrentCellState(c.CellIndex);
                    var cell = allCells[c.CellIndex];
                    cell.SetState(state);
                    Debug.WriteLine($"Set {state}");
                }

                CheckComplete();

                Debug.WriteLine($"Redo after - undo stack count: {undoStack.Count}; redo stack count {redoStack.Count}");
            }
        }

        internal void KeyDown(int value)
        {
            if (!IsInProgress) return;

            bool ctl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

            List<CellState> list = new List<CellState>();

            foreach (var cell in Cells.SelectedItems)
            {
                int cellIndex = QQWing.RowColumnToCell(cell.Row, cell.Col);

                var oldState = GetCurrentCellState(cellIndex);
                if (oldState.Given)
                {
                    continue;
                }    

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
                undoStack.Push(list);
                redoStack.Clear();
            }

            CheckComplete();
        }

        private void CheckComplete()
        {
            bool complete = true;
            foreach (var cell in allCells)
            {
                if (cell.Value != cell.Answer)
                {
                    complete = false;
                    break;
                }
            }

            if (complete)
            {
                MessageBox.Show("Solved!", "Sudoku", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                IsInProgress = false;
            }
        }

        private CellState GetCurrentCellState(int cellIndex)
        {
            foreach (var move in undoStack)
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

            foreach (var cell in Cells.SelectedItems)
            {
                //var cell = Cells.SelectedItems.First();
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

        //private RelayCommand showCountCommand;
        //public ICommand ShowCountCommand
        //{
        //    get
        //    {
        //        if (showCountCommand == null)
        //        {
        //            showCountCommand = new RelayCommand(
        //                p => MessageBox.Show($"{Cells.SelectedItems.Count} items selected")
        //                );
        //        }
        //        return showCountCommand;
        //    }
        //}

        private int GetSquare(int row, int col)
        {
            int cell = QQWing.RowColumnToCell(row, col);
            return QQWing.CellToSection(cell);
        }

        internal void Restore(string[] ssData)
        {
            ClearBoard();

            int[] initial = GetPuzzle(ssData);

            QQWing ss = new QQWing();
            ss.SetPuzzle(initial);
            ss.Solve();
            if (ss.IsSolved())
            {
                int[] solution = ss.GetSolution();
                if (ss.HasMultipleSolutions())
                    MessageBox.Show("Puzzle has multiple solutions");

                List<CellState> list = new List<CellState>();

                for (int row = 0; row < 9; row++)
                {
                    string[] parts = ssData[row].Split(',');
                    if (parts.Length == 9)
                    {
                        for (int col = 0; col < 9; col++)
                        {
                            int cellIndex = QQWing.RowColumnToCell(row, col);
                            var state = CellState.FromSnapshotString(cellIndex, parts[col]);
                            list.Add(state);
                            allCells[cellIndex].Initialize(state, solution[cellIndex]);
                        }
                    }
                }
                undoStack.Push(list);
                IsInProgress = true;
            }
        }

        private int[] GetPuzzle(string[] ssData)
        {
            if (ssData.Length != 9)
                return new int[0];

            int[] puzzle = new int[81];
            for (int row = 0; row < 9; row++)
            {
                string[] parts = ssData[row].Split(',');
                if (parts.Length != 9)
                    return new int[0];

                for (int col = 0; col < 9; col++)
                {
                    string str = parts[col];
                    if (str.Length == 1 && char.IsDigit(str[0]))
                    {
                        int cellIndex = QQWing.RowColumnToCell(row, col);
                        int given = str[0] - '0';
                        puzzle[cellIndex] = given;
                    }
                }
            }
            return puzzle;
        }

        //private int[] Clone(int[] input)
        //{
        //    int[] clone = new int[input.Length];
        //    Array.Copy(input, clone, input.Length);
        //    return clone;
        //}

        internal void OpenSimpleSudoku(string[] lines)
        {
            ClearBoard();

            if (lines.Length > 9)
            {
                var list = new List<string>();
                foreach (var text in lines)
                {
                    string line = text.Trim();
                    if (!string.IsNullOrWhiteSpace(line) &&
                        !line.StartsWith("*") &&
                        !line.StartsWith("-") &&
                        !line.StartsWith("|-") &&
                        !line.StartsWith("E") &&
                        !line.StartsWith("I"))
                    {
                        list.Add(line.Replace("|", ""));
                    }
                }
                lines = list.ToArray();
            }

            if (lines.Length != 9)
                return;

            int[] initial = new int[81];
            for (int row = 0; row < 9; row++)
            {
                string line = lines[row].Replace("X", ".");

                if (line.Length != 9)
                    return;

                for (int col = 0; col < 9; col++)
                {
                    int given = Math.Max(0, line[col] - '0');

                    int cellIndex = QQWing.RowColumnToCell(row, col);
                    initial[cellIndex] = given;
                }
            }

            QQWing ss = new QQWing();
            ss.SetPuzzle(initial);
            ss.Solve();
            if (ss.IsSolved())
            {
                int[] solution = ss.GetSolution();
                if (ss.HasMultipleSolutions())
                    MessageBox.Show("Puzzle has multiple solutions");

                int[] candidates = new int[0];// GetCandidates(initial);

                undoStack.Clear();
                List<CellState> list = new List<CellState>();

                int idx = 0;
                foreach (var cell in allCells)
                {
                    int given = initial[idx];
                    int answer = solution[idx];

                    var cellState = new CellState(idx, given > 0, Math.Max(0, given), GetCandiatesForCell(candidates, idx));
                    cell.Initialize(cellState, answer);

                    list.Add(cellState);

                    idx++;
                }

                undoStack.Push(list);
                IsInProgress = true;
            }
        }

        private void ClearBoard()
        {
            undoStack.Clear();
            redoStack.Clear();

            foreach (var cell in allCells)
            {
                cell.Reset();
            }
        }

        internal async void NewPuzzle()
        {
            ClearBoard();
            Puzzle puz = new Puzzle();
            await puz.Generate();

            if (puz.Initial.Length == allCells.Count)
            {
                int[] candidates = new int[0];// GetCandidates(puz.Initial);

                undoStack.Clear();
                List<CellState> list = new List<CellState>();

                int idx = 0;
                foreach (var cell in allCells)
                {
                    int given = puz.Initial[idx];
                    int answer = puz.Solution[idx];

                    var cellState = new CellState(idx, given > 0, Math.Max(0, given), GetCandiatesForCell(candidates, idx));
                    cell.Initialize(cellState, answer);

                    list.Add(cellState);

                    idx++;
                }

                undoStack.Push(list);
                IsInProgress = true;
            }
        }

        private int[] GetCandidates(int[] puzzle)
        {
            int[] candidates = new int[81 * 9];
            for (int cell = 0; cell < 81; cell++)
            {
                for (int idx = 0; idx < 9; idx++)
                {
                    candidates[cell * 9 + idx] = idx + 1;
                }
            }

            for (int cell = 0; cell < 81; cell++)
            {
                int given = Math.Max(0, puzzle[cell]);
                if (given > 0)
                {
                    int valIdx = given - 1;
                    int cellRow = QQWing.CellToRow(cell);
                    int cellCol = QQWing.CellToColumn(cell);
                    int cellSqr = QQWing.CellToSection(cell);

                    for (int col = 0; col < 9; col++)
                    {
                        int cellIndex = QQWing.RowColumnToCell(cellRow, col);
                        int pi = QQWing.GetPossibilityIndex(valIdx, cellIndex);
                        candidates[pi] = 0;
                    }

                    for (int row = 0; row < 9; row++)
                    {
                        int cellIndex = QQWing.RowColumnToCell(row, cellCol);
                        int pi = QQWing.GetPossibilityIndex(valIdx, cellIndex);
                        candidates[pi] = 0;
                    }

                    for (int off = 0; off < 9; off++)
                    {
                        int cellIndex = QQWing.SectionToCell(cellSqr, off);
                        int pi = QQWing.GetPossibilityIndex(valIdx, cellIndex);
                        candidates[pi] = 0;
                    }
                }
            }

            return candidates;
        }

        private int[] GetCandiatesForCell(int[] candidates, int cell)
        {
            if (candidates.Length > 0)
            {
                List<int> list = new List<int>();

                for (int idx = 0; idx < 9; idx++)
                {
                    int pi = QQWing.GetPossibilityIndex(idx, cell);
                    int num = candidates[pi];
                    if (num > 0)
                        list.Add(num);
                }
                return list.ToArray();
            }
            return new int[0];
        }
    }
}
