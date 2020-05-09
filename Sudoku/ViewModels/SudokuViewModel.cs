namespace Sudoku
{
    public class SudokuViewModel : ViewModelBase
    {
        public SudokuViewModel()
        {
        }

        private GameBoardViewModel gameBoard = new GameBoardViewModel();

        public GameBoardViewModel GameBoard
        {
            get { return gameBoard; }
            set
            {
                if (value == gameBoard)
                    return;

                gameBoard = value;
                OnPropertyChanged(nameof(GameBoard));
            }
        }
    }
}
