using System;
using System.Collections.Generic;
using System.Linq;

namespace QQWingLib
{
    public interface ISectionLayout
    {
        IEnumerable<int> BottomBoundaries { get; }
        IEnumerable<int> RightBoundaries { get; }
    }

    public class RegularLayout : ISectionLayout
    {
        public IEnumerable<int> BottomBoundaries { get; } = new int[]
        {
            18, 19, 20, 21, 22, 23, 24, 25, 26,
            45, 46, 47, 48, 49, 50, 51, 52, 53
        };

        public IEnumerable<int> RightBoundaries { get; } = new int[]
        {
            2, 11, 20, 29, 38, 47, 56, 65, 74,
            5, 14, 23, 32, 41, 50, 59, 68, 77
        };
    }

    public class IrregularLayout : ISectionLayout
    {
        private readonly int[][] sections = new int[][]
        {
            new int[] {  0,  1,  2,  3, 12, 21, 30, 39, 48 },
            new int[] {  4,  5,  6,  7,  8, 14, 15, 16, 17 },
            new int[] {  9, 10, 11, 18, 19, 20, 28, 29, 38 },
            new int[] { 13, 22, 23, 31, 40, 49, 57, 58, 67 },
            new int[] { 24, 25, 26, 33, 34, 35, 43, 44, 53 },
            new int[] { 27, 36, 37, 45, 46, 47, 54, 55, 56 },
            new int[] { 32, 41, 50, 59, 68, 77, 78, 79, 80 },
            new int[] { 42, 51, 52, 60, 61, 62, 69, 70, 71 },
            new int[] { 63, 64, 65, 66, 72, 73, 74, 75, 76 }
        };

        public IEnumerable<int> BottomBoundaries { get; } = new List<int>
        {
            0, 1, 2, 4, 14, 15, 16, 17, 18, 23, 28,
            33, 38, 43, 48, 53, 54, 55, 56, 57, 67,
            69, 70, 71
        };
        public IEnumerable<int> RightBoundaries { get; } = new List<int>
        {
            3, 11, 12, 13, 20, 21, 23, 27, 29, 30,
            31, 32, 37, 38, 39, 40, 41, 42, 47, 48, 49, 50,
            52, 56, 58, 59, 66, 67, 68, 76
        };

        private readonly List<IrregularSection> sectionList = new();
        private readonly Dictionary<int, IrregularSection> cellToSectionMap = new();

        public IrregularLayout()
        {
            for (int idx = 0; idx < sections.Length; idx++)
            {
                int[] sectionCells = sections[idx];
                IrregularSection section = new(idx, sectionCells);
                sectionList.Add(section);

                for (int jdx = 0; jdx < sectionCells.Length; jdx++)
                {
                    int cell = sectionCells[jdx];
                    cellToSectionMap.Add(cell, section);
                }
            }
        }
    }

    internal class IrregularSection
    {
        private readonly int[] cells;
        private readonly Dictionary<int, List<int>> rowColumnMap = new();
        private readonly Dictionary<int, List<int>> columnRowMap = new();

        public IrregularSection(int sectionIndex, int[] cells)
        {
            if (cells.Length != QQWing.ROW_COL_SEC_SIZE)
                throw new ArgumentOutOfRangeException(nameof(cells));

            Index = sectionIndex;
            this.cells = cells;

            for (int idx = 0; idx < cells.Length; idx++)
            {
                int cell = cells[idx];

                int row = QQWing.CellToRow(cell);
                int col = QQWing.CellToColumn(cell);

                MinRow = Math.Min(row, MinRow);
                MinCol = Math.Min(col, MinCol);

                if (!rowColumnMap.ContainsKey(row))
                {
                    rowColumnMap.Add(row, new());
                }
                if (!rowColumnMap[row].Contains(col))
                {
                    rowColumnMap[row].Add(col);
                }

                if (!columnRowMap.ContainsKey(col))
                {
                    columnRowMap.Add(col, new());
                }
                if (!columnRowMap[col].Contains(row))
                {
                    columnRowMap[col].Add(row);
                }
            }
        }

        public int Index { get; private set; }

        public IEnumerable<int> Cells => cells;

        public int MinRow { get; private set; } = QQWing.ROW_COL_SEC_SIZE;

        public int MinCol { get; private set; } = QQWing.ROW_COL_SEC_SIZE;

        public IEnumerable<int> Rows => rowColumnMap.Keys.OrderBy(x => x);

        public IEnumerable<int> Cols => columnRowMap.Keys.OrderBy(x => x);

        public IEnumerable<int> ColsByRow(int row)
        {
            return rowColumnMap[row].OrderBy(x => x);
        }

        public IEnumerable<int> RowsByCol(int col)
        {
            return columnRowMap[col].OrderBy(x => x);
        }

        public int GetCell(int offset)
        {
            if (offset < 0 || offset >= cells.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return cells[offset];
        }
    }

}
