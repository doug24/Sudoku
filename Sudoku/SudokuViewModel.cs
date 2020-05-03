using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku
{
    public class SudokuViewModel : ViewModelBase
    {
        public ObservableCollection<SquareViewModel> Squares { get; set; } = new ObservableCollection<SquareViewModel>();

        public SudokuViewModel()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    Squares.Add(new SquareViewModel(row, col));
                }
            }
        }
    }
}
