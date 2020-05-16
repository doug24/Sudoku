using System;
using System.Collections.Generic;
using System.Linq;
using QQWingLib;

namespace Sudoku
{
    /// <summary>
    /// 
    /// </summary>
    public class CellState
    {
        public CellState(int cellIndex, bool given, int value)
            : this(cellIndex, given, value, new int[0])
        {
        }

        public CellState(int cellIndex, bool given, int value, int[] candidates)
        {
            CellIndex = cellIndex;
            Given = given;
            Value = Math.Max(0, value);
            Candidates = candidates;
        }

        public static CellState Empty => new CellState(-1, false, 0);

        public int CellIndex { get; private set; }
        public bool Given { get; private set; }
        public int Value { get; private set; }
        public int[] Candidates { get; private set; }

        public bool HasValue(int value)
        {
            return Value == value;
        }

        public bool HasCandidate(int value)
        {
            return Candidates.Contains(value);
        }

        public CellState AddCandidate(int candidate)
        {
            List<int> list = Candidates.ToList();
            if (!list.Contains(candidate))
            {
                list.Add(candidate);
                list.Sort();
                return new CellState(CellIndex, Given, Value, list.ToArray());
            }
            else
                return this;
        }

        public CellState RemoveCandidate(int candidate)
        {
            List<int> list = Candidates.ToList();
            if (list.Contains(candidate))
            {
                list.Remove(candidate);
                list.Sort();
                return new CellState(CellIndex, Given, Value, list.ToArray());
            }
            else
                return this;
        }

        public CellState SetValue(int newValue)
        {
            return new CellState(CellIndex, Given, newValue, Candidates);
        }

        public CellState UnsetValue()
        {
            if (!Given)
                return new CellState(CellIndex, Given, 0, Candidates);
            else
                return this;
        }

        public override string ToString()
        {
            int row = QQWing.CellToRow(CellIndex) + 1;
            int col = QQWing.CellToColumn(CellIndex) + 1;
            return $"cell[{row}, {col}] : {ToSnapshotString()}";
        }

        /// <remarks>
        /// Sudoku Snapshot
        /// This format contains a line for each row in the grid.
        /// A comma separates the cells. For unsolved cells, the 
        /// candidates are listed in parenthesis without separating space. 
        /// Solved cells are preceded by u when they are placed 
        /// by the user, the givens have no prefix.
        /// </remarks>
        public string ToSnapshotString()
        {
            return Given ? Value.ToString()
                : Value > 0 ? "u" + Value.ToString()
                : $"c{string.Join(string.Empty, Candidates)}";
        }

        public static CellState FromSnapshotString(int cellIndex, string text)
        {
            int value;

            if (string.IsNullOrWhiteSpace(text))
            {
                return new CellState(cellIndex, false, 0);
            }
            else if (text.Length == 1)
            {
                value = text[0] - '0';
                return new CellState(cellIndex, true, value, new int[0]);
            }
            else if (text.Length == 2 && text[0] == 'u')
            {
                value = text[1] - '0';
                return new CellState(cellIndex, false, value, new int[0]);
            }
            else if (text.Length > 1 && text[0] == 'c')
            {
                List<int> list = new List<int>();
                foreach (char ch in text.TrimStart('c'))
                {
                    list.Add(ch - '0');
                }
                list.Sort();
                return new CellState(cellIndex, false, 0, list.ToArray());
            }
            return null;
        }
    }
}
