using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Sudoku.KillerCalculator;

namespace Sudoku;

public partial class KillerCalculatorResultViewModel : ObservableObject
{
    public Query Query { get; private set; }
    private bool _inInit;

    public KillerCalculatorResultViewModel()
    {
        Query = new(4, 16);
        if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
        {
            Title = "Size: 4\r\nValue: 16";
            IsSelected = true;
            IncludeExclude = "w/ 4" + Environment.NewLine + "w/o 8";
            Results = "9 4 2 1" + Environment.NewLine + "7 4 3 2" + Environment.NewLine + "6 5 4 1";
        }
    }

    public KillerCalculatorResultViewModel(Query query)
    {
        Query = query;
        SetQuery(query);
    }

    public void SetQuery(Query query)
    {
        _inInit = true;

        if (query.Value > 0)
        {
            Title = $"Size: {query.Size}\r\nValue: {query.Value}";
            IncludeExclude = query.IncludeExclude;

            Results = query.ResultString;
        }
        else
        {
            Title = $"Size:\r\nValue:";
            IncludeExclude = string.Empty;
            Results = string.Empty;
        }

        _inInit = false;
    }

    internal void Clear()
    {
        Title = $"Size:\r\nValue:";
        IncludeExclude = string.Empty;
        Results = string.Empty;

        Query?.Clear();
    }

    [ObservableProperty]
    private bool isSelected = false;

    [ObservableProperty]
    private string title = $"Size:\r\nValue:";

    [ObservableProperty]
    private string includeExclude = string.Empty;

    [ObservableProperty]
    private string results = string.Empty;

    partial void OnResultsChanged(string value)
    {
        if (!_inInit && Query != null)
        {
            Query.ResultString = value;
        }
    }
}
