using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Sudoku
{
    public class GameBoardViewModel : ViewModelBase
    {
        private ObservableCollection<CellViewModel> list = new ObservableCollection<CellViewModel>();
        public MultiSelectCollectionView<CellViewModel> Cells { get; private set; }

        private readonly Dictionary<int, List<CellViewModel>> rows;
        private readonly Dictionary<int, List<CellViewModel>> cols;
        private readonly Dictionary<int, List<CellViewModel>> sqrs;
        private readonly List<CellViewModel> allCells;


        public GameBoardViewModel()
        {
            Cells = new MultiSelectCollectionView<CellViewModel>(list);
            Cells.SelectionChanged += Cells_SelectionChanged;

            for (int row = 1; row <= 9; row++)
            {
                for (int col = 1; col <= 9; col++)
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

            Initialize();
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
            if (row > 0 && row <= 3)
            {
                if (col > 0 && col <= 3)
                    return 1;
                else if (col <= 6)
                    return 2;
                else if (col <= 9)
                    return 3;
            }
            else if (row <= 6)
            {
                if (col > 0 && col <= 3)
                    return 4;
                else if (col <= 6)
                    return 5;
                else if (col <= 9)
                    return 6;
            }
            else if (row <= 9)
            {
                if (col > 0 && col <= 3)
                    return 7;
                else if (col <= 6)
                    return 8;
                else if (col <= 9)
                    return 9;
            }

            return 1;
        }

        public async void Initialize()
        {
            Puzzle puz = new Puzzle();
            await puz.Generate();

            if (puz.Initial.Length == allCells.Count)
            {
                int idx = 0;
                foreach (var cell in allCells)
                {
                    cell.Reset();

                    char ch = puz.Initial[idx++];
                    if (char.IsDigit(ch))
                    {
                        cell.SetGiven(ch);
                    }
                }
            }
        }
    }
}
