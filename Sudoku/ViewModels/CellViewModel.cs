using System.Collections.ObjectModel;
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

            SetValue(8);
        }

        public CellViewModel(int row, int col, int sqr)
        {
            Row = row;
            Col = col;
            Square = sqr;

            for (int idx = 1; idx <= 9; idx++)
            {
                Candidates.Add(new CandidateViewModel(idx));
            }
        }

        public override string ToString()
        {
            return $"r{Row} c{Col} : {Number}";
        }

        public int Row { get; private set; }
        public int Col { get; private set; }
        public int Square { get; private set; }

        public ObservableCollection<CandidateViewModel> Candidates { get; } = new ObservableCollection<CandidateViewModel>();

        public void Reset()
        {
            Given = false;
            ClearValue();
            foreach (var can in Candidates)
            {
                can.Visible = false;
            }
        }

        public void SetGiven(char ch)
        {
            Number = ch.ToString();
            Given = true;
            Background = Brushes.LightCyan;
        }

        public void SetValue(int value)
        {
            if (!Given)
                Number = value.ToString();
        }

        public void ClearValue()
        {
            if (!Given)
                Number = string.Empty;
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

        public Brush background = Brushes.Transparent;
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
    }
}
