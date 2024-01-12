using System.Collections.Generic;
using System.Linq;

namespace QQWingLib;

public class RegularLayout : ISectionLayout
{
    public int Layout { get { return -1; } set { /* do nothing */ } }

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
