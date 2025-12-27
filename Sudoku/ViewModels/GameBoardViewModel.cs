using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using QQWingLib;

namespace Sudoku;

public partial class GameBoardViewModel : ObservableObject
{
    private readonly DispatcherTimer periodicTimer = new();
    private readonly Stopwatch stopwatch = new();

    private readonly Stack<List<CellState>> undoStack = new();
    private readonly Stack<List<CellState>> redoStack = new();

    public MultiSelectCollectionView<CellViewModel> Cells { get; private set; }

    private readonly List<CellViewModel> list = [];
    private readonly List<CellViewModel> allCells;
    private readonly Dictionary<int, List<CellViewModel>> rows;
    private readonly Dictionary<int, List<CellViewModel>> cols;
    private Dictionary<int, List<CellViewModel>> sections;

    private int currentHighlightNumber = -1;
    private bool selectedNumberChanged;

    public GameBoardViewModel()
    {
        Background = Properties.Settings.Default.DarkMode ? Brushes.Black : Brushes.White;
        SectionBorder = Properties.Settings.Default.DarkMode ? Brushes.LightBlue : Brushes.DarkViolet;
        HighlightIncorrect = Properties.Settings.Default.HighlightIncorrect;
        CleanPencilMarks = Properties.Settings.Default.CleanPencilMarks;
        ShowTimer = Properties.Settings.Default.ShowTimer;
        NumberFirstMode = Properties.Settings.Default.NumberFirstMode;

        periodicTimer.Interval = TimeSpan.FromSeconds(1);
        periodicTimer.Tick += OnTimer_Tick;
        periodicTimer.Start();

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

        allCells =
        [
            .. list.OrderBy(cell => cell.Row).ThenBy(cell => cell.Col)
        ];

        EnableNumberHighlight = Properties.Settings.Default.EnableNumberHighlight;
    }

    internal void SaveSettings()
    {
        Properties.Settings.Default.HighlightIncorrect = HighlightIncorrect;
        Properties.Settings.Default.CleanPencilMarks = CleanPencilMarks;
        Properties.Settings.Default.ShowTimer = ShowTimer;
        Properties.Settings.Default.NumberFirstMode = NumberFirstMode;
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
    private Brush background = Brushes.White;
    [ObservableProperty]
    private Brush sectionBorder = Brushes.White;

    [ObservableProperty]
    private Brush innerSelectionBorder = Brushes.Yellow;
    [ObservableProperty]
    private Brush outerSelectionBorder = Brushes.Black;

    [ObservableProperty]
    private NumberButtonViewModel number1 = new(1);
    [ObservableProperty]
    private NumberButtonViewModel number2 = new(2);
    [ObservableProperty]
    private NumberButtonViewModel number3 = new(3);
    [ObservableProperty]
    private NumberButtonViewModel number4 = new(4);
    [ObservableProperty]
    private NumberButtonViewModel number5 = new(5);
    [ObservableProperty]
    private NumberButtonViewModel number6 = new(6);
    [ObservableProperty]
    private NumberButtonViewModel number7 = new(7);
    [ObservableProperty]
    private NumberButtonViewModel number8 = new(8);
    [ObservableProperty]
    private NumberButtonViewModel number9 = new(9);

    [ObservableProperty]
    private bool cleanPencilMarks = true;

    [ObservableProperty]
    private bool isDesignMode;

    [ObservableProperty]
    private bool isMultiSelect = true;

    [ObservableProperty]
    private SelectionMode selectionMode = SelectionMode.Extended;

    [ObservableProperty]
    private KeyPadMode keyInputMode = KeyPadMode.Pen;

    [ObservableProperty]
    private bool numberFirstMode = false;

    partial void OnNumberFirstModeChanged(bool value)
    {
        // order of setting matters
        SelectionMode = value ? SelectionMode.Single : SelectionMode.Extended;
        IsMultiSelect = !value;
        if (!value)
        {
            SelectedNumber = NumberSelection.None;
        }

        InnerSelectionBorder = value ? Brushes.Transparent : Brushes.Yellow;
        OuterSelectionBorder = value ? Brushes.Transparent : Brushes.Black;
    }

    [ObservableProperty]
    private bool showTimer = false;

    [ObservableProperty]
    private string time = string.Empty;

    [ObservableProperty]
    private bool isInProgress = false;

    private void OnTimer_Tick(object? sender, EventArgs e)
    {
        if (ShowTimer)
        {
            Time = stopwatch.Elapsed.ToString(@"mm\:ss");
        }
        else
        {
            Time = string.Empty;
        }
    }

    internal void OnStateChanged(WindowState state)
    {
        if (state == WindowState.Minimized && stopwatch.IsRunning)
        {
            stopwatch.Stop();
        }
        else if (state != WindowState.Minimized && IsInProgress)
        {
            stopwatch.Start();
        }
    }

    [ObservableProperty]
    private NumberSelection selectedNumber = NumberSelection.None;

    private int numberHighlightWithColor = -1;
    partial void OnSelectedNumberChanged(NumberSelection oldValue, NumberSelection newValue)
    {
        selectedNumberChanged = true;

        if (EnableNumberHighlight && NumberFirstMode)
        {
            if (oldValue <= NumberSelection.D9 &&
                newValue >= NumberSelection.C1)
            {
                // toggle highlight off and back on to the
                // old value to keep numbers highlighted 
                // when in color cell mode
                HighlightNumbers(-1);
                HighlightNumbers((int)oldValue);
                numberHighlightWithColor = (int)oldValue;
            }
            else if (oldValue >= NumberSelection.C1 &&
                     newValue >= NumberSelection.C1)
            {
                // changing colors, keep previous number highlighted
                HighlightNumbers(-1);
                HighlightNumbers(numberHighlightWithColor);
            }
            else if (oldValue >= NumberSelection.C1 &&
                     newValue <= NumberSelection.D9)
            {
                // going from color to number, reselect the number
                HighlightNumbers(-1);
                HighlightNumbers((int)newValue);
            }
            else
            {
                int number = (int)newValue;
                HighlightNumbers(number);
            }
        }
        if (!NumberFirstMode && SelectedNumber != NumberSelection.None)
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

    public ICommand FillCandidatesCommand => new RelayCommand(
        p => FillCandidates(),
        q => IsInProgress);

    public ICommand FastForwardCommand => new RelayCommand(
        p => FastForward(),
        q => IsInProgress);


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

            int chn = currentHighlightNumber;
            HighlightNumbers(-1);
            HighlightNumbers(chn);
            UpdateRemainderCounts();
            IsInProgress = true;
            stopwatch.Start();
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

            int chn = currentHighlightNumber;
            HighlightNumbers(-1);
            HighlightNumbers(chn);
            UpdateRemainderCounts();
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
        numberHighlightWithColor = -1;
    }

    internal void KeyDown(int value)
    {
        if (!(IsInProgress || IsDesignMode)) return;

        List<CellState> list = [];
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
                if (KeyInputMode == KeyPadMode.Pen)
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
                else if (KeyInputMode == KeyPadMode.Pencil)
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
            UpdateRemainderCounts();
        }
        else
        {
            if (list.Count > 0)
            {
                undoStack.Push(list);
                redoStack.Clear();
            }

            UpdateRemainderCounts();
            CheckComplete();

            if (IsInProgress && CleanPencilMarks && inkAnswerIndex > -1)
                DoPencilCleanup(inkAnswerIndex);
        }
    }

    internal void CellMouseDown(CellViewModel cell, MouseButtonEventArgs mouseArgs)
    {
        if (!(IsInProgress || IsDesignMode)) return;

        if (!NumberFirstMode) return;

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
                UpdateRemainderCounts();
                CheckInvalid();
            }
            return;
        }

        List<CellState> list = [];
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

            KeyPadMode inputMode = KeyPadMode.Pen;
            if ((KeyInputMode == KeyPadMode.Pen && mouseArgs.ChangedButton == MouseButton.Right) ||
                (KeyInputMode == KeyPadMode.Pencil && mouseArgs.ChangedButton == MouseButton.Left))
            {
                inputMode = KeyPadMode.Pencil;
            }

            CellState? newState = null;
            if (inputMode == KeyPadMode.Pen)
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
            else if (inputMode == KeyPadMode.Pencil)
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

            UpdateRemainderCounts();
            CheckComplete();

            if (IsInProgress && CleanPencilMarks && inkAnswerIndex > -1)
                DoPencilCleanup(inkAnswerIndex);
        }
    }

    internal void CellMouseDoubleClick(CellViewModel cell)
    {
        if (!(IsInProgress || IsDesignMode)) return;


        if (NumberFirstMode && cell.Given)
        {
            SelectedNumber = (NumberSelection)cell.Value;
        }
        else
        {
            if (cell.Value > 0)
            {
                HighlightNumbers(cell.Value);
            }
            else
            {
                HighlightNumbers(-1);
            }
        }
    }

    private void FillCandidates()
    {
        int[] current = new int[81];
        foreach (var cell in allCells)
        {
            if (cell.Value > 0)
            {
                int cellIndex = QQWing.RowColumnToCell(cell.Row, cell.Col);
                current[cellIndex] = cell.Value;
            }
        }

        int[] candidates = GetCandidates(current);

        List<CellState> list = [];

        int idx = 0;
        foreach (var cell in allCells)
        {
            if (cell.Value == 0)
            {
                int[] cellCandidates = GetCandidatesForCell(candidates, idx);
                AddCandidates(list, cell, idx, cellCandidates);
            }

            idx++;
        }

        if (list.Count > 0)
        {
            undoStack.Push(list);
            redoStack.Clear();
        }
    }

    private static int[] GetCandidates(int[] puzzle)
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

    private static int[] GetCandidatesForCell(int[] candidates, int cell)
    {
        if (candidates.Length > 0)
        {
            List<int> list = [];

            for (int idx = 0; idx < 9; idx++)
            {
                int pi = QQWing.GetPossibilityIndex(idx, cell);
                int num = candidates[pi];
                if (num > 0)
                    list.Add(num);
            }
            return [.. list];
        }
        return [];
    }

    private void DoPencilCleanup(int cellIndex)
    {
        List<CellState> list = [];

        var cell = allCells[cellIndex];
        if (!cell.Given && cell.Value > 0)
        {
            if (rows.TryGetValue(cell.Row, out List<CellViewModel>? cellsInRow))
            {
                foreach (var c in cellsInRow)
                {
                    ClearCandidates(list, cell.Value, c);
                }
            }
            if (cols.TryGetValue(cell.Col, out List<CellViewModel>? cellsInCol))
            {
                foreach (var c in cellsInCol)
                {
                    ClearCandidates(list, cell.Value, c);
                }
            }
            if (sections.TryGetValue(cell.Section, out List<CellViewModel>? cellsInSect))
            {
                foreach (var c in cellsInSect)
                {
                    ClearCandidates(list, cell.Value, c);
                }
            }
        }

        if (list.Count > 0)
        {
            undoStack.Push(list);
            redoStack.Clear();
        }
    }

    private void AddCandidates(List<CellState> list, CellViewModel cell, int cellIndex, int[] cellCandidates)
    {
        var oldState = GetCurrentCellState(cellIndex);
        var newState = oldState.AddCandidates(cellCandidates);
        if (newState != oldState)
        {
            cell.SetState(newState, HighlightIncorrect);
            list.Add(newState);
        }
    }

    private void ClearCandidates(List<CellState> list, int value, CellViewModel cell)
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

    private void ClearRemainderCounts()
    {
        Number1.SetRemainder(-1);
        Number2.SetRemainder(-1);
        Number3.SetRemainder(-1);
        Number4.SetRemainder(-1);
        Number5.SetRemainder(-1);
        Number6.SetRemainder(-1);
        Number7.SetRemainder(-1);
        Number8.SetRemainder(-1);
        Number9.SetRemainder(-1);
    }

    private void UpdateRemainderCounts()
    {
        // skipping index 0, using indexes 1 - 9
        int[] completed = new int[10];
        foreach (var cell in allCells)
        {
            if (cell.Value > 0)
            {
                completed[cell.Value]++;
            }
        }
        Number1.SetRemainder(9 - completed[1]);
        Number2.SetRemainder(9 - completed[2]);
        Number3.SetRemainder(9 - completed[3]);
        Number4.SetRemainder(9 - completed[4]);
        Number5.SetRemainder(9 - completed[5]);
        Number6.SetRemainder(9 - completed[6]);
        Number7.SetRemainder(9 - completed[7]);
        Number8.SetRemainder(9 - completed[8]);
        Number9.SetRemainder(9 - completed[9]);
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
            stopwatch.Stop();
            IsInProgress = false;
            CustomMessageBox.Show("Solved!", "Sudoku", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

        if (ssData.Length > 0 && ssData[0].StartsWith('L'))
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
                CustomMessageBox.Show("Puzzle has multiple solutions", "Sudoku");

            List<CellState> list = [];

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
            stopwatch.Restart();
        }
        UpdateRemainderCounts();
    }

    private static int[] GetPuzzle(string[] ssData)
    {
        if (ssData.Length != 9)
            return [];

        int[] puzzle = new int[81];
        for (int row = 0; row < 9; row++)
        {
            string[] parts = ssData[row].Split(',');
            if (parts.Length != 9)
                return [];

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

        if (lines.Length > 0 && lines[0].StartsWith('L'))
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
                    !line.StartsWith('*') &&
                    !line.StartsWith('-') &&
                    !line.StartsWith("|-") &&
                    !line.StartsWith('E') &&
                    !line.StartsWith('I'))
                {
                    list.Add(line.Replace("|", ""));
                }
            }
            lines = [.. list];
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
                CustomMessageBox.Show("Puzzle has multiple solutions", "Sudoku");

            undoStack.Clear();
            List<CellState> list = [];

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
            stopwatch.Restart();
        }
        UpdateRemainderCounts();
    }

    internal void ClearBoard()
    {
        undoStack.Clear();
        redoStack.Clear();
        stopwatch.Reset();
        Time = ShowTimer ? "00:00" : string.Empty;

        foreach (var cell in allCells)
        {
            cell.Reset();
        }
        if (NumberFirstMode)
        {
            SelectedNumber = NumberSelection.None;
        }
        HighlightNumbers(-1);
        ClearRemainderCounts();
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
            List<CellState> list = [];

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
            UpdateRemainderCounts();
            IsInProgress = true;
            stopwatch.Restart();
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
                CustomMessageBox.Show("Puzzle has multiple solutions", "Sudoku");

            undoStack.Clear();
            List<CellState> list = [];

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
            UpdateRemainderCounts();
            IsInProgress = true;
            stopwatch.Restart();
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

    internal async void FastForward()
    {
        Dictionary<int, List<CellViewModel>>[] groups =
        [
            rows,
            cols,
            sections
        ];

        bool hasChange;
        do
        {
            hasChange = false;

            foreach (var group in groups)
            {
                for (int index = 0; index < 9; index++)
                {
                    if (group.TryGetValue(index, out var cellGroup))
                    {
                        if (await CheckSingletonCandidate(cellGroup))
                        {
                            hasChange = true;
                        }

                        if (await CheckSingletonOpenCell(cellGroup))
                        {
                            hasChange = true;
                        }
                    }
                }
            }
        } while (hasChange);
    }

    private static readonly int[] digits = [1, 2, 3, 4, 5, 6, 7, 8, 9];

    private async Task<bool> CheckSingletonOpenCell(List<CellViewModel> list)
    {
        var openCells = list.Where(c => c.Value == 0);
        if (openCells.Count() == 1 && openCells.FirstOrDefault() is CellViewModel cell)
        {
            int digit = digits.Except(list.Where(c => c.Value != 0).Select(c => c.Value)).FirstOrDefault();
            if (digit != 0)
            {
                await UpdateCellState(cell, digit);
                return true;
            }
        }

        return false;
    }

    private async Task<bool> CheckSingletonCandidate(List<CellViewModel> list)
    {
        foreach (var cell in list)
        {
            if (cell.Value == 0)
            {
                var singletons = cell.Candidates.Where(p => p.Visible);
                if (singletons.Count() == 1 && singletons.FirstOrDefault() is CandidateViewModel candidate)
                {
                    int digit = candidate.Value;
                    if (digit == cell.Answer)
                    {
                        await UpdateCellState(cell, digit);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private async Task UpdateCellState(CellViewModel cell, int value)
    {
        var oldState = GetCurrentCellState(cell.CellIndex);
        if (oldState.Given)
        {
            return;
        }

        List<CellState> changes = [];
        CellState newState = oldState.SetValue(value);
        if (newState != oldState)
        {
            await Task.Delay(300); // just for animation

            cell.SetState(newState, HighlightIncorrect);
            changes.Add(newState);
            if (EnableNumberHighlight)
            {
                cell.SetHighlight(currentHighlightNumber);
            }

            undoStack.Push(changes);
            redoStack.Clear();

            UpdateRemainderCounts();
            CheckComplete();

            if (CleanPencilMarks)
            {
                DoPencilCleanup(cell.CellIndex);
            }
        }
    }
}
