using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QQWingLib;

namespace Sudoku
{
    public partial class CellViewModel : ObservableObject
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

        public CellViewModel(int row, int col)
        {
            Row = row;
            Col = col;
            CellIndex = QQWing.RowColumnToCell(row, col);
            Section = QQWing.CellToSection(CellIndex);

            var sectionLayout = QQWing.SectionLayout;
            if (sectionLayout.RightBoundaries.Contains(cellIndex))
            {
                RightBrush = Brushes.DarkViolet;
            }
            if (sectionLayout.BottomBoundaries.Contains(cellIndex))
            {
                BottomBrush = Brushes.DarkViolet;
            }

            for (int idx = 1; idx <= 9; idx++)
            {
                Candidates.Add(new CandidateViewModel(idx));
            }
        }

        public override string ToString()
        {
            return $"r{Row} c{Col} s{Section}";
        }

        public int Row { get; private set; }
        public int Col { get; private set; }
        public int Section { get; private set; }

        [ObservableProperty]
        private int cellIndex;

        public int Value { get; private set; }
        public int Answer { get; private set; }

        public ObservableCollection<CandidateViewModel> Candidates { get; } = new();

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

            SetState(state, false);
        }

        public void SetState(CellState state, bool colorIncorrect)
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
                if (colorIncorrect)
                    Foreground = Value == Answer ? Brushes.DarkGreen : Brushes.Red;
                else
                    Foreground = Brushes.DarkGreen;
            }
            SetCandidates(state.Candidates);
        }

        public void ToggleHighlight(Brush br)
        {
            if (Background == br)
                Background = Brushes.White;
            else
                Background = br;
        }

        public void Redraw(bool colorIncorrect)
        {
            if (!Given)
            {
                if (colorIncorrect)
                    Foreground = Value == Answer ? Brushes.DarkGreen : Brushes.Red;
                else
                    Foreground = Brushes.DarkGreen;
            }
        }

        private void SetCandidates(int[] candidates)
        {
            for (int c = 1; c <= 9; c++)
            {
                Candidates[c - 1].Visible = candidates.Contains(c) && string.IsNullOrEmpty(Number);
            }
        }

        [ObservableProperty]
        private bool given;

        [ObservableProperty]
        private string number = string.Empty;

        [ObservableProperty]
        public Brush background = Brushes.White;

        [ObservableProperty]
        public Brush foreground = Brushes.DarkGreen;

        [ObservableProperty]
        private Brush rightBrush = Brushes.Transparent;

        [ObservableProperty]
        private Brush bottomBrush = Brushes.Transparent;
    }
}
