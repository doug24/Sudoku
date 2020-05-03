using System.Collections.ObjectModel;

namespace Sudoku
{
    public class SudokuViewModel : ViewModelBase
    {
        public ObservableCollection<SquareViewModel> Squares { get; } = new ObservableCollection<SquareViewModel>();

        public SudokuViewModel()
        {
            for (int sq = 1; sq <= 9; sq++)
            {
                Squares.Add(new SquareViewModel(sq));
            }
        }
    }
}
