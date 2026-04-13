using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using QQWingLib;
using Sudoku.KillerCalculator;

namespace Sudoku;

public partial class KillerCalculatorViewModel : ObservableObject
{
    private readonly List<Query> queries = [];

    public KillerCalculatorViewModel()
    {
        Include.PropertyChanged += IncludeExclude_PropertyChanged;
        Exclude.PropertyChanged += IncludeExclude_PropertyChanged;
    }

    public void Reset()
    {
        queries.Clear();
        Selections.Clear();
        SelectedIndex = -1;
        Clear();
    }

    public void Initialize(List<Cage> cages)
    {
        queries.Clear();
        Selections.Clear();

        HashSet<string> seen = [];
        List<Query> newQueries = [];
        for (int idx = 0; idx < cages.Count; idx++)
        {
            var cage = cages[idx];
            if (cage.Cells.Length > 1)
            {
                if (seen.Contains(cage.ToString()))
                {
                    continue;
                }
                seen.Add(cage.ToString());

                var query = new Query(cage.Cells.Length, cage.Sum);
                newQueries.Add(query);
            }
        }

        queries.AddRange(newQueries.OrderBy(q => q.Size).ThenBy(q => q.Value));

        foreach (var query in queries)
        {
            Selections.Add(query.ToString());
        }
    }

    private bool skipPropertyChanged = false;

    public void Select(int index)
    {
        if (index >= 0 && index < queries.Count)
        {
            SelectedIndex = index;
            var query = queries[SelectedIndex];

            skipPropertyChanged = true;
            Include.Set(query.Include);
            Exclude.Set(query.Exclude);
            skipPropertyChanged = false;

            Evaluate();
        }
    }

    private void IncludeExclude_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!skipPropertyChanged && e.PropertyName == nameof(CheckNumsViewModel.CheckedValues))
        {
            if (SelectedIndex >= 0 && SelectedIndex < queries.Count)
            {
                var query = queries[SelectedIndex];
                query.Include = [.. Include.CheckedValues];
                query.Exclude = [.. Exclude.CheckedValues];
                Evaluate();
            }
        }
    }

    public ObservableCollection<string> Selections { get; } = [];

    [ObservableProperty]
    private int selectedIndex = -1;

    partial void OnSelectedIndexChanged(int value)
    {
        Select(value);
    }

    [ObservableProperty]
    private CheckNumsViewModel exclude = new("Exclude");

    [ObservableProperty]
    private CheckNumsViewModel include = new("Include");

    [ObservableProperty]
    private KillerCalculatorResultViewModel results = new();

    public ICommand GoCommand => new RelayCommand(
        p => Evaluate());

    public ICommand ClearCommand => new RelayCommand(
        p => Clear());

    private void Clear()
    {
        Results.Clear();
        Include.Reset();
        Exclude.Reset();
    }

    private void Evaluate()
    {
        if (SelectedIndex >= 0 && SelectedIndex < queries.Count)
        {
            var query = queries[SelectedIndex];

            if (Killer.Combos.TryGetValue(query.Key, out List<List<int>>? lists))
            {
                query.Evaluate(lists);
            }

            Results.SetQuery(query);
        }
    }
}
