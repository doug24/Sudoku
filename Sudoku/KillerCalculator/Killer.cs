using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sudoku.KillerCalculator;

public static class Killer
{
    public static readonly Dictionary<string, List<List<int>>> Combos = [];

    static Killer()
    {
        LoadCombos();
    }

    private static void LoadCombos()
    {
        string data = string.Empty;
        var assembly = typeof(Killer).Assembly;

        using (var stream = assembly.GetManifestResourceStream("Sudoku.KillerCalculator.killercombos.csv"))
        {
            if (stream != null)
            {
                using StreamReader reader = new(stream);
                data = reader.ReadToEnd();
            }
        }

        var lines = data.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1))
        {
            string[] fields = line.Split([','], StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length > 2)
            {
                string key = fields[0] + "|" + fields[1];

                var items = fields[2].Split(['/'], StringSplitOptions.RemoveEmptyEntries);

                var digits = items.Select(d => Convert.ToInt32(d)).OrderBy(d => d).ToList();

                if (!Combos.ContainsKey(key))
                    Combos.Add(key, []);

                Combos[key].Add(digits);
            }
        }

        DescListSorter ls = new();
        foreach (string key in Combos.Keys)
        {
            var lists = Combos[key];
            lists.Sort(ls);
        }
    }
}

public class DescListSorter : IComparer<List<int>>
{
    public int Compare(List<int>? x, List<int>? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        string a = string.Join("", x.ToArray());
        string b = string.Join("", y.ToArray());

        return a.CompareTo(b);
    }
}

