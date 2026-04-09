using System;
using System.Collections.Generic;

namespace QQWingLib;

/// <summary>
/// Solver and verifier for Killer Sudoku puzzles. Enforces standard Sudoku
/// constraints (row, column, section uniqueness) plus cage constraints
/// (digits in each cage must be unique and sum to the cage's target value).
/// </summary>
public class KillerSolver
{
    private const int SIZE = 9;
    private const int BOARD_SIZE = 81;
    private const int ALL_CANDIDATES = (1 << SIZE) - 1;

    private readonly Cage[] _cages;
    private readonly int[] _cellToCage;

    /// <summary>
    /// Creates a new KillerSolver for the given set of cages.
    /// </summary>
    /// <param name="cages">
    /// The cages defining the Killer Sudoku puzzle. Each cell (0-80) must belong
    /// to at most one cage. Cells not covered by any cage are solved using standard
    /// Sudoku constraints only.
    /// </param>
    public KillerSolver(IReadOnlyList<Cage> cages)
    {
        ArgumentNullException.ThrowIfNull(cages);
        _cages = new Cage[cages.Count];
        for (int i = 0; i < cages.Count; i++)
            _cages[i] = cages[i];

        _cellToCage = new int[BOARD_SIZE];
        Array.Fill(_cellToCage, -1);

        for (int i = 0; i < _cages.Length; i++)
        {
            foreach (int cell in _cages[i].Cells)
            {
                if (cell < 0 || cell >= BOARD_SIZE)
                    throw new ArgumentException($"Cell index {cell} in cage {i} is out of range (0-80).");
                if (_cellToCage[cell] != -1)
                    throw new ArgumentException($"Cell {cell} belongs to cage {_cellToCage[cell]} and cage {i}.");
                _cellToCage[cell] = i;
            }
        }
    }

    /// <summary>
    /// Returns true if the puzzle has exactly one solution.
    /// </summary>
    public bool HasUniqueSolution()
    {
        return CountSolutions(2) == 1;
    }

    /// <summary>
    /// Returns true if the puzzle has no valid solution.
    /// </summary>
    public bool HasNoSolution()
    {
        return CountSolutions(1) == 0;
    }

    /// <summary>
    /// Count solutions up to the specified limit. Stops early once the limit is reached.
    /// </summary>
    public int CountSolutions(int limit)
    {
        int[] solution = new int[BOARD_SIZE];
        int[] candidates = new int[BOARD_SIZE];
        Array.Fill(candidates, ALL_CANDIDATES);

        if (!Propagate(solution, candidates))
            return 0;

        return CountSolutionsRecursive(solution, candidates, limit);
    }

    /// <summary>
    /// Find and return a valid solution as an 81-element array (values 1-9),
    /// or null if no solution exists.
    /// </summary>
    public int[] Solve()
    {
        int[] solution = new int[BOARD_SIZE];
        int[] candidates = new int[BOARD_SIZE];
        Array.Fill(candidates, ALL_CANDIDATES);

        if (!Propagate(solution, candidates))
            return null;

        return SolveRecursive(solution, candidates);
    }

    /// <summary>
    /// Assess the difficulty of this puzzle by solving it with layered strategies.
    /// Returns the difficulty level based on the most advanced strategy required.
    /// Returns <see cref="Difficulty.UNKNOWN"/> if the puzzle has no solution.
    /// </summary>
    public Difficulty GetDifficulty()
    {
        int[] solution = new int[BOARD_SIZE];
        int[] candidates = new int[BOARD_SIZE];
        Array.Fill(candidates, ALL_CANDIDATES);

        Difficulty maxDifficulty = Difficulty.SIMPLE;

        if (!PropagateDifficulty(solution, candidates, ref maxDifficulty))
            return Difficulty.UNKNOWN;

        if (IsSolved(solution))
            return maxDifficulty;

        // Could not solve with logic alone — verify solvable with backtracking
        if (SolveRecursive(solution, candidates) != null)
            return Difficulty.EXPERT;

        return Difficulty.UNKNOWN;
    }

    #region Constraint Propagation

    /// <summary>
    /// Apply constraint propagation until no more progress or a contradiction is found.
    /// Handles naked singles, hidden singles in rows/columns/sections, and cage sum pruning.
    /// Returns false if a contradiction is detected.
    /// </summary>
    private bool Propagate(int[] solution, int[] candidates)
    {
        bool progress = true;
        while (progress)
        {
            progress = false;

            // Naked singles: cells with exactly one candidate
            for (int cell = 0; cell < BOARD_SIZE; cell++)
            {
                if (solution[cell] != 0) continue;
                if (candidates[cell] == 0) return false;
                if (IsSingleBit(candidates[cell]))
                {
                    if (!PlaceValue(solution, candidates, cell, BitToValue(candidates[cell])))
                        return false;
                    progress = true;
                }
            }

            // Hidden singles in rows, columns, sections
            if (!HiddenSinglesInRows(solution, candidates, ref progress)) return false;
            if (!HiddenSinglesInColumns(solution, candidates, ref progress)) return false;
            if (!HiddenSinglesInSections(solution, candidates, ref progress)) return false;

            // Cage validation and sum constraint pruning
            for (int ci = 0; ci < _cages.Length; ci++)
            {
                if (!ValidateCage(solution, ci))
                    return false;

                if (PruneCageCandidates(solution, candidates, ci))
                {
                    progress = true;
                    for (int cell = 0; cell < BOARD_SIZE; cell++)
                    {
                        if (solution[cell] == 0 && candidates[cell] == 0)
                            return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Apply constraint propagation using layered strategies, tracking the most
    /// advanced strategy required. Tries strategies in order of difficulty and
    /// restarts from the simplest level after each successful elimination.
    /// Returns false if a contradiction is detected.
    /// </summary>
    private bool PropagateDifficulty(int[] solution, int[] candidates, ref Difficulty maxDifficulty)
    {
        bool progress = true;
        while (progress)
        {
            progress = false;

            // --- SIMPLE: Naked singles + cage sum pruning ---
            for (int cell = 0; cell < BOARD_SIZE; cell++)
            {
                if (solution[cell] != 0) continue;
                if (candidates[cell] == 0) return false;
                if (IsSingleBit(candidates[cell]))
                {
                    if (!PlaceValue(solution, candidates, cell, BitToValue(candidates[cell])))
                        return false;
                    progress = true;
                }
            }
            if (progress) continue;

            for (int ci = 0; ci < _cages.Length; ci++)
            {
                if (!ValidateCage(solution, ci))
                    return false;
                if (PruneCageCandidates(solution, candidates, ci))
                {
                    progress = true;
                    for (int cell = 0; cell < BOARD_SIZE; cell++)
                    {
                        if (solution[cell] == 0 && candidates[cell] == 0)
                            return false;
                    }
                }
            }
            if (progress) continue;

            // --- EASY: Hidden singles ---
            bool strategyProgress = false;

            if (!HiddenSinglesInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.EASY) maxDifficulty = Difficulty.EASY; progress = true; continue; }

            if (!HiddenSinglesInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.EASY) maxDifficulty = Difficulty.EASY; progress = true; continue; }

            if (!HiddenSinglesInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.EASY) maxDifficulty = Difficulty.EASY; progress = true; continue; }

            // --- INTERMEDIATE: Naked pairs, hidden pairs, pointing pairs, box-line reduction ---
            if (!NakedPairsInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!NakedPairsInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!NakedPairsInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!HiddenPairsInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!HiddenPairsInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!HiddenPairsInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!PointingPairs(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!BoxLineReduction(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }
        }

        return true;
    }

    /// <summary>
    /// Place a value in a cell and eliminate it from all peer cells
    /// (same row, column, section, and cage). Returns false if a contradiction is detected.
    /// </summary>
    private bool PlaceValue(int[] solution, int[] candidates, int cell, int value)
    {
        if (solution[cell] != 0)
            return solution[cell] == value;

        int bit = 1 << (value - 1);
        if ((candidates[cell] & bit) == 0)
            return false;

        solution[cell] = value;
        candidates[cell] = 0;

        // Eliminate from row
        int row = cell / SIZE;
        for (int col = 0; col < SIZE; col++)
        {
            int peer = row * SIZE + col;
            if (peer != cell)
            {
                candidates[peer] &= ~bit;
                if (solution[peer] == 0 && candidates[peer] == 0)
                    return false;
            }
        }

        // Eliminate from column
        int column = cell % SIZE;
        for (int r = 0; r < SIZE; r++)
        {
            int peer = r * SIZE + column;
            if (peer != cell)
            {
                candidates[peer] &= ~bit;
                if (solution[peer] == 0 && candidates[peer] == 0)
                    return false;
            }
        }

        // Eliminate from section
        int section = QQWing.SectionLayout.CellToSection(cell);
        foreach (int peer in QQWing.SectionLayout.SectionToSectionCells(section))
        {
            if (peer != cell)
            {
                candidates[peer] &= ~bit;
                if (solution[peer] == 0 && candidates[peer] == 0)
                    return false;
            }
        }

        // Eliminate from cage (digit uniqueness within cage)
        int cageIdx = _cellToCage[cell];
        if (cageIdx >= 0)
        {
            foreach (int peer in _cages[cageIdx].Cells)
            {
                if (peer != cell)
                {
                    candidates[peer] &= ~bit;
                    if (solution[peer] == 0 && candidates[peer] == 0)
                        return false;
                }
            }
        }

        return true;
    }

    private bool HiddenSinglesInRows(int[] solution, int[] candidates, ref bool progress)
    {
        for (int row = 0; row < SIZE; row++)
        {
            for (int val = 1; val <= SIZE; val++)
            {
                int bit = 1 << (val - 1);
                int count = 0;
                int lastCell = -1;
                bool placed = false;
                for (int col = 0; col < SIZE; col++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == val) { placed = true; break; }
                    if ((candidates[cell] & bit) != 0) { count++; lastCell = cell; }
                }
                if (placed) continue;
                if (count == 0) return false;
                if (count == 1)
                {
                    if (!PlaceValue(solution, candidates, lastCell, val))
                        return false;
                    progress = true;
                }
            }
        }
        return true;
    }

    private bool HiddenSinglesInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        for (int col = 0; col < SIZE; col++)
        {
            for (int val = 1; val <= SIZE; val++)
            {
                int bit = 1 << (val - 1);
                int count = 0;
                int lastCell = -1;
                bool placed = false;
                for (int row = 0; row < SIZE; row++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == val) { placed = true; break; }
                    if ((candidates[cell] & bit) != 0) { count++; lastCell = cell; }
                }
                if (placed) continue;
                if (count == 0) return false;
                if (count == 1)
                {
                    if (!PlaceValue(solution, candidates, lastCell, val))
                        return false;
                    progress = true;
                }
            }
        }
        return true;
    }

    private bool HiddenSinglesInSections(int[] solution, int[] candidates, ref bool progress)
    {
        for (int sec = 0; sec < SIZE; sec++)
        {
            for (int val = 1; val <= SIZE; val++)
            {
                int bit = 1 << (val - 1);
                int count = 0;
                int lastCell = -1;
                bool placed = false;
                foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
                {
                    if (solution[cell] == val) { placed = true; break; }
                    if ((candidates[cell] & bit) != 0) { count++; lastCell = cell; }
                }
                if (placed) continue;
                if (count == 0) return false;
                if (count == 1)
                {
                    if (!PlaceValue(solution, candidates, lastCell, val))
                        return false;
                    progress = true;
                }
            }
        }
        return true;
    }

    #endregion

    #region Intermediate Strategies

    private static bool NakedPairsInRows(int[] solution, int[] candidates, ref bool progress)
    {
        for (int row = 0; row < SIZE; row++)
        {
            for (int col1 = 0; col1 < SIZE; col1++)
            {
                int cell1 = row * SIZE + col1;
                if (solution[cell1] != 0 || PopCount(candidates[cell1]) != 2) continue;

                for (int col2 = col1 + 1; col2 < SIZE; col2++)
                {
                    int cell2 = row * SIZE + col2;
                    if (solution[cell2] != 0 || candidates[cell2] != candidates[cell1]) continue;

                    int pairMask = candidates[cell1];
                    for (int col3 = 0; col3 < SIZE; col3++)
                    {
                        int cell3 = row * SIZE + col3;
                        if (cell3 == cell1 || cell3 == cell2 || solution[cell3] != 0) continue;
                        int before = candidates[cell3];
                        candidates[cell3] &= ~pairMask;
                        if (candidates[cell3] != before) progress = true;
                        if (candidates[cell3] == 0) return false;
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool NakedPairsInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        for (int col = 0; col < SIZE; col++)
        {
            for (int row1 = 0; row1 < SIZE; row1++)
            {
                int cell1 = row1 * SIZE + col;
                if (solution[cell1] != 0 || PopCount(candidates[cell1]) != 2) continue;

                for (int row2 = row1 + 1; row2 < SIZE; row2++)
                {
                    int cell2 = row2 * SIZE + col;
                    if (solution[cell2] != 0 || candidates[cell2] != candidates[cell1]) continue;

                    int pairMask = candidates[cell1];
                    for (int row3 = 0; row3 < SIZE; row3++)
                    {
                        int cell3 = row3 * SIZE + col;
                        if (cell3 == cell1 || cell3 == cell2 || solution[cell3] != 0) continue;
                        int before = candidates[cell3];
                        candidates[cell3] &= ~pairMask;
                        if (candidates[cell3] != before) progress = true;
                        if (candidates[cell3] == 0) return false;
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool NakedPairsInSections(int[] solution, int[] candidates, ref bool progress)
    {
        for (int sec = 0; sec < SIZE; sec++)
        {
            int pairCount = 0;
            int[] pairCells = new int[SIZE];
            foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
            {
                if (solution[cell] == 0 && PopCount(candidates[cell]) == 2)
                    pairCells[pairCount++] = cell;
            }

            for (int a = 0; a < pairCount; a++)
            {
                for (int b = a + 1; b < pairCount; b++)
                {
                    if (candidates[pairCells[a]] != candidates[pairCells[b]]) continue;

                    int pairMask = candidates[pairCells[a]];
                    foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
                    {
                        if (cell == pairCells[a] || cell == pairCells[b] || solution[cell] != 0) continue;
                        int before = candidates[cell];
                        candidates[cell] &= ~pairMask;
                        if (candidates[cell] != before) progress = true;
                        if (candidates[cell] == 0) return false;
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool HiddenPairsInRows(int[] solution, int[] candidates, ref bool progress)
    {
        for (int row = 0; row < SIZE; row++)
        {
            int[] valCols = new int[SIZE + 1];
            for (int val = 1; val <= SIZE; val++)
            {
                bool placed = false;
                for (int col = 0; col < SIZE; col++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == val) { placed = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & (1 << (val - 1))) != 0)
                        valCols[val] |= 1 << col;
                }
                if (placed) valCols[val] = 0;
            }

            for (int v1 = 1; v1 <= SIZE; v1++)
            {
                if (PopCount(valCols[v1]) != 2) continue;
                for (int v2 = v1 + 1; v2 <= SIZE; v2++)
                {
                    if (valCols[v2] != valCols[v1]) continue;
                    int keepMask = (1 << (v1 - 1)) | (1 << (v2 - 1));
                    int colMask = valCols[v1];
                    for (int col = 0; col < SIZE; col++)
                    {
                        if ((colMask & (1 << col)) == 0) continue;
                        int cell = row * SIZE + col;
                        int before = candidates[cell];
                        candidates[cell] &= keepMask;
                        if (candidates[cell] != before) progress = true;
                        if (candidates[cell] == 0) return false;
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool HiddenPairsInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        for (int col = 0; col < SIZE; col++)
        {
            int[] valRows = new int[SIZE + 1];
            for (int val = 1; val <= SIZE; val++)
            {
                bool placed = false;
                for (int row = 0; row < SIZE; row++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == val) { placed = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & (1 << (val - 1))) != 0)
                        valRows[val] |= 1 << row;
                }
                if (placed) valRows[val] = 0;
            }

            for (int v1 = 1; v1 <= SIZE; v1++)
            {
                if (PopCount(valRows[v1]) != 2) continue;
                for (int v2 = v1 + 1; v2 <= SIZE; v2++)
                {
                    if (valRows[v2] != valRows[v1]) continue;
                    int keepMask = (1 << (v1 - 1)) | (1 << (v2 - 1));
                    int rowMask = valRows[v1];
                    for (int row = 0; row < SIZE; row++)
                    {
                        if ((rowMask & (1 << row)) == 0) continue;
                        int cell = row * SIZE + col;
                        int before = candidates[cell];
                        candidates[cell] &= keepMask;
                        if (candidates[cell] != before) progress = true;
                        if (candidates[cell] == 0) return false;
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool HiddenPairsInSections(int[] solution, int[] candidates, ref bool progress)
    {
        for (int sec = 0; sec < SIZE; sec++)
        {
            int[] secCells = new int[SIZE];
            int cellIdx = 0;
            foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
                secCells[cellIdx++] = cell;

            int[] valPositions = new int[SIZE + 1];
            for (int val = 1; val <= SIZE; val++)
            {
                bool placed = false;
                for (int i = 0; i < SIZE; i++)
                {
                    int cell = secCells[i];
                    if (solution[cell] == val) { placed = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & (1 << (val - 1))) != 0)
                        valPositions[val] |= 1 << i;
                }
                if (placed) valPositions[val] = 0;
            }

            for (int v1 = 1; v1 <= SIZE; v1++)
            {
                if (PopCount(valPositions[v1]) != 2) continue;
                for (int v2 = v1 + 1; v2 <= SIZE; v2++)
                {
                    if (valPositions[v2] != valPositions[v1]) continue;
                    int keepMask = (1 << (v1 - 1)) | (1 << (v2 - 1));
                    int posMask = valPositions[v1];
                    for (int i = 0; i < SIZE; i++)
                    {
                        if ((posMask & (1 << i)) == 0) continue;
                        int cell = secCells[i];
                        int before = candidates[cell];
                        candidates[cell] &= keepMask;
                        if (candidates[cell] != before) progress = true;
                        if (candidates[cell] == 0) return false;
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool PointingPairs(int[] solution, int[] candidates, ref bool progress)
    {
        for (int sec = 0; sec < SIZE; sec++)
        {
            for (int val = 1; val <= SIZE; val++)
            {
                int bit = 1 << (val - 1);
                int row = -1, col = -1;
                bool sameRow = true, sameCol = true;
                bool placed = false;
                int count = 0;

                foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
                {
                    if (solution[cell] == val) { placed = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        int r = cell / SIZE;
                        int c = cell % SIZE;
                        if (count == 0) { row = r; col = c; }
                        else { if (r != row) sameRow = false; if (c != col) sameCol = false; }
                        count++;
                    }
                }
                if (placed || count < 2) continue;

                if (sameRow)
                {
                    for (int c = 0; c < SIZE; c++)
                    {
                        int cell = row * SIZE + c;
                        if (QQWing.SectionLayout.CellToSection(cell) == sec) continue;
                        if (solution[cell] != 0) continue;
                        int before = candidates[cell];
                        candidates[cell] &= ~bit;
                        if (candidates[cell] != before) progress = true;
                        if (candidates[cell] == 0) return false;
                    }
                    if (progress) return true;
                }

                if (sameCol)
                {
                    for (int r = 0; r < SIZE; r++)
                    {
                        int cell = r * SIZE + col;
                        if (QQWing.SectionLayout.CellToSection(cell) == sec) continue;
                        if (solution[cell] != 0) continue;
                        int before = candidates[cell];
                        candidates[cell] &= ~bit;
                        if (candidates[cell] != before) progress = true;
                        if (candidates[cell] == 0) return false;
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool BoxLineReduction(int[] solution, int[] candidates, ref bool progress)
    {
        for (int row = 0; row < SIZE; row++)
        {
            for (int val = 1; val <= SIZE; val++)
            {
                int bit = 1 << (val - 1);
                int sec = -1;
                bool sameSec = true;
                bool placed = false;
                int count = 0;

                for (int col = 0; col < SIZE; col++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == val) { placed = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        int s = QQWing.SectionLayout.CellToSection(cell);
                        if (count == 0) sec = s;
                        else if (s != sec) sameSec = false;
                        count++;
                    }
                }
                if (placed || count < 2 || !sameSec) continue;

                foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
                {
                    if (cell / SIZE == row) continue;
                    if (solution[cell] != 0) continue;
                    int before = candidates[cell];
                    candidates[cell] &= ~bit;
                    if (candidates[cell] != before) progress = true;
                    if (candidates[cell] == 0) return false;
                }
                if (progress) return true;
            }
        }

        for (int col = 0; col < SIZE; col++)
        {
            for (int val = 1; val <= SIZE; val++)
            {
                int bit = 1 << (val - 1);
                int sec = -1;
                bool sameSec = true;
                bool placed = false;
                int count = 0;

                for (int row = 0; row < SIZE; row++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == val) { placed = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        int s = QQWing.SectionLayout.CellToSection(cell);
                        if (count == 0) sec = s;
                        else if (s != sec) sameSec = false;
                        count++;
                    }
                }
                if (placed || count < 2 || !sameSec) continue;

                foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
                {
                    if (cell % SIZE == col) continue;
                    if (solution[cell] != 0) continue;
                    int before = candidates[cell];
                    candidates[cell] &= ~bit;
                    if (candidates[cell] != before) progress = true;
                    if (candidates[cell] == 0) return false;
                }
                if (progress) return true;
            }
        }

        return true;
    }

    #endregion

    #region Cage Constraints

    /// <summary>
    /// Check basic cage feasibility: placed values don't exceed the target,
    /// and a completed cage sums exactly to its target.
    /// </summary>
    private bool ValidateCage(int[] solution, int cageIndex)
    {
        Cage cage = _cages[cageIndex];
        int placedSum = 0;
        int unfilledCount = 0;

        foreach (int cell in cage.Cells)
        {
            if (solution[cell] != 0)
                placedSum += solution[cell];
            else
                unfilledCount++;
        }

        if (placedSum > cage.Sum)
            return false;
        if (unfilledCount == 0 && placedSum != cage.Sum)
            return false;
        if (unfilledCount > 0 && placedSum == cage.Sum)
            return false;

        return true;
    }

    /// <summary>
    /// Prune candidates in a cage based on sum feasibility. Enumerates all valid
    /// digit combinations for unfilled cells and eliminates candidates that don't
    /// appear in any valid combination. Returns true if any candidates were eliminated.
    /// </summary>
    private bool PruneCageCandidates(int[] solution, int[] candidates, int cageIndex)
    {
        Cage cage = _cages[cageIndex];
        int remainingSum = cage.Sum;
        int usedDigitsMask = 0;
        int unfilledCount = 0;

        foreach (int cell in cage.Cells)
        {
            if (solution[cell] != 0)
            {
                remainingSum -= solution[cell];
                usedDigitsMask |= 1 << (solution[cell] - 1);
            }
            else
            {
                unfilledCount++;
            }
        }

        if (unfilledCount == 0 || remainingSum <= 0)
            return false;

        // Collect unfilled cells and their candidates (excluding digits already used in cage)
        int[] unfilledCells = new int[unfilledCount];
        int[] cellCands = new int[unfilledCount];
        int idx = 0;
        foreach (int cell in cage.Cells)
        {
            if (solution[cell] == 0)
            {
                unfilledCells[idx] = cell;
                cellCands[idx] = candidates[cell] & ~usedDigitsMask;
                idx++;
            }
        }

        // Find all feasible digit assignments via recursive enumeration
        int[] feasibleMasks = new int[unfilledCount];
        int[] chosen = new int[unfilledCount];
        FindFeasibleCombinations(cellCands, unfilledCount, feasibleMasks,
            usedDigitsMask, remainingSum, 0, chosen);

        // Apply the feasible masks
        bool changed = false;
        for (int i = 0; i < unfilledCount; i++)
        {
            int cell = unfilledCells[i];
            int newCand = candidates[cell] & feasibleMasks[i];
            if (newCand != candidates[cell])
            {
                candidates[cell] = newCand;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// Recursively enumerate all valid digit assignments for unfilled cage cells.
    /// For each valid assignment (digits are distinct, not in usedMask, and sum to
    /// remainingSum), OR the chosen digit into the corresponding feasibleMasks entry.
    /// </summary>
    private static void FindFeasibleCombinations(
        int[] cellCandidates, int count, int[] feasibleMasks,
        int usedMask, int remainingSum, int cellIndex, int[] chosen)
    {
        int remaining = count - cellIndex;

        // Last unfilled cell: digit must equal the remaining sum exactly
        if (remaining == 1)
        {
            if (remainingSum >= 1 && remainingSum <= SIZE)
            {
                int bit = 1 << (remainingSum - 1);
                int available = cellCandidates[cellIndex] & ~usedMask;
                if ((available & bit) != 0)
                {
                    chosen[cellIndex] = remainingSum;
                    for (int i = 0; i < count; i++)
                        feasibleMasks[i] |= 1 << (chosen[i] - 1);
                }
            }
            return;
        }

        // Bounds for the sum of (remaining-1) more distinct digits after this one
        int afterThis = remaining - 1;
        int minAfter = afterThis * (afterThis + 1) / 2;
        int maxAfter = afterThis * (19 - afterThis) / 2;

        int available2 = cellCandidates[cellIndex] & ~usedMask;
        for (int bit = 0; bit < SIZE; bit++)
        {
            if ((available2 & (1 << bit)) == 0) continue;
            int digit = bit + 1;
            int newRemaining = remainingSum - digit;

            // Prune: remaining sum after placing this digit must be achievable
            if (newRemaining < minAfter || newRemaining > maxAfter) continue;

            chosen[cellIndex] = digit;
            FindFeasibleCombinations(cellCandidates, count, feasibleMasks,
                usedMask | (1 << bit), newRemaining, cellIndex + 1, chosen);
        }
    }

    #endregion

    #region Backtracking Search

    private int CountSolutionsRecursive(int[] solution, int[] candidates, int limit)
    {
        if (IsSolved(solution))
            return 1;

        int bestCell = FindBestCell(solution, candidates);
        if (bestCell == -1)
            return 0;

        int total = 0;
        int cands = candidates[bestCell];
        for (int bit = 0; bit < SIZE; bit++)
        {
            if ((cands & (1 << bit)) == 0) continue;

            int[] solCopy = new int[BOARD_SIZE];
            int[] candCopy = new int[BOARD_SIZE];
            Array.Copy(solution, solCopy, BOARD_SIZE);
            Array.Copy(candidates, candCopy, BOARD_SIZE);

            if (PlaceValue(solCopy, candCopy, bestCell, bit + 1)
                && Propagate(solCopy, candCopy))
            {
                total += CountSolutionsRecursive(solCopy, candCopy, limit - total);
                if (total >= limit) return total;
            }
        }

        return total;
    }

    private int[] SolveRecursive(int[] solution, int[] candidates)
    {
        if (IsSolved(solution))
        {
            int[] result = new int[BOARD_SIZE];
            Array.Copy(solution, result, BOARD_SIZE);
            return result;
        }

        int bestCell = FindBestCell(solution, candidates);
        if (bestCell == -1)
            return null;

        int cands = candidates[bestCell];
        for (int bit = 0; bit < SIZE; bit++)
        {
            if ((cands & (1 << bit)) == 0) continue;

            int[] solCopy = new int[BOARD_SIZE];
            int[] candCopy = new int[BOARD_SIZE];
            Array.Copy(solution, solCopy, BOARD_SIZE);
            Array.Copy(candidates, candCopy, BOARD_SIZE);

            if (PlaceValue(solCopy, candCopy, bestCell, bit + 1)
                && Propagate(solCopy, candCopy))
            {
                int[] result = SolveRecursive(solCopy, candCopy);
                if (result != null) return result;
            }
        }

        return null;
    }

    #endregion

    #region Helpers

    private static bool IsSolved(int[] solution)
    {
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            if (solution[i] == 0) return false;
        }
        return true;
    }

    /// <summary>
    /// Find the unsolved cell with the fewest candidates (MRV heuristic).
    /// Returns -1 if a contradiction is found (unsolved cell with no candidates)
    /// or if all cells are solved.
    /// </summary>
    private static int FindBestCell(int[] solution, int[] candidates)
    {
        int best = -1;
        int minCount = SIZE + 1;
        for (int cell = 0; cell < BOARD_SIZE; cell++)
        {
            if (solution[cell] != 0) continue;
            int count = PopCount(candidates[cell]);
            if (count == 0) return -1;
            if (count < minCount)
            {
                minCount = count;
                best = cell;
            }
        }
        return best;
    }

    private static bool IsSingleBit(int mask)
    {
        return mask != 0 && (mask & (mask - 1)) == 0;
    }

    private static int BitToValue(int singleBitMask)
    {
        int bit = 0;
        while ((singleBitMask >> bit) != 1) bit++;
        return bit + 1;
    }

    private static int PopCount(int value)
    {
        int count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }

    #endregion
}
