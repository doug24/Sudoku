using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QQWingLib;

public partial class IrregularLayout : ISectionLayout
{
    private readonly static List<int[][]> layouts = [];

    private readonly List<IrregularSection> sectionList = [];
    private readonly Dictionary<int, IrregularSection> cellToSectionMap = [];
    private int layoutIndex = -1;

    public IrregularLayout()
    {
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
                layoutIndex = 0;
            }
            Initialize();
        }
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
        List<int> right = [];
        List<int> bottom = [];
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

    public static int LayoutCount => layouts.Count;

    public IEnumerable<int> BottomBoundaries { get; private set; }

    public IEnumerable<int> RightBoundaries { get; private set; }

    public int CellToSection(int cell)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(cell, QQWing.BOARD_SIZE);

        // dictionary of cell, section
        if (cellToSectionMap.TryGetValue(cell, out IrregularSection section))
        {
            return section.Index;
        }

        throw new ArgumentOutOfRangeException(nameof(cell));
    }

    public int CellToSectionStartCell(int cell)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(cell, QQWing.BOARD_SIZE);

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
}
