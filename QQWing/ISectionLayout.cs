using System.Collections.Generic;

namespace QQWingLib;

public interface ISectionLayout
{
    int Layout { get; set; }

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
