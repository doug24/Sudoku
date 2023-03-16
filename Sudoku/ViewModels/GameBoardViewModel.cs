using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QQWingLib;

namespace Sudoku
{
    public partial class GameBoardViewModel : ObservableObject
    {
        private readonly Stack<List<CellState>> undoStack = new();
        private readonly Stack<List<CellState>> redoStack = new();

        private readonly ObservableCollection<CellViewModel> list = new();
        public MultiSelectCollectionView<CellViewModel> Cells { get; private set; }

        private readonly List<CellViewModel> allCells;
        private readonly Dictionary<int, List<CellViewModel>> rows;
        private readonly Dictionary<int, List<CellViewModel>> cols;
        private Dictionary<int, List<CellViewModel>> sections;

        private int currentHighlightNumber = -1;
        private bool selectedNumberChanged;

        public GameBoardViewModel()
        {
            HighlightIncorrect = Properties.Settings.Default.HighlightIncorrect;
            CleanPencilMarks = Properties.Settings.Default.CleanPencilMarks;
            if (Enum.TryParse(Properties.Settings.Default.SelectionMode, out GamePlayMode mode))
            {
                PlayMode = mode;
            }

            Cells = new MultiSelectCollectionView<CellViewModel>(list);

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    list.Add(new CellViewModel(row, col));
                }
            }

            rows = list.GroupBy(cell => cell.Row)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Col).ToList());

            cols = list.GroupBy(cell => cell.Col)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Row).ToList());

            sections = list.GroupBy(cell => cell.Section)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Row).ThenBy(c => c.Col).ToList());

            allCells = list
                .OrderBy(cell => cell.Row)
                .ThenBy(cell => cell.Col)
                .ToList();

            EnableNumberHighlight = Properties.Settings.Default.EnableNumberHighlight;
        }

        internal void SaveSettings()
        {
            Properties.Settings.Default.HighlightIncorrect = HighlightIncorrect;
            Properties.Settings.Default.CleanPencilMarks = CleanPencilMarks;
            Properties.Settings.Default.SelectionMode = PlayMode.ToString();
            Properties.Settings.Default.EnableNumberHighlight = EnableNumberHighlight;
        }

        [ObservableProperty]
        private bool highlightIncorrect = true;

        partial void OnHighlightIncorrectChanged(bool value)
        {
            if (allCells != null)
            {
                foreach (var cell in allCells)
                {
                    cell.Redraw(HighlightIncorrect);
                }
            }
        }

        [ObservableProperty]
        private bool cleanPencilMarks = true;

        [ObservableProperty]
        private bool isDesignMode;

        [ObservableProperty]
        private bool isMultiSelect = true;

        [ObservableProperty]
        private SelectionMode selectionMode = SelectionMode.Extended;

        [ObservableProperty]
        private GamePlayMode playMode = GamePlayMode.CellFirst;

        partial void OnPlayModeChanged(GamePlayMode value)
        {
            // order of setting matters
            SelectionMode = value == GamePlayMode.CellFirst ? SelectionMode.Extended : SelectionMode.Single;
            IsMultiSelect = value == GamePlayMode.CellFirst;
            if (value == GamePlayMode.CellFirst)
            {
                SelectedNumber = NumberSelection.None;
            }
        }

        [ObservableProperty]
        private NumberSelection selectedNumber = NumberSelection.None;

        partial void OnSelectedNumberChanged(NumberSelection value)
        {
            selectedNumberChanged = true;

            if (EnableNumberHighlight && PlayMode == GamePlayMode.NumbersFirst)
            {
                int number = (int)value;
                HighlightNumbers(number);
            }
            if (PlayMode == GamePlayMode.CellFirst && SelectedNumber != NumberSelection.None)
            {
                SelectedNumber = NumberSelection.None;
            }
        }

        [ObservableProperty]
        private bool enableNumberHighlight;

        partial void OnEnableNumberHighlightChanged(bool value)
        {
            int number = value ? (int)currentHighlightNumber : -1;
            HighlightNumbers(number);
        }

        public bool IsInProgress { get; private set; }

        internal string ToSnapshotString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"L{QQWing.SectionLayout.Layout}");
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int cellIndex = QQWing.RowColumnToCell(row, col);
                    var state = GetCurrentCellState(cellIndex);
                    if (col < 8)
                        sb.Append(state.ToSnapshotString()).Append(',');
                    else
                        sb.AppendLine(state.ToSnapshotString());
                }
            }
            return sb.ToString();
        }

        internal string ToSimpleSudokuString()
        {
            StringBuilder sb = new();
            if (QQWing.SectionLayout.Layout != QQWing.ClassicLayout)
            {
                sb.AppendLine($"L{QQWing.SectionLayout.Layout}");
            }
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

        public bool CanUndo => undoStack.Count > 1;
        public bool CanRedo => redoStack.Count > 0;

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
                    cell.SetState(state, HighlightIncorrect);
                    Debug.WriteLine($"Set {state}");
                }

                Debug.WriteLine($"Undo after - undo stack count: {undoStack.Count}; redo stack count {redoStack.Count}");

                HighlightNumbers(currentHighlightNumber);
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
                    cell.SetState(state, HighlightIncorrect);
                    Debug.WriteLine($"Set {state}");
                }

                HighlightNumbers(currentHighlightNumber);
                CheckComplete();

                Debug.WriteLine($"Redo after - undo stack count: {undoStack.Count}; redo stack count {redoStack.Count}");
            }
        }

        internal void HighlightNumbers(int number)
        {
            if (number > 9 || !EnableNumberHighlight || currentHighlightNumber == number)
                number = -1;

            currentHighlightNumber = number;

            foreach (CellViewModel cell in allCells)
            {
                cell.SetHighlight(number);
            }
        }

        internal void ClearColors()
        {
            foreach (var cell in allCells)
            {
                cell.ResetBackground();
            }
        }

        internal void ClearCell()
        {
            List<CellState> list = new();
            foreach (var cell in Cells.SelectedItems)
            {
                int cellIndex = QQWing.RowColumnToCell(cell.Row, cell.Col);

                if (IsDesignMode)
                {
                    cell.Reset();
                }
                else
                {
                    var oldState = GetCurrentCellState(cellIndex);
                    if (oldState.Given)
                    {
                        continue;
                    }

                    CellState newState = oldState.UnsetValue().RemoveCandidates();

                    if (newState != oldState)
                    {
                        cell.SetState(newState, HighlightIncorrect);
                        list.Add(newState);
                    }

                    cell.ResetBackground();
                }
            }

            if (IsDesignMode)
            {
                CheckInvalid();
            }
            else if (list.Count > 0)
            {
                undoStack.Push(list);
                redoStack.Clear();
            }
        }

        internal void KeyDown(int value, KeyPadMode mode)
        {
            if (!(IsInProgress || IsDesignMode)) return;

            List<CellState> list = new();
            int inkAnswerIndex = -1;

            bool allHaveSameCandidate = Cells.SelectedItems.Where(c => c.Value == 0).All(c => c.HasCandidateSet(value));

            foreach (var cell in Cells.SelectedItems)
            {
                int cellIndex = QQWing.RowColumnToCell(cell.Row, cell.Col);

                if (IsDesignMode)
                {
                    if (cell.Value == value)
                    {
                        cell.Reset();
                    }
                    else
                    {
                        cell.Initialize(new(cellIndex, true, value), value);
                    }
                }
                else
                {
                    var oldState = GetCurrentCellState(cellIndex);
                    if (oldState.Given)
                    {
                        continue;
                    }

                    CellState? newState = null;
                    if (mode == KeyPadMode.Pen)
                    {
                        if (oldState.HasValue(value))
                        {
                            newState = oldState.UnsetValue();
                        }
                        else
                        {
                            newState = oldState.SetValue(value);
                            inkAnswerIndex = cellIndex;
                        }
                    }
                    else if (mode == KeyPadMode.Pencil)
                    {
                        if (allHaveSameCandidate)
                        {
                            newState = oldState.RemoveCandidate(value);
                        }
                        else if (!oldState.HasCandidate(value))
                        {
                            newState = oldState.AddCandidate(value);
                        }
                    }

                    if (newState != null && newState != oldState)
                    {
                        cell.SetState(newState, HighlightIncorrect);
                        list.Add(newState);
                        if (EnableNumberHighlight)
                        {
                            cell.SetHighlight(currentHighlightNumber);
                        }
                    }
                }
            }

            if (IsDesignMode)
            {
                CheckInvalid();
            }
            else
            {
                if (list.Count > 0)
                {
                    undoStack.Push(list);
                    redoStack.Clear();
                }

                CheckComplete();

                if (IsInProgress && CleanPencilMarks && inkAnswerIndex > -1)
                    DoPencilCleanup(inkAnswerIndex);
            }
        }

        internal void CellMouseDown(CellViewModel cell, MouseButtonEventArgs mouseArgs)
        {
            if (!(IsInProgress || IsDesignMode)) return;

            if (PlayMode == GamePlayMode.CellFirst) return;

            int value = (int)SelectedNumber;

            if (IsDesignMode)
            {
                if (value >= 1 && value <= 9)
                {
                    if (cell.Value == value)
                    {
                        cell.Reset();
                    }
                    else
                    {
                        cell.Initialize(new(QQWing.RowColumnToCell(cell.Row, cell.Col), true, value), value);
                    }
                    CheckInvalid();
                }
                return;
            }

            List<CellState> list = new();
            int inkAnswerIndex = -1;

            if (value > 9)
            {
                cell.SetColor(value - 10);
                return;
            }

            if (value >= 1 && value <= 9)
            {
                int cellIndex = QQWing.RowColumnToCell(cell.Row, cell.Col);
                var oldState = GetCurrentCellState(cellIndex);
                if (oldState.Given)
                {
                    return;
                }

                CellState? newState = null;
                if (mouseArgs.ChangedButton == MouseButton.Left)
                {
                    if (oldState.HasValue(value))
                    {
                        newState = oldState.UnsetValue();
                    }
                    else
                    {
                        newState = oldState.SetValue(value);
                        inkAnswerIndex = cellIndex;
                    }
                }
                else if (mouseArgs.ChangedButton == MouseButton.Right)
                {
                    if (oldState.HasCandidate(value))
                    {
                        newState = oldState.RemoveCandidate(value);
                    }
                    else
                    {
                        newState = oldState.AddCandidate(value);
                    }
                }

                if (newState != null && newState != oldState)
                {
                    cell.SetState(newState, HighlightIncorrect);
                    list.Add(newState);
                    if (EnableNumberHighlight)
                    {
                        cell.SetHighlight(value);
                    }
                }

                if (list.Count > 0)
                {
                    undoStack.Push(list);
                    redoStack.Clear();
                }

                CheckComplete();

                if (IsInProgress && CleanPencilMarks && inkAnswerIndex > -1)
                    DoPencilCleanup(inkAnswerIndex);
            }
        }

        private void DoPencilCleanup(int cellIndex)
        {
            List<CellState> list = new();

            var cell = allCells[cellIndex];
            if (!cell.Given && cell.Value > 0)
            {
                if (rows.TryGetValue(cell.Row, out List<CellViewModel>? cellsInRow))
                {
                    foreach (var c in cellsInRow)
                    {
                        ClearCanditates(list, cell.Value, c);
                    }
                }
                if (cols.TryGetValue(cell.Col, out List<CellViewModel>? cellsInCol))
                {
                    foreach (var c in cellsInCol)
                    {
                        ClearCanditates(list, cell.Value, c);
                    }
                }
                if (sections.TryGetValue(cell.Section, out List<CellViewModel>? cellsInSect))
                {
                    foreach (var c in cellsInSect)
                    {
                        ClearCanditates(list, cell.Value, c);
                    }
                }
            }

            if (list.Count > 0)
            {
                undoStack.Push(list);
                redoStack.Clear();
            }
        }

        private void ClearCanditates(List<CellState> list, int value, CellViewModel cell)
        {
            int cellIndex = QQWing.RowColumnToCell(cell.Row, cell.Col);
            var oldState = GetCurrentCellState(cellIndex);
            if (oldState.HasCandidate(value))
            {
                var newState = oldState.RemoveCandidate(value);
                cell.SetState(newState, HighlightIncorrect);
                list.Add(newState);
            }
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

        private void CheckInvalid()
        {
            foreach (var cell in allCells)
            {
                if (cell.Value != 0 && !UniqueValue(cell))
                {
                    cell.Foreground = Brushes.Red;
                }
                else
                {
                    cell.Foreground = Brushes.Black;
                }
            }
        }

        private bool UniqueValue(CellViewModel cell)
        {
            if (rows.TryGetValue(cell.Row, out List<CellViewModel>? cellsInRow))
            {
                foreach (var c in cellsInRow)
                {
                    if (c != cell && c.Value > 0 && c.Value == cell.Value)
                    {
                        return false;
                    }
                }
            }
            if (cols.TryGetValue(cell.Col, out List<CellViewModel>? cellsInCol))
            {
                foreach (var c in cellsInCol)
                {
                    if (c != cell && c.Value > 0 && c.Value == cell.Value)
                    {
                        return false;
                    }
                }
            }
            if (sections.TryGetValue(cell.Section, out List<CellViewModel>? cellsInSect))
            {
                foreach (var c in cellsInSect)
                {
                    if (c != cell && c.Value > 0 && c.Value == cell.Value)
                    {
                        return false;
                    }
                }
            }
            return true;
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
            return CellState.Empty;
        }

        internal void SetColor(int brushIndex)
        {
            Brush br = CellViewModel.GetColor(brushIndex);

            bool allSame = !Cells.SelectedItems.Any(c => c.Background != br);

            foreach (var cell in Cells.SelectedItems)
            {
                if (allSame)
                {
                    cell.ResetBackground();
                }
                else
                {
                    cell.Background = br;
                }
            }
        }

        internal void Restore(string[] ssData)
        {
            ClearBoard();
            IsDesignMode = false;

            if (ssData.Length > 0 && ssData[0].StartsWith("L"))
            {
                var numString = ssData[0][1..];
                if (int.TryParse(numString, out var num))
                {
                    ChangeLayout(num);
                }
                ssData = ssData.Skip(1).ToArray();
            }

            int[] initial = GetPuzzle(ssData);

            QQWing ss = new();
            ss.SetPuzzle(initial);
            ss.Solve(CancellationToken.None);
            if (ss.IsSolved())
            {
                int[] solution = ss.GetSolution();
                if (ss.HasMultipleSolutions())
                    MessageBox.Show("Puzzle has multiple solutions");

                List<CellState> list = new();

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

        private static int[] GetPuzzle(string[] ssData)
        {
            if (ssData.Length != 9)
                return Array.Empty<int>();

            int[] puzzle = new int[81];
            for (int row = 0; row < 9; row++)
            {
                string[] parts = ssData[row].Split(',');
                if (parts.Length != 9)
                    return Array.Empty<int>();

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

        internal void OpenSimpleSudoku(string[] lines)
        {
            ClearBoard();

            if (lines.Length > 0 && lines[0].StartsWith("L"))
            {
                var numString = lines[0][1..];
                if (int.TryParse(numString, out var num))
                {
                    ChangeLayout(num);
                }
                lines = lines.Skip(1).ToArray();
            }
            else
            {
                ChangeLayout(QQWing.ClassicLayout);
            }

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

            QQWing ss = new();
            ss.SetPuzzle(initial);
            ss.Solve(CancellationToken.None);
            if (ss.IsSolved())
            {
                int[] solution = ss.GetSolution();
                if (ss.HasMultipleSolutions())
                    MessageBox.Show("Puzzle has multiple solutions");

                undoStack.Clear();
                List<CellState> list = new();

                int idx = 0;
                foreach (var cell in allCells)
                {
                    int given = initial[idx];
                    int answer = solution[idx];

                    CellState cellState = new(idx, given > 0, Math.Max(0, given));
                    cell.Initialize(cellState, answer);

                    list.Add(cellState);

                    idx++;
                }

                undoStack.Push(list);
                IsInProgress = true;
            }
        }

        internal void ClearBoard()
        {
            undoStack.Clear();
            redoStack.Clear();

            foreach (var cell in allCells)
            {
                cell.Reset();
            }
        }

        private void UpdateLayout()
        {
            foreach (var cell in allCells)
            {
                cell.ShowLayoutBoundaries();
            }
        }

        internal void ChangeLayout(int layout)
        {
            ClearBoard();
            if (layout == QQWing.ClassicLayout)
            {
                if (!QQWing.IsClassicLayout)
                {
                    QQWing.SectionLayout = new RegularLayout();
                }
            }
            else
            {
                if (QQWing.IsClassicLayout)
                {
                    QQWing.SectionLayout = new IrregularLayout();
                }
                QQWing.SectionLayout.Layout = layout;
            }

            UpdateLayout();
            sections = list.GroupBy(cell => cell.Section)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Row).ThenBy(c => c.Col).ToList());
        }

        internal async void NewPuzzle(Difficulty difficulty, Symmetry symmetry)
        {
            ClearBoard();
            Puzzle puz = new();
            await puz.Generate(difficulty, symmetry);

            if (puz.Initial.Length == allCells.Count)
            {
                undoStack.Clear();
                List<CellState> list = new();

                int idx = 0;
                foreach (var cell in allCells)
                {
                    int given = puz.Initial[idx];
                    int answer = puz.Solution[idx];

                    CellState cellState = new(idx, given > 0, Math.Max(0, given));
                    cell.Initialize(cellState, answer);

                    list.Add(cellState);

                    idx++;
                }

                undoStack.Push(list);
                IsInProgress = true;
            }
        }

        internal void EnterDesignMode()
        {
            IsDesignMode = true;
        }

        internal void ExitDesignMode()
        {
            IsDesignMode = false;

            if (!allCells.Any(c => c.Given))
                return;

            int[] initial = new int[81];
            foreach (var cell in allCells)
            {
                if (cell.Given)
                {
                    int cellIndex = QQWing.RowColumnToCell(cell.Row, cell.Col);
                    initial[cellIndex] = cell.Value;
                }
            }

            QQWing ss = new();
            ss.SetPuzzle(initial);
            ss.Solve(CancellationToken.None);
            if (ss.IsSolved())
            {
                int[] solution = ss.GetSolution();
                if (ss.HasMultipleSolutions())
                    MessageBox.Show("Puzzle has multiple solutions");

                undoStack.Clear();
                List<CellState> list = new();

                int idx = 0;
                foreach (var cell in allCells)
                {
                    int given = initial[idx];
                    int answer = solution[idx];

                    CellState cellState = new(idx, given > 0, Math.Max(0, given));
                    cell.Initialize(cellState, answer);

                    list.Add(cellState);

                    idx++;
                }

                undoStack.Push(list);
                IsInProgress = true;
            }
        }

        internal void SetSelectedNumber(int value)
        {
            // click on selected button, unset the button
            if (!selectedNumberChanged && (int)SelectedNumber == value)
            {
                SelectedNumber = NumberSelection.None;
            }
            else if (value >= 1 && value <= 12)
            {
                SelectedNumber = (NumberSelection)value;
            }

            selectedNumberChanged = false;
        }
    }
}
