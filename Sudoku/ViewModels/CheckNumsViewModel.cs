using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sudoku;

public partial class CheckNumsViewModel : ObservableObject
{
    public CheckNumsViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
        {
            Title = "Exclude";
        }

        for (int idx = 1; idx <= 9; idx++)
        {
            var number = new NumberViewModel(idx);
            number.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NumberViewModel.IsChecked))
                    OnPropertyChanged(nameof(CheckedValues));
            };
            Numbers.Add(number);
        }
    }

    public CheckNumsViewModel(string title)
        : this()
    {
        Title = title;
    }

    public void Set(List<int> values)
    {
        foreach (var num in Numbers)
            num.IsChecked = values.Contains(num.Value);
    }

    public IEnumerable<int> CheckedValues
    {
        get
        {
            for (int idx = Numbers.Count - 1; idx >= 0; idx--)
            {
                if (Numbers[idx].IsChecked)
                    yield return idx + 1;
            }
        }
    }

    [ObservableProperty]
    private string title = string.Empty;

    public ObservableCollection<NumberViewModel> Numbers { get; } = [];

    public ICommand ResetCommand => new RelayCommand(
        p => Reset());

    internal void Reset()
    {
        foreach (var num in Numbers)
            num.IsChecked = false;
    }
}

public partial class NumberViewModel : ObservableObject
{
    public NumberViewModel(int value)
    {
        Value = value;
        Label = value.ToString();
    }

    public int Value { get; private set; }

    [ObservableProperty]
    private string label = string.Empty;

    [ObservableProperty]
    private bool isChecked = false;
}

