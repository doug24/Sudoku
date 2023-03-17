using CommunityToolkit.Mvvm.ComponentModel;

namespace Sudoku
{
    public partial class NumberButtonViewModel : ObservableObject
    {
        public NumberButtonViewModel(int num)
        {
            Value = num;
            Number = num.ToString();
        }

        internal void SetRemainder(int remainder)
        {
            if (remainder >= 1 && remainder <= 9)
            {
                Count = remainder.ToString();
            }
            else
            {
                Count = string.Empty;
            }
        }

        [ObservableProperty]
        private int value;

        [ObservableProperty]
        private string number = string.Empty;

        [ObservableProperty]
        private string count = string.Empty;
    }
}