using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QQWingLib
{
    public interface ISectionLayout
    {
        int LayoutCount { get; }

        int Layout { get; set; }

        void SetRandomLayout();

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
        public int LayoutCount => 1;

        public int Layout { get { return -1; } set { /* do nothing */ } }

        public void SetRandomLayout() { }


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

    public class IrregularLayout : ISectionLayout
    {
        private readonly static Random rand;
        private readonly static List<int[][]> layouts = new();

        private readonly List<IrregularSection> sectionList = new();
        private readonly Dictionary<int, IrregularSection> cellToSectionMap = new();
        private int layoutIndex = -1;

        public IrregularLayout()
        {
            //Layout = rand.Next(0, layouts.Count);
            Layout = layouts.Count - 1;
        }

        public int Layout
        {
            get { return layoutIndex; }
            set
            {
                if (layoutIndex == value)
                    return;

                layoutIndex = value;

                if (layoutIndex < 0 || layoutIndex >= layouts.Count)
                {
                    layoutIndex = rand.Next(0, layouts.Count);
                }
                Initialize();
            }
        }

        public void SetRandomLayout()
        {
            Layout = rand.Next(0, layouts.Count);
        }

        private void Initialize()
        {
            if (layoutIndex < 0 || layoutIndex >= layouts.Count)
            {
                return;
            }
            Debug.WriteLine($"Irregular layout index {Layout}");

            sectionList.Clear();
            cellToSectionMap.Clear();
            RightBoundaries = Enumerable.Empty<int>();
            BottomBoundaries = Enumerable.Empty<int>();

            var sections = layouts[layoutIndex];

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

            SetBoundaries();
        }

        private void SetBoundaries()
        {
            List<int> right = new();
            List<int> bottom = new();
            for (int row = 0; row < QQWing.ROW_COL_SEC_SIZE; row++)
            {
                for (int col = 0; col < QQWing.ROW_COL_SEC_SIZE; col++)
                {
                    int cell = QQWing.RowColumnToCell(row, col);
                    int section = CellToSection(cell);

                    if (col + 1 < QQWing.ROW_COL_SEC_SIZE)
                    {
                        int cellRight = QQWing.RowColumnToCell(row, col + 1);
                        int section2 = CellToSection(cellRight);
                        if (section != section2)
                        {
                            right.Add(cell);
                        }
                    }

                    if (row + 1 < QQWing.ROW_COL_SEC_SIZE)
                    {
                        int cellBelow = QQWing.RowColumnToCell(row + 1, col);
                        int section3 = CellToSection(cellBelow);
                        if (section != section3)
                        {
                            bottom.Add(cell);
                        }
                    }
                }
            }
            right.Sort();
            bottom.Sort();

            RightBoundaries = right;
            BottomBoundaries = bottom;
        }

        public int LayoutCount => layouts.Count;

        public IEnumerable<int> BottomBoundaries { get; private set; }

        public IEnumerable<int> RightBoundaries { get; private set; }

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
            var sec = sectionList[section];
            return sec.RowsByCol(col);
        }

        /// <summary>
        /// Given a section (0-8), return the columns in the section 
        /// that fall in the given column
        /// </summary>
        public IEnumerable<int> SectionToSectionColsByRow(int section, int row)
        {
            var sec = sectionList[section];
            return sec.ColsByRow(row);
        }

        static IrregularLayout()
        {
            rand = new Random();

            layouts.Add(new int[][] // #1
            {
                new int[] {  0,  1,  2,  3, 12, 21, 30, 39, 48 },//1
                new int[] {  4,  5,  6,  7,  8, 14, 15, 16, 17 },//2
                new int[] {  9, 10, 11, 18, 19, 20, 28, 29, 38 },//3
                new int[] { 24, 25, 26, 33, 34, 35, 43, 44, 53 },//5
                new int[] { 13, 22, 23, 31, 40, 49, 57, 58, 67 },//4
                new int[] { 27, 36, 37, 45, 46, 47, 54, 55, 56 },//6
                new int[] { 42, 51, 52, 60, 61, 62, 69, 70, 71 },//8
                new int[] { 63, 64, 65, 66, 72, 73, 74, 75, 76 },//9
                new int[] { 32, 41, 50, 59, 68, 77, 78, 79, 80 },//7
            });

            layouts.Add(new int[][] // #2
            {
                new int[] {  0,  1,  2,  3,  4, 10, 11, 12, 19 },//1
                new int[] {  5,  6,  7, 13, 14, 15, 16, 23, 25 },//2
                new int[] {  8, 17, 26, 35, 43, 44, 53, 62, 71 },//3
                new int[] { 24, 33, 34, 42, 50, 51, 52, 59, 60 },//7
                new int[] { 22, 31, 32, 39, 40, 41, 48, 49, 58 },//6
                new int[] { 20, 21, 28, 29, 30, 38, 46, 47, 56 },//5
                new int[] {  9, 18, 27, 36, 37, 45, 54, 63, 72 },//4
                new int[] { 55, 57, 64, 65, 66, 67, 73, 74, 75 },//8
                new int[] { 61, 68, 69, 70, 76, 77, 78, 79, 80 },//9
            });

            layouts.Add(new int[][] // #3
            {
                new int[] {  0,  1,  9, 10, 11, 18, 20, 21, 22 },//1
                new int[] {  2,  3,  4,  5,  6,  7,  8, 17, 26 },//2
                new int[] { 12, 13, 14, 15, 16, 23, 24, 31, 32 },//3
                new int[] { 19, 27, 28, 37, 38, 46, 47, 48, 55 },//4
                new int[] { 29, 30, 39, 40, 41, 42, 50, 51, 59 },//6
                new int[] { 25, 33, 34, 35, 43, 44, 52, 61, 70 },//5
                new int[] { 49, 57, 58, 67, 68, 73, 74, 75, 76 },//8
                new int[] { 36, 45, 54, 56, 63, 64, 65, 66, 72 },//7
                new int[] { 53, 60, 62, 69, 71, 77, 78, 79, 80 },//9
            });

            layouts.Add(new int[][] // #4
            {
                new int[] {  0,  9, 18, 27, 28, 29, 36, 37, 38 },//1
                new int[] {  1,  2,  3, 10, 11, 12, 19, 20, 21 },//2
                new int[] {  5,  6,  7, 14, 15, 16, 23, 24, 25 },//4 - 3
                new int[] {  8, 17, 26, 32, 33, 34, 35, 41, 50 },//5 - 4
                new int[] {  4, 13, 22, 31, 40, 49, 58, 67, 76 },//3 - 5
                new int[] { 30, 39, 45, 46, 47, 48, 54, 63, 72 },//6
                new int[] { 55, 56, 57, 64, 65, 66, 73, 74, 75 },//8 - 7
                new int[] { 59, 60, 61, 68, 69, 70, 77, 78, 79 },//9 - 8
                new int[] { 42, 43, 44, 51, 52, 53, 62, 71, 80 },//7 - 9
            });

            layouts.Add(new int[][] // #5
            {
                new int[] {  0,  1,  9, 10, 18, 19, 27, 36, 37 },//1
                new int[] {  2,  3,  4,  5,  6, 12, 14, 23, 32 },//2
                new int[] {  7,  8, 15, 16, 17, 25, 26, 34, 35 },//3
                new int[] { 11, 13, 20, 21, 22, 28, 29, 30, 31 },//4
                new int[] { 24, 33, 38, 39, 40, 41, 42, 47, 56 },//5
                new int[] { 49, 50, 51, 52, 58, 59, 60, 67, 69 },//9 - 6
                new int[] { 45, 46, 54, 55, 63, 64, 65, 72, 73 },//7
                new int[] { 48, 57, 66, 68, 74, 75, 76, 77, 78 },//8
                new int[] { 43, 44, 53, 61, 62, 70, 71, 79, 80 },//6 - 9
            });

            layouts.Add(new int[][] // #6
            {
                new int[] {  0,  9, 10, 18, 27, 28, 36, 45, 46 },//1
                new int[] {  1,  2,  3,  4,  5,  6, 11, 12, 15 },//2
                new int[] {  7,  8, 16, 17, 24, 25, 26, 32, 33 },//3
                new int[] { 13, 14, 19, 20, 21, 22, 23, 29, 30 },//4
                new int[] { 31, 37, 38, 39, 40, 41, 42, 43, 49 },//5
                new int[] { 50, 51, 57, 58, 59, 60, 61, 66, 67 },//8 - 6
                new int[] { 47, 48, 54, 55, 56, 63, 64, 72, 73 },//7
                new int[] { 65, 68, 69, 74, 75, 76, 77, 78, 79 },//9 - 8
                new int[] { 34, 35, 44, 52, 53, 62, 70, 71, 80 },//6 - 9
            });

            layouts.Add(new int[][] // #7
            {
                new int[] {  0,  1,  2,  3,  4,  9, 12, 21, 22 },//1
                new int[] {  5,  6,  7,  8, 13, 14, 16, 17, 23 },//2
                new int[] { 15, 24, 25, 26, 33, 35, 44, 53, 62 },//4 - 3
                new int[] { 10, 11, 19, 20, 28, 29, 30, 31, 32 },//3 - 4
                new int[] { 34, 37, 38, 39, 40, 41, 42, 43, 46 },//6 - 5
                new int[] { 48, 49, 50, 51, 52, 60, 61, 69, 70 },//7 - 6
                new int[] { 18, 27, 36, 45, 47, 54, 55, 56, 65 },//5 - 7
                new int[] { 57, 63, 64, 66, 67, 72, 73, 74, 75 },//8
                new int[] { 58, 59, 68, 71, 76, 77, 78, 79, 80 },//9
            });

            layouts.Add(new int[][] // #8
            {
                new int[] {  0,  1,  2,  9, 10, 11, 12, 18, 20 },//1
                new int[] {  3,  4,  5, 13, 14, 15, 21, 22, 30 },//2
                new int[] {  6,  7,  8, 16, 17, 24, 25, 26, 34 },//3
                new int[] { 19, 27, 28, 29, 36, 37, 38, 45, 47 },//4
                new int[] { 23, 31, 32, 39, 40, 41, 48, 49, 57 },//5
                new int[] { 33, 35, 42, 43, 44, 51, 52, 53, 61 },//6
                new int[] { 46, 54, 55, 56, 63, 64, 72, 73, 74 },//7
                new int[] { 50, 58, 59, 65, 66, 67, 75, 76, 77 },//8
                new int[] { 60, 62, 68, 69, 70, 71, 78, 79, 80 },//9
            });

            layouts.Add(new int[][] // #9
            {
                new int[] {  0,  9, 10, 18, 19, 20, 27, 28, 29 },//1
                new int[] {  1,  2,  3, 11, 12, 21, 22, 30, 31 },//2
                new int[] {  4,  5,  6,  7,  8, 13, 14, 15, 23 },//3
                new int[] { 16, 17, 24, 25, 26, 33, 34, 35, 44 },//4
                new int[] { 32, 37, 38, 39, 40, 41, 42, 43, 48 },//5
                new int[] { 36, 45, 46, 47, 54, 55, 56, 63, 64 },//6
                new int[] { 57, 65, 66, 67, 72, 73, 74, 75, 76 },//9 - 7
                new int[] { 49, 50, 58, 59, 68, 69, 77, 78, 79 },//7 - 8
                new int[] { 51, 52, 53, 60, 61, 62, 70, 71, 80 },//8 - 9
            });

            layouts.Add(new int[][] // #10
            {
                new int[] {  0,  1,  2,  3,  4,  9, 18, 27, 36 },//1
                new int[] { 10, 11, 12, 19, 20, 28, 37, 45, 46 },//2
                new int[] {  5,  6,  7,  8, 13, 14, 17, 22, 23 },//3
                new int[] { 15, 16, 24, 25, 26, 32, 33, 42, 51 },//4
                new int[] { 21, 30, 31, 39, 40, 41, 49, 50, 59 },//5
                new int[] { 29, 38, 47, 48, 54, 55, 56, 64, 65 },//6
                new int[] { 57, 58, 63, 66, 67, 72, 73, 74, 75 },//9 - 7
                new int[] { 34, 35, 43, 52, 60, 61, 68, 69, 70 },//7 - 8
                new int[] { 44, 53, 62, 71, 76, 77, 78, 79, 80 },//8 - 9
            });

            layouts.Add(new int[][] // #11
            {
                new int[] {  0,  1,  2,  3,  4,  9, 13, 18, 22 },//1
                new int[] {  5,  6,  7,  8, 14, 15, 17, 23, 24 },//2
                new int[] { 16, 25, 26, 34, 35, 43, 44, 52, 53 },//4 - 3
                new int[] { 10, 11, 12, 19, 20, 21, 29, 38, 47 },//3 - 4
                new int[] { 30, 31, 32, 39, 40, 41, 48, 49, 50 },//6 - 5
                new int[] { 33, 42, 51, 59, 60, 61, 68, 69, 70 },//7 - 6
                new int[] { 27, 28, 36, 37, 45, 46, 54, 55, 64 },//5 - 7
                new int[] { 56, 57, 63, 65, 66, 72, 73, 74, 75 },//8
                new int[] { 58, 62, 67, 71, 76, 77, 78, 79, 80 },//9
            });

            layouts.Add(new int[][] // #12
            {
                new int[] {  0,  1,  9, 10, 18, 19, 27, 28, 29 },//1
                new int[] {  2,  3,  4,  5,  6, 15, 24, 32, 33 },//2
                new int[] {  7,  8, 16, 17, 25, 26, 34, 35, 44 },//3
                new int[] { 11, 12, 13, 14, 20, 21, 22, 23, 31 },//4
                new int[] { 30, 37, 38, 39, 40, 41, 42, 43, 50 },//5
                new int[] { 49, 57, 58, 59, 60, 66, 67, 68, 69 },//8 - 6
                new int[] { 36, 45, 46, 54, 55, 63, 64, 72, 73 },//6 - 7
                new int[] { 47, 48, 56, 65, 74, 75, 76, 77, 78 },//7 - 8
                new int[] { 51, 52, 53, 61, 62, 70, 71, 79, 80 },//9
            });

            layouts.Add(new int[][] // #13
            {
                new int[] {  0,  1,  2,  3,  4, 11, 20, 29, 38 },//1
                new int[] {  5,  6,  7,  8, 14, 15, 16, 17, 25 },//2
                new int[] {  9, 10, 18, 19, 27, 28, 36, 45, 54 },//3
                new int[] { 12, 13, 21, 22, 23, 24, 33, 34, 43 },//4
                new int[] { 30, 31, 32, 39, 40, 41, 48, 49, 50 },//6 - 5
                new int[] { 37, 46, 47, 56, 57, 58, 59, 67, 68 },//7 - 6
                new int[] { 26, 35, 44, 52, 53, 61, 62, 70, 71 },//5 - 7
                new int[] { 55, 63, 64, 65, 66, 72, 73, 74, 75 },//9 - 8
                new int[] { 42, 51, 60, 69, 76, 77, 78, 79, 80 },//8 - 9
            });

            layouts.Add(new int[][] // #14
            {
                new int[] {  0,  9, 18, 27, 28, 36, 37, 46, 55 },//1
                new int[] {  1,  2, 10, 11, 19, 20, 21, 29, 30 },//2
                new int[] {  5,  6,  7,  8, 15, 16, 17, 26, 35 },//4 - 3
                new int[] {  3,  4, 12, 13, 14, 22, 23, 24, 33 },//3 - 4
                new int[] { 31, 32, 38, 39, 40, 41, 42, 48, 49 },//6 - 5
                new int[] { 47, 56, 57, 58, 66, 67, 68, 76, 77 },//8 - 6
                new int[] { 45, 54, 63, 64, 65, 72, 73, 74, 75 },//7 - 7
                new int[] { 50, 51, 59, 60, 61, 69, 70, 78, 79 },//9 - 8
                new int[] { 25, 34, 43, 44, 52, 53, 62, 71, 80 },//5 - 9
            });
        }
    }
}
