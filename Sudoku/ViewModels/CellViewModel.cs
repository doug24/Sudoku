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

            ShowLayoutBoundaries();

            for (int idx = 1; idx <= 9; idx++)
            {
                Candidates.Add(new CandidateViewModel(idx));
            }
        }

        public void ShowLayoutBoundaries()
        {
            Section = QQWing.CellToSection(CellIndex);

            var sectionLayout = QQWing.SectionLayout;
            RightBrush = sectionLayout.RightBoundaries.Contains(CellIndex) ? Brushes.DarkViolet : RightBrush = Brushes.Transparent;
            BottomBrush = sectionLayout.BottomBoundaries.Contains(CellIndex) ? Brushes.DarkViolet : BottomBrush = Brushes.Transparent;

            Background = defaultBackground = brushes[Section];
        }

        private readonly static Brush[] brushes = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(255, 250, 247)),//hue= 20
            new SolidColorBrush(Color.FromRgb(235, 246, 255)),//hue=180
            new SolidColorBrush(Color.FromRgb(245, 240, 255)),//hue=260
            new SolidColorBrush(Color.FromRgb(245, 255, 240)),//hue=100
            new SolidColorBrush(Color.FromRgb(255, 240, 245)),//hue=340
            new SolidColorBrush(Color.FromRgb(245, 255, 240)),//hue=100
            new SolidColorBrush(Color.FromRgb(245, 240, 255)),//hue=260
            new SolidColorBrush(Color.FromRgb(235, 246, 255)),//hue=180
            new SolidColorBrush(Color.FromRgb(255, 250, 247)),//hue= 20
        };

        private readonly static Brush[] colorBrushes = new Brush[]
        {
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC2F0FF")),
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFACFFAC")),
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFDDBF")),
        };

        public static Brush GetColor(int idx)
        {
            return colorBrushes[idx];
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

        private Brush defaultBackground = Brushes.Cyan;

        public int Value { get; private set; }
        public int Answer { get; private set; }

        public ObservableCollection<CandidateViewModel> Candidates { get; } = new();

        public void Reset()
        {
            Given = false;
            Background = defaultBackground;
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

        public void ResetBackground()
        {
            Background = defaultBackground;
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

        internal bool HasCandidateSet(int value)
        {
            return Candidates.FirstOrDefault(c => c.Value != 0 && c.Value == value)?.Visible ?? false;
        }

        internal void SetHighlight(int highlightValue)
        {
            IsHighlight = Value == highlightValue;

            foreach (var candidate in Candidates)
            {
                candidate.IsHighlight = candidate.Visible && candidate.Value == highlightValue;
            }
        }

        internal void SetColor(int color)
        {
            if (color >= 0 && color < colorBrushes.Length)
            {
                Brush br = colorBrushes[color];
                if (Background == br)
                {
                    ResetBackground();
                }
                else
                {
                    Background = colorBrushes[color];
                }
            }
        }

        [ObservableProperty]
        private bool given;

        [ObservableProperty]
        private string number = string.Empty;

        [ObservableProperty]
        private bool isHighlight;

        [ObservableProperty]
        private Brush background = Brushes.White;

        [ObservableProperty]
        private Brush foreground = Brushes.DarkGreen;

        [ObservableProperty]
        private Brush rightBrush = Brushes.Transparent;

        [ObservableProperty]
        private Brush bottomBrush = Brushes.Transparent;
    }
}
