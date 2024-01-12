using System;
using System.Collections.Generic;
using System.Linq;

namespace QQWingLib;

internal class IrregularSection
{
    private readonly int[] cells;
    private readonly Dictionary<int, List<int>> rowColumnMap = [];
    private readonly Dictionary<int, List<int>> columnRowMap = [];

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

            if (!rowColumnMap.TryGetValue(row, out List<int> rowList))
            {
                rowList = ([]);
                rowColumnMap.Add(row, rowList);
            }
            if (!rowList.Contains(col))
            {
                rowList.Add(col);
            }

            if (!columnRowMap.TryGetValue(col, out List<int> colList))
            {
                colList = ([]);
                columnRowMap.Add(col, colList);
            }
            if (!colList.Contains(row))
            {
                colList.Add(row);
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
