using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku
{
    public class CellViewModel : ViewModelBase
    {
        public CellViewModel(int row, int col)
        {
            Row = row;
            Col = col;
            SetValue(row + 1);
        }

        public int Row { get; private set; }
        public int Col { get; private set; }

        public void Reset()
        {
            Given = false;
            ClearValue();
            Can1 = false;
            Can2 = false;
            Can3 = false;
            Can4 = false;
            Can5 = false;
            Can6 = false;
            Can7 = false;
            Can8 = false;
            Can9 = false;
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

        private bool can1;
        public bool Can1
        {
            get { return can1; }
            set
            {
                if (can1 == value)
                    return;

                can1 = value;
                OnPropertyChanged(nameof(Can1));
            }
        }

        private bool can2;
        public bool Can2
        {
            get { return can2; }
            set
            {
                if (can2 == value)
                    return;

                can2 = value;
                OnPropertyChanged(nameof(Can2));
            }
        }

        private bool can3;
        public bool Can3
        {
            get { return can3; }
            set
            {
                if (can3 == value)
                    return;

                can3 = value;
                OnPropertyChanged(nameof(Can3));
            }
        }

        private bool can4;
        public bool Can4
        {
            get { return can4; }
            set
            {
                if (can4 == value)
                    return;

                can4 = value;
                OnPropertyChanged(nameof(Can4));
            }
        }

        private bool can5;
        public bool Can5
        {
            get { return can5; }
            set
            {
                if (can5 == value)
                    return;

                can5 = value;
                OnPropertyChanged(nameof(Can5));
            }
        }

        private bool can6;
        public bool Can6
        {
            get { return can6; }
            set
            {
                if (can6 == value)
                    return;

                can6 = value;
                OnPropertyChanged(nameof(Can6));
            }
        }

        private bool can7;
        public bool Can7
        {
            get { return can7; }
            set
            {
                if (can7 == value)
                    return;

                can7 = value;
                OnPropertyChanged(nameof(Can7));
            }
        }

        private bool can8;
        public bool Can8
        {
            get { return can8; }
            set
            {
                if (can8 == value)
                    return;

                can8 = value;
                OnPropertyChanged(nameof(Can8));
            }
        }

        private bool can9;
        public bool Can9
        {
            get { return can9; }
            set
            {
                if (can9 == value)
                    return;

                can9 = value;
                OnPropertyChanged(nameof(Can9));
            }
        }
    }
}
