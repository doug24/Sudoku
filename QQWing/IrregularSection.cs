using System;
using System.Collections.Generic;

namespace QQWingLib;

internal class IrregularSection
{
    private readonly int[] cells;
    // Sorted arrays cached at construction time - no LINQ allocations on hot paths
    private readonly int[] sortedRows;
    private readonly int[] sortedCols;
    private readonly int[][] colsByRow;   // indexed by row (0-8), null if row not in section
    private readonly int[][] rowsByCol;   // indexed by col (0-8), null if col not in section

    public IrregularSection(int sectionIndex, int[] cells)
    {
        if (cells.Length != QQWing.ROW_COL_SEC_SIZE)
            throw new ArgumentOutOfRangeException(nameof(cells));

        Index = sectionIndex;
        this.cells = cells;

        // Build temporary mutable collections first
        var rowColumnMap = new Dictionary<int, List<int>>();
        var columnRowMap = new Dictionary<int, List<int>>();

        for (int idx = 0; idx < cells.Length; idx++)
        {
            int cell = cells[idx];

            int row = QQWing.CellToRow(cell);
            int col = QQWing.CellToColumn(cell);

            MinRow = Math.Min(row, MinRow);
            MinCol = Math.Min(col, MinCol);

            if (!rowColumnMap.TryGetValue(row, out List<int> rowList))
            {
                rowList = [];
                rowColumnMap.Add(row, rowList);
            }
            if (!rowList.Contains(col))
                rowList.Add(col);

            if (!columnRowMap.TryGetValue(col, out List<int> colList))
            {
                colList = [];
                columnRowMap.Add(col, colList);
            }
            if (!colList.Contains(row))
                colList.Add(row);
        }

        // Cache sorted rows and cols arrays
        sortedRows = [.. rowColumnMap.Keys];
        Array.Sort(sortedRows);

        sortedCols = [.. columnRowMap.Keys];
        Array.Sort(sortedCols);

        // Cache colsByRow and rowsByCol as sorted arrays indexed by row/col number
        colsByRow = new int[QQWing.ROW_COL_SEC_SIZE][];
        foreach (var kvp in rowColumnMap)
        {
            int[] arr = [.. kvp.Value];
            Array.Sort(arr);
            colsByRow[kvp.Key] = arr;
        }

        rowsByCol = new int[QQWing.ROW_COL_SEC_SIZE][];
        foreach (var kvp in columnRowMap)
        {
            int[] arr = [.. kvp.Value];
            Array.Sort(arr);
            rowsByCol[kvp.Key] = arr;
        }
    }

    public int Index { get; private set; }

    public IEnumerable<int> Cells => cells;

    public int MinRow { get; private set; } = QQWing.ROW_COL_SEC_SIZE;

    public int MinCol { get; private set; } = QQWing.ROW_COL_SEC_SIZE;

    public IEnumerable<int> Rows => sortedRows;

    public IEnumerable<int> Cols => sortedCols;

    public bool ContainsCol(int col) => rowsByCol[col] != null;

    public bool ContainsRow(int row) => colsByRow[row] != null;

    public IEnumerable<int> ColsByRow(int row) => colsByRow[row];

    public IEnumerable<int> RowsByCol(int col) => rowsByCol[col];

    public int GetCell(int offset)
    {
        if (offset < 0 || offset >= cells.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return cells[offset];
    }
}
