namespace Sudoku
{
    public class CandidateViewModel : ViewModelBase
    {
        public CandidateViewModel(int num)
        {
            Number = num.ToString();
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

        private bool visible;
        public bool Visible
        {
            get { return visible; }
            set
            {
                if (visible == value)
                    return;

                visible = value;
                OnPropertyChanged(nameof(Visible));
            }
        }

    }
}