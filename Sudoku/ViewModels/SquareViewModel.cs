using System.Collections.ObjectModel;

namespace Sudoku
{
    public class SquareViewModel : ViewModelBase
    {
        public SquareViewModel()
        {
            Square = 1;
            InitCells();

            int num = 1;
            foreach (var cell in Cells)
            {
                cell.SetValue(num++);
            }
        }

        public SquareViewModel(int sq)
        {
            Square = sq;
            InitCells();
        }

        private void InitCells()
        {
            int baseRow = 0, baseCol = 0;
            switch (Square)
            {
                case 1:
                    baseRow = 1;
                    baseCol = 1;
                    break;
                case 2:
                    baseRow = 1;
                    baseCol = 4;
                    break;
                case 3:
                    baseRow = 1;
                    baseCol = 7;
                    break;
                case 4:
                    baseRow = 4;
                    baseCol = 1;
                    break;
                case 5:
                    baseRow = 4;
                    baseCol = 4;
                    break;
                case 6:
                    baseRow = 4;
                    baseCol = 7;
                    break;
                case 7:
                    baseRow = 7;
                    baseCol = 1;
                    break;
                case 8:
                    baseRow = 7;
                    baseCol = 4;
                    break;
                case 9:
                    baseRow = 7;
                    baseCol = 7;
                    break;
            }

            for (int row = baseRow; row < baseRow + 3; row++)
            {
                for (int col = baseCol; col < baseCol + 3; col++)
                {
                    Cells.Add(new CellViewModel(row, col, Square));
                }
            }
        }

        public int Square { get; private set; }
        public ObservableCollection<CellViewModel> Cells { get; } = new ObservableCollection<CellViewModel>();
    }
}