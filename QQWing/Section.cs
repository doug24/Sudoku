using System;
using System.Collections.Generic;
using System.Linq;

namespace QQWingLib
{
    public interface ISectionLayout
    {
        IEnumerable<int> BottomBoundaries { get; }
        IEnumerable<int> RightBoundaries { get; }

        /// <summary>
        /// Given the index of a cell (0-80) return the section (0-8) in which it
        /// resides.
        /// </summary>
        int CellToSection(int cell);

        /// <summary>
        /// Given the index of a cell (0-80) return the cell (0-80) that is the
        /// upper left start cell of that section.
        /// </summary>
        int CellToSectionStartCell(int cell);

        /// <summary>
        /// Given a section (0-8) and an offset into that section (0-8) return the
        /// cell (0-80)
        /// </summary>
        int SectionToCell(int section, int offset);

        /// <summary>
        /// Given a section (0-8) return the first cell (0-80) of that section.
        /// </summary>
        int SectionToFirstCell(int section);

        /// <summary>
        /// Given a column (0-8) return the sections that intersect that column
        /// </summary>
        IEnumerable<int> ColumnToSections(int col);

        /// <summary>
        /// Given a row (0-8) return the sections that intersect that row
        /// </summary>
        IEnumerable<int> RowToSections(int row);

        /// <summary>
        /// Given a section (0-8), iterate over all cells in the section
        /// </summary>
        IEnumerable<int> SectionToSectionCells(int section);

        /// <summary>
        /// Given a section (0-8), return the rows in the section
        /// </summary>
        IEnumerable<int> SectionToSectionRows(int section);

        /// <summary>
        /// Given a section (0-8), return the columns in the section
        /// </summary>
        IEnumerable<int> SectionToSectionCols(int section);

        /// <summary>
        /// Given a section (0-8), return the rows in the section
        /// that fall in the given column
        /// </summary>
        IEnumerable<int> SectionToSectionRowsByCol(int section, int col);

        /// <summary>
        /// Given a section (0-8), return the columns in the section 
        /// that fall in the given row
        /// </summary>
        IEnumerable<int> SectionToSectionColsByRow(int section, int row);
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

        /// <summary>
        /// Given the index of a cell (0-80) calculate the section (0-8) in which it
        /// resides.
        /// </summary>
        public int CellToSection(int cell)
        {
            return (cell / QQWing.SEC_GROUP_SIZE * QQWing.GRID_SIZE)
                + (QQWing.CellToColumn(cell) / QQWing.GRID_SIZE);
        }

        /// <summary>
        /// Given the index of a cell (0-80) calculate the cell (0-80) that is the
        /// upper left start cell of that section.
        /// </summary>
        public int CellToSectionStartCell(int cell)
        {
            return (cell / QQWing.SEC_GROUP_SIZE * QQWing.SEC_GROUP_SIZE)
                + (QQWing.CellToColumn(cell) / QQWing.GRID_SIZE * QQWing.GRID_SIZE);
        }

        /// <summary>
        /// Given a section (0-8) and an offset into that section (0-8) calculate the
        /// cell (0-80)
        /// </summary>
        public int SectionToCell(int section, int offset)
        {
            return SectionToFirstCell(section)
                + (offset / QQWing.GRID_SIZE * QQWing.ROW_COL_SEC_SIZE)
                + (offset % QQWing.GRID_SIZE);
        }

        /// <summary>
        /// Given a section (0-8) calculate the first cell (0-80) of that section.
        /// </summary>
        public int SectionToFirstCell(int section)
        {
            return (section % QQWing.GRID_SIZE * QQWing.GRID_SIZE)
                + (section / QQWing.GRID_SIZE * QQWing.SEC_GROUP_SIZE);
        }

        /// <summary>
        /// Given a column (0-8) return the sections that intersect that column
        /// </summary>
        public IEnumerable<int> ColumnToSections(int col)
        {
            for (int idx = 0; idx < QQWing.GRID_SIZE; idx++) 
            {
                yield return col / QQWing.GRID_SIZE + idx * QQWing.GRID_SIZE;
            }
        }

        /// <summary>
        /// Given a row (0-8) return the sections that intersect that row
        /// </summary>
        public IEnumerable<int> RowToSections(int row)
        {
            for (int idx = 0; idx < QQWing.GRID_SIZE; idx++)
            {
                yield return row / QQWing.GRID_SIZE * QQWing.GRID_SIZE + idx;
            }
        }

        /// <summary>
        /// Given the index of a section (0-8), iterate over all cells in the section
        /// </summary>
        public IEnumerable<int> SectionToSectionCells(int section)
        {
            int sectionStart = SectionToFirstCell(section);
            for (int idx = 0; idx < QQWing.GRID_SIZE; idx++)
            {
                for (int jdx = 0; jdx < QQWing.GRID_SIZE; jdx++)
                {
                    yield return sectionStart + idx + (QQWing.ROW_COL_SEC_SIZE * jdx);
                }
            }
        }

        /// <summary>
        /// Given a section (0-8), return the rows in the section
        /// </summary>
        public IEnumerable<int> SectionToSectionRows(int section)
        {
            int secStart = SectionToFirstCell(section);
            int firstRow = QQWing.CellToRow(secStart);
            return Enumerable.Range(firstRow, QQWing.GRID_SIZE);
        }

        /// <summary>
        /// Given a section (0-8), return the columns in the section
        /// </summary>
        public IEnumerable<int> SectionToSectionCols(int section)
        {
            int secStart = SectionToFirstCell(section);
            int firstCol = QQWing.CellToColumn(secStart);
            return Enumerable.Range(firstCol, QQWing.GRID_SIZE);
        }

        /// <summary>
        /// Given a section (0-8), return the rows in the section
        /// that fall in the given column
        /// </summary>
        public IEnumerable<int> SectionToSectionRowsByCol(int section, int col)
        {
            int secStart = SectionToFirstCell(section);
            int firstRow = QQWing.CellToRow(secStart);
            return Enumerable.Range(firstRow, QQWing.GRID_SIZE);
        }

        /// <summary>
        /// Given a section (0-8), return the columns in the section 
        /// that fall in the given row
        /// </summary>
        public IEnumerable<int> SectionToSectionColsByRow(int section, int row)
        {
            int secStart = SectionToFirstCell(section);
            int firstCol = QQWing.CellToColumn(secStart);
            return Enumerable.Range(firstCol, QQWing.GRID_SIZE);
        }
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

        public int CellToSection(int cell)
        {
            if (cell >= QQWing.BOARD_SIZE)
                throw new ArgumentOutOfRangeException(nameof(cell));

            // dictionary of cell, section
            if (cellToSectionMap.TryGetValue(cell, out IrregularSection section))
            {
                return section.Index;
            }

            throw new ArgumentOutOfRangeException(nameof(cell));
        }

        public int CellToSectionStartCell(int cell)
        {
            if (cell >= QQWing.BOARD_SIZE)
                throw new ArgumentOutOfRangeException(nameof(cell));

            // dictionary of cell, section
            if (cellToSectionMap.TryGetValue(cell, out IrregularSection section))
            {
                return section.GetCell(0);
            }

            throw new ArgumentOutOfRangeException(nameof(cell));
        }

        public int SectionToCell(int section, int offset)
        {
            if (section < 0 || section >= QQWing.ROW_COL_SEC_SIZE)
                throw new ArgumentOutOfRangeException(nameof(section));

            if (offset < 0 || offset >= QQWing.ROW_COL_SEC_SIZE)
                throw new ArgumentOutOfRangeException(nameof(section));

            return sectionList[section].GetCell(offset);
        }

        public int SectionToFirstCell(int section)
        {
            if (section < 0 || section >= QQWing.ROW_COL_SEC_SIZE)
                throw new ArgumentOutOfRangeException(nameof(section));

            return sectionList[section].GetCell(0);
        }

        /// <summary>
        /// Given a column (0-8) return the sections that intersect that column
        /// </summary>
        public IEnumerable<int> ColumnToSections(int col)
        {
            return sectionList.Where(s => s.Cols.Contains(col))
                .OrderBy(s => s.Index)
                .Select(s => s.Index);
        }

        /// <summary>
        /// Given a row (0-8) return the sections that intersect that row
        /// </summary>
        public IEnumerable<int> RowToSections(int row)
        {
            return sectionList.Where(s => s.Rows.Contains(row))
                .OrderBy(s => s.Index)
                .Select(s => s.Index);
        }

        public IEnumerable<int> SectionToSectionCells(int section)
        {
            return sectionList[section].Cells;
        }

        /// <summary>
        /// Given a section (0-8), return the rows in the section
        /// </summary>
        public IEnumerable<int> SectionToSectionRows(int section)
        {
            return sectionList[section].Rows;
        }

        /// <summary>
        /// Given a section (0-8), return the columns in the section
        /// </summary>
        public IEnumerable<int> SectionToSectionCols(int section)
        {
            return sectionList[section].Cols;
        }

        /// <summary>
        /// Given a section (0-8), return the rows in the section
        /// that fall in the given row
        /// </summary>
        public IEnumerable<int> SectionToSectionRowsByCol(int section, int col)
        {
            return sectionList[section].RowsByCol(col);
        }

        /// <summary>
        /// Given a section (0-8), return the columns in the section 
        /// that fall in the given column
        /// </summary>
        public IEnumerable<int> SectionToSectionColsByRow(int section, int row)
        {
            return sectionList[section].ColsByRow(row);
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
                //}

                //// the rows and cols are indexes within the section
                //for (int idx = 0; idx < cells.Length; idx++)
                //{
                //    int cell = cells[idx];

                //    int row = QQWing.CellToRow(cell);
                //    int col = QQWing.CellToColumn(cell);

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
