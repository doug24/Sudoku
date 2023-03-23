using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sudoku
{
    public partial class CandidateViewModel : ObservableObject
    {
        public CandidateViewModel(int num)
        {
            Value = num;
            Number = num.ToString();
        }

        [ObservableProperty]
        private int value;

        [ObservableProperty]
        private string number = string.Empty;

        [ObservableProperty]
        private bool visible;

        [ObservableProperty]
        private bool isHighlight;
    }
}