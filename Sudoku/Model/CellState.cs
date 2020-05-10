using System;
using System.Collections.Generic;
using System.Linq;
using QQWingLib;

namespace Sudoku
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Sudoku Puzzle Progress (.sdx)
    /// This format contains a line for each row in the grid.
    /// A blank separates the cells. For unsolved cells, the 
    /// candidates are listed without separating space. 
    /// Solved cells are preceded by u when they are placed 
    /// by the user, the givens have no prefix.
    ///
    /// 2 679 6789 1 46789 5 469 9 3
    /// 389 5 4 69 689 68 7 1 29
    /// 9 1 679 2 4679 3 4569 8 59
    /// 6 9 2 8 u1 7 3 59 4
    /// 3489 3479 3789 56 2456 46 u1 2579 2579
    /// 1 47 5 3 24 9 8 27 6
    /// 3459 2 39 7 3589 1 59 6 589
    /// 359 8 1 569 3569 6 2 4 579
    /// 7 369 369 4 35689 2 59 359 1
    /// </remarks>
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
            return $"cell[{row}, {col}] : {ToSdxString()}";
        }

        public string ToSdxString()
        {
            return Given ? Value.ToString()
                : Value > 0 ? "u" + Value.ToString()
                : string.Join(string.Empty, Candidates);
        }

        public static CellState FromSdxString(int cellIndex, string text)
        {
            int value;

            if (text.Length == 1)
            {
                value = text[0] - '0';
                return new CellState(cellIndex, true, value, new int[0]);
            }
            else if (text.Length == 2 && text[0] == 'u')
            {
                value = text[1] - '0';
                return new CellState(cellIndex, false, value, new int[0]);
            }
            else if (text.Length > 1)
            {
                List<int> list = new List<int>();
                foreach (char ch in text)
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
