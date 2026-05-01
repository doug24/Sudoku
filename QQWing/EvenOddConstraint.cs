namespace QQWingLib;

/// <summary>
/// Describes the even/odd shading constraint for an Even/Odd Sudoku puzzle.
/// Shaded cells may only contain even numbers (2, 4, 6, 8) or odd numbers (1, 3, 5, 7, 9)
/// depending on the parity. A special "indicator" cell is shaded differently to signal the
/// puzzle type to the player.
/// </summary>
public class EvenOddConstraint
{
    /// <summary>
    /// Whether the shaded cells must contain even numbers. False means odd numbers.
    /// </summary>
    public bool IsEven { get; }

    /// <summary>
    /// The 0-based cell indices (0–80) of the shaded cells that enforce the parity constraint.
    /// </summary>
    public int[] ShadedCells { get; }

    /// <summary>
    /// The 0-based cell index of the indicator cell (a given cell shaded differently to show even or odd).
    /// </summary>
    public int IndicatorCell { get; }

    public EvenOddConstraint(bool isEven, int[] shadedCells, int indicatorCell)
    {
        IsEven = isEven;
        ShadedCells = shadedCells;
        IndicatorCell = indicatorCell;
    }
}
