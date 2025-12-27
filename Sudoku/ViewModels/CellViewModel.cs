using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QQWingLib;

namespace Sudoku;

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
        isDarkMode = Properties.Settings.Default.DarkMode;

        HighlightBrush = isDarkMode ? Brushes.Cyan : Brushes.ForestGreen;

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
        if (isDarkMode)
        {
            RightBrush = sectionLayout.RightBoundaries.Contains(CellIndex) ? Brushes.LightBlue : RightBrush = Brushes.Transparent;
            BottomBrush = sectionLayout.BottomBoundaries.Contains(CellIndex) ? Brushes.LightBlue : BottomBrush = Brushes.Transparent;
        }
        else
        {
            RightBrush = sectionLayout.RightBoundaries.Contains(CellIndex) ? Brushes.DarkViolet : RightBrush = Brushes.Transparent;
            BottomBrush = sectionLayout.BottomBoundaries.Contains(CellIndex) ? Brushes.DarkViolet : BottomBrush = Brushes.Transparent;
        }

        brushes = isDarkMode ? darkBrushes : lightBrushes;
        ColorBrushes = isDarkMode ? darkColorBrushes : lightColorBrushes;
        Background = defaultBackground = brushes[Section];
    }

    private bool isDarkMode = false;

    private static Brush[] brushes = [];

    private static readonly Brush[] lightBrushes =
    [
        new SolidColorBrush(Color.FromRgb(255, 250, 247)),//hue= 20
        new SolidColorBrush(Color.FromRgb(235, 246, 255)),//hue=180
        new SolidColorBrush(Color.FromRgb(245, 240, 255)),//hue=260
        new SolidColorBrush(Color.FromRgb(245, 255, 240)),//hue=100
        new SolidColorBrush(Color.FromRgb(255, 240, 245)),//hue=340
        new SolidColorBrush(Color.FromRgb(245, 255, 240)),//hue=100
        new SolidColorBrush(Color.FromRgb(245, 240, 255)),//hue=260
        new SolidColorBrush(Color.FromRgb(235, 246, 255)),//hue=180
        new SolidColorBrush(Color.FromRgb(255, 250, 247)),//hue= 20
    ];

    private static readonly Brush[] darkBrushes =
    [
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C370D")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#594C0A")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#153f70")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#301174")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#002680")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#301174")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#153f70")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#594C0A")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C370D")),
    ];

    // these are the colors used for user-marking multiple colors
    public static Brush[] ColorBrushes { get; private set; } = [];

    private static readonly Brush[] lightColorBrushes =
    [
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C2F0FF")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ACFFAC")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDBF")),
    ];

    private static readonly Brush[] darkColorBrushes =
    [
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#006EB8")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B964B")),
        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DB6600")),
    ];

    public static Brush GetColor(int idx)
    {
        return ColorBrushes[idx];
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

    public ObservableCollection<CandidateViewModel> Candidates { get; } = [];

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
            Foreground = isDarkMode ? Brushes.White : Brushes.Black;
        }
        else
        {
            Value = state.Value;
            Number = state.Value <= 0 ? string.Empty : state.Value.ToString();
            if (colorIncorrect)
            {
                if (isDarkMode)
                {
                    Foreground = Value == Answer ? Brushes.PaleGreen : Brushes.Red;
                }
                else
                {
                    Foreground = Value == Answer ? Brushes.DarkGreen : Brushes.Red;
                }
            }
            else if (isDarkMode)
            {
                Foreground = Brushes.PaleGreen;
            }
            else
            {
                Foreground = Brushes.DarkGreen;
            }
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
            {
                if (isDarkMode)
                {
                    Foreground = Value == Answer ? Brushes.White : Brushes.Red;
                }
                else
                {
                    Foreground = Value == Answer ? Brushes.DarkGreen : Brushes.Red;
                }
            }
            else if (isDarkMode)
            {
                Foreground = Brushes.White;
            }
            else
            {
                Foreground = Brushes.DarkGreen;
            }
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
        if (color >= 0 && color < ColorBrushes.Length)
        {
            Brush br = ColorBrushes[color];
            if (Background == br)
            {
                ResetBackground();
            }
            else
            {
                Background = ColorBrushes[color];
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

    [ObservableProperty]
    private Brush highlightBrush = Brushes.ForestGreen;
}
