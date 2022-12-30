using CommunityToolkit.Mvvm.ComponentModel;

namespace Sudoku
{
    public partial class CandidateViewModel : ObservableObject
    {
        public CandidateViewModel(int num)
        {
            Number = num.ToString();
        }

        [ObservableProperty]
        private string number;

        [ObservableProperty]
        private bool visible;
    }
}