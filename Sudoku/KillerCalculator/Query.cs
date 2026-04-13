using System;
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.KillerCalculator;

public class Query(int size, int value)
{
    internal void Clear()
    {
        Size = 0;
        Value = 0;
        Exclude.Clear();
        Include.Clear();
        ResultString = string.Empty;
        Results.Clear();
    }

    public int Size { get; private set; } = size;
    public int Value { get; private set; } = value;
    public List<int> Exclude { get; set; } = [];
    public List<int> Include { get; set; } = [];

    public string ResultString { get; set; } = string.Empty;
    public List<List<int>> Results { get; private set; } = [];

    public string Key
    {
        get { return Size.ToString() + "|" + Value.ToString(); }
    }

    public string IncludeExclude
    {
        get
        {
            string text = string.Empty;

            if (Include.Count != 0)
            {
                text += "w/ " + string.Join("", Include.OrderBy(i => i));
            }
            if (Exclude.Count != 0)
            {
                if (!string.IsNullOrWhiteSpace(text))
                    text += Environment.NewLine;
                text += "w/o " + string.Join("", Exclude.OrderBy(i => i));
            }

            return text;
        }
    }

    public override string ToString()
    {
        return $"{Size}-{Value}";
    }

    internal void Evaluate(List<List<int>> lists)
    {
        ResultString = string.Empty;
        Results.Clear();

        foreach (var list in lists)
        {
            bool include = true;

            if (Include.Count > 0)
                if (!Include.All(r => list.Contains(r)))
                    include = false;

            if (Exclude.Count != 0)
                if (list.Intersect(Exclude).Any())
                    include = false;

            if (include)
            {
                Results.Add(list);
                ResultString += string.Join(" ", list) + Environment.NewLine;
            }
        }
    }
}
