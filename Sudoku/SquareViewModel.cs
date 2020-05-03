using System;
using System.Collections.ObjectModel;

namespace Sudoku
{
    public class SquareViewModel : ViewModelBase
    {
        public SquareViewModel()
        {
            Row = 0;
            Col = 0;
            InitCells();
        }

        public SquareViewModel(int row, int col)
        {
            Row = row;
            Col = col;
            InitCells();
        }

        private void InitCells()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    Cells.Add(new CellViewModel(row, col));
                }
            }
        }

        public int Row { get; private set; }
        public int Col { get; private set; }
        public ObservableCollection<CellViewModel> Cells { get; set; } = new ObservableCollection<CellViewModel>();
    }
}