using System;

namespace QQWingLib;

/// <summary>
/// Represents a cage in a Killer Sudoku puzzle: a group of cells whose digits
/// must be unique and sum to a specified target value.
/// </summary>
public sealed class Cage
{
    /// <summary>
    /// Cell indices (0-80) belonging to this cage.
    /// </summary>
    public int[] Cells { get; }

    /// <summary>
    /// Target sum for the digits in this cage.
    /// </summary>
    public int Sum { get; }

    /// <summary>
    /// Number of cells in this cage.
    /// </summary>
    public int Size => Cells.Length;

    /// <summary>
    /// Creates a new cage with the specified cells and target sum.
    /// </summary>
    /// <param name="cells">Cell indices (0-80). Must contain at least one cell and at most nine.</param>
    /// <param name="sum">Target sum. Must be between 1 and 45 inclusive.</param>
    public Cage(int[] cells, int sum)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (cells.Length == 0 || cells.Length > 9)
            throw new ArgumentException("A cage must contain between 1 and 9 cells.", nameof(cells));
        if (sum < 1 || sum > 45)
            throw new ArgumentException("Cage sum must be between 1 and 45.", nameof(sum));

        Cells = (int[])cells.Clone();
        Sum = sum;
    }

    public override string ToString()
    {
        return $"Cage(Sum={Sum}, Size={Size})";
    }
}
