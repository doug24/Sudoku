using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Sudoku
{
    public class SudokuViewModel : ViewModelBase
    {
        public ObservableCollection<SquareViewModel> Squares { get; } = new ObservableCollection<SquareViewModel>();

        private readonly Dictionary<int, List<CellViewModel>> rows;
        private readonly Dictionary<int, List<CellViewModel>> cols;
        private readonly Dictionary<int, List<CellViewModel>> sqrs;
        private readonly List<CellViewModel> allCells;


        public SudokuViewModel()
        {
            for (int sq = 1; sq <= 9; sq++)
            {
                Squares.Add(new SquareViewModel(sq));
            }

            rows = Squares.SelectMany(c => c.Cells)
                .GroupBy(cell => cell.Row)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Col).ToList());

            cols = Squares.SelectMany(c => c.Cells)
                .GroupBy(cell => cell.Col)
                .OrderBy(v => v.Key)
                .ToDictionary(v => v.Key, v => v.OrderBy(c => c.Row).ToList());

            sqrs = Squares.OrderBy(s => s.Square)
                .ToDictionary(s => s.Square, s => s.Cells.OrderBy(c => c.Row).ThenBy(c => c.Col).ToList());

            allCells = Squares.SelectMany(c => c.Cells)
                .OrderBy(cell => cell.Row)
                .ThenBy(cell => cell.Col)
                .ToList();

            Initialize();
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
