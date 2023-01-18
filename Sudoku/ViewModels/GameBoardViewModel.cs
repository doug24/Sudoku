﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
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

        public GameBoardViewModel()
        {
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
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HighlightIncorrect))
            {
                foreach (var cell in allCells)
                {
                    cell.Redraw(highlightIncorrect);
                }
            }

            base.OnPropertyChanged(e);
        }

        [ObservableProperty]
        private bool highlightIncorrect = true;

        [ObservableProperty]
        private bool cleanPencilMarks = true;

        [ObservableProperty]
        private bool isDesignMode;

        public bool IsInProgress { get; private set; }

        internal string ToSnapshotString()
        {
            StringBuilder sb = new();
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

                CheckComplete();

                Debug.WriteLine($"Redo after - undo stack count: {undoStack.Count}; redo stack count {redoStack.Count}");
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

            foreach (var cell in Cells.SelectedItems)
            {
                int cellIndex = QQWing.RowColumnToCell(cell.Row, cell.Col);

                if (IsDesignMode)
                {
                    cell.Initialize(new(cellIndex, true, value), value);
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
                        if (!oldState.HasCandidate(value))
                            newState = oldState.AddCandidate(value);
                    }
                    else if (mode == KeyPadMode.Eraser)
                    {
                        if (oldState.HasCandidate(value))
                            newState = oldState.RemoveCandidate(value);
                    }

                    if (newState != null && newState != oldState)
                    {
                        cell.SetState(newState, HighlightIncorrect);
                        list.Add(newState);
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

        internal void SetColor(Brush br, KeyPadMode mode)
        {
            foreach (var cell in Cells.SelectedItems)
            {
                if (mode == KeyPadMode.Eraser)
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

        internal async void NewPuzzle(Difficulty difficulty, Symmetry symmetry)
        {
            ClearBoard();
            QQWing.SectionLayout.SetRandomLayout();
            UpdateLayout();
            sections = list.GroupBy(cell => cell.Section)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Row).ThenBy(c => c.Col).ToList());


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
    }
}
