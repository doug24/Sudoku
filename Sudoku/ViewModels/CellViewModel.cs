using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace Sudoku
{
    public class CellViewModel : ViewModelBase
    {
        public CellViewModel()
        {
            for (int idx = 1; idx <= 9; idx++)
            {
                Candidates.Add(new CandidateViewModel(idx));
            }
            foreach (var can in Candidates)
            {
                can.Visible = true;
            }

            Number = "8";
        }

        public CellViewModel(int row, int col, int sqr)
        {
            Row = row;
            Col = col;
            Square = sqr;

            LayoutRow = row < 3 ? row : row < 6 ? row + 1 : row + 2;
            LayoutCol = col < 3 ? col : col < 6 ? col + 1 : col + 2;

            for (int idx = 1; idx <= 9; idx++)
            {
                Candidates.Add(new CandidateViewModel(idx));
            }
        }

        public override string ToString()
        {
            return $"r{Row} c{Col} s{Square}";
        }

        public int Row { get; private set; }
        public int Col { get; private set; }
        public int Square { get; private set; }

        public int LayoutRow { get; private set; }
        public int LayoutCol { get; private set; }

        public int Value { get; private set; }
        public int Answer { get; private set; }

        public ObservableCollection<CandidateViewModel> Candidates { get; } = new ObservableCollection<CandidateViewModel>();

        public void Reset()
        {
            Given = false;
            Background = Brushes.White;
            Foreground = Brushes.DarkGreen;
            Number = string.Empty;
            Value = 0;
            Answer = 0;

            foreach (var can in Candidates)
            {
                can.Visible = false;
            }
        }

        public void Initialize(CellState state, int answer)
        {
            Reset();

            Answer = answer;

            SetState(state);
        }

        public void SetState(CellState state)
        {
            if (state.Given)
            {
                Value = state.Value;
                Given = true;
                Number = Value.ToString();
                Foreground = Brushes.Black;
            }
            else
            {
                Value = state.Value;
                Number = state.Value <= 0 ? string.Empty : state.Value.ToString();
                Foreground = Value == Answer ? Brushes.DarkGreen : Brushes.Red;
            }
            SetCandidates(state.Candidates);
        }

        private void SetCandidates(int[] candidates)
        {
            for (int c = 1; c <= 9; c++)
            {
                Candidates[c - 1].Visible = candidates.Contains(c) && string.IsNullOrEmpty(Number);
            }
        }

        private bool given;
        public bool Given
        {
            get { return given; }
            set
            {
                if (given == value)
                    return;

                given = value;
                OnPropertyChanged(nameof(Given));
            }
        }

        private string number;
        public string Number
        {
            get { return number; }
            set
            {
                if (number == value)
                    return;

                number = value;
                OnPropertyChanged(nameof(Number));
            }
        }

        public Brush background = Brushes.White;
        public Brush Background
        {
            get { return background; }
            set
            {
                if (background == value)
                    return;

                background = value;
                OnPropertyChanged(nameof(Background));
            }
        }

        public Brush foreground = Brushes.DarkGreen;
        public Brush Foreground
        {
            get { return foreground; }
            set
            {
                if (foreground == value)
                    return;

                foreground = value;
                OnPropertyChanged(nameof(Foreground));
            }
        }
    }
}
