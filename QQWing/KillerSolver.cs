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
    private readonly List<string> _strategiesUsed = [];
    private readonly HashSet<string> _strategiesSeen = [];

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
        _strategiesUsed.Clear();
        _strategiesSeen.Clear();

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
        {
            RecordStrategy("Guess");
            return Difficulty.EXPERT;
        }

        return Difficulty.UNKNOWN;
    }

    /// <summary>
    /// Get a distinct list of strategy names used in the most recent GetDifficulty call,
    /// in the order they were first used.
    /// </summary>
    public List<string> GetStrategiesUsed() => new(_strategiesUsed);

    private void RecordStrategy(string name)
    {
        if (_strategiesSeen.Add(name))
            _strategiesUsed.Add(name);
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
            if (progress) { RecordStrategy("Naked Single"); continue; }

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
            if (progress) { RecordStrategy("Cage Sum Pruning"); continue; }

            // --- EASY: Hidden singles ---
            bool strategyProgress = false;

            if (!HiddenSinglesInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Single"); if (maxDifficulty < Difficulty.EASY) maxDifficulty = Difficulty.EASY; progress = true; continue; }

            if (!HiddenSinglesInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Single"); if (maxDifficulty < Difficulty.EASY) maxDifficulty = Difficulty.EASY; progress = true; continue; }

            if (!HiddenSinglesInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Single"); if (maxDifficulty < Difficulty.EASY) maxDifficulty = Difficulty.EASY; progress = true; continue; }

            // --- INTERMEDIATE: Naked pairs, hidden pairs, pointing pairs, box-line reduction ---
            if (!NakedPairsInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Naked Pair"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!NakedPairsInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Naked Pair"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!NakedPairsInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Naked Pair"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!HiddenPairsInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Pair"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!HiddenPairsInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Pair"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!HiddenPairsInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Pair"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!PointingPairs(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Pointing Pair/Triple"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!BoxLineReduction(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Box/Line Reduction"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!NakedTriplesInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Naked Triple"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!NakedTriplesInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Naked Triple"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!NakedTriplesInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Naked Triple"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!HiddenTriplesInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Triple"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!HiddenTriplesInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Triple"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            if (!HiddenTriplesInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Triple"); if (maxDifficulty < Difficulty.INTERMEDIATE) maxDifficulty = Difficulty.INTERMEDIATE; progress = true; continue; }

            // --- TOUGH: Naked quads, hidden quads, X-Wing, Y-Wing, XYZ-Wing, Swordfish, Jellyfish, Simple Coloring ---
            if (!NakedQuadsInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Naked Quad"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!NakedQuadsInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Naked Quad"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!NakedQuadsInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Naked Quad"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!HiddenQuadsInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Quad"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!HiddenQuadsInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Quad"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!HiddenQuadsInSections(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Hidden Quad"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!XWingInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("X-Wing"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!XWingInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("X-Wing"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!YWing(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Y-Wing"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!XyzWing(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("XYZ-Wing"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!SwordfishInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Swordfish"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!SwordfishInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Swordfish"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!JellyfishInRows(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Jellyfish"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!JellyfishInColumns(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Jellyfish"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }

            if (!SimpleColoring(solution, candidates, ref strategyProgress)) return false;
            if (strategyProgress) { RecordStrategy("Simple Coloring"); if (maxDifficulty < Difficulty.TOUGH) maxDifficulty = Difficulty.TOUGH; progress = true; continue; }
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

    #region Additional Intermediate Strategies

    private static bool NakedTriplesInRows(int[] solution, int[] candidates, ref bool progress)
    {
        for (int row = 0; row < SIZE; row++)
        {
            if (!NakedSubsetInUnit(solution, candidates, GetRowCells(row), 3, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool NakedTriplesInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        for (int col = 0; col < SIZE; col++)
        {
            if (!NakedSubsetInUnit(solution, candidates, GetColumnCells(col), 3, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool NakedTriplesInSections(int[] solution, int[] candidates, ref bool progress)
    {
        for (int sec = 0; sec < SIZE; sec++)
        {
            if (!NakedSubsetInUnit(solution, candidates, GetSectionCells(sec), 3, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool HiddenTriplesInRows(int[] solution, int[] candidates, ref bool progress)
    {
        for (int row = 0; row < SIZE; row++)
        {
            if (!HiddenSubsetInUnit(solution, candidates, GetRowCells(row), 3, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool HiddenTriplesInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        for (int col = 0; col < SIZE; col++)
        {
            if (!HiddenSubsetInUnit(solution, candidates, GetColumnCells(col), 3, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool HiddenTriplesInSections(int[] solution, int[] candidates, ref bool progress)
    {
        for (int sec = 0; sec < SIZE; sec++)
        {
            if (!HiddenSubsetInUnit(solution, candidates, GetSectionCells(sec), 3, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    #endregion

    #region Tough Strategies

    private static bool NakedQuadsInRows(int[] solution, int[] candidates, ref bool progress)
    {
        for (int row = 0; row < SIZE; row++)
        {
            if (!NakedSubsetInUnit(solution, candidates, GetRowCells(row), 4, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool NakedQuadsInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        for (int col = 0; col < SIZE; col++)
        {
            if (!NakedSubsetInUnit(solution, candidates, GetColumnCells(col), 4, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool NakedQuadsInSections(int[] solution, int[] candidates, ref bool progress)
    {
        for (int sec = 0; sec < SIZE; sec++)
        {
            if (!NakedSubsetInUnit(solution, candidates, GetSectionCells(sec), 4, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool HiddenQuadsInRows(int[] solution, int[] candidates, ref bool progress)
    {
        for (int row = 0; row < SIZE; row++)
        {
            if (!HiddenSubsetInUnit(solution, candidates, GetRowCells(row), 4, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool HiddenQuadsInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        for (int col = 0; col < SIZE; col++)
        {
            if (!HiddenSubsetInUnit(solution, candidates, GetColumnCells(col), 4, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool HiddenQuadsInSections(int[] solution, int[] candidates, ref bool progress)
    {
        for (int sec = 0; sec < SIZE; sec++)
        {
            if (!HiddenSubsetInUnit(solution, candidates, GetSectionCells(sec), 4, ref progress)) return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool XWingInRows(int[] solution, int[] candidates, ref bool progress)
    {
        for (int val = 1; val <= SIZE; val++)
        {
            int bit = 1 << (val - 1);
            for (int r1 = 0; r1 < SIZE - 1; r1++)
            {
                int colMask1 = 0;
                int count1 = 0;
                bool placed1 = false;
                for (int col = 0; col < SIZE; col++)
                {
                    int cell = r1 * SIZE + col;
                    if (solution[cell] == val) { placed1 = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        colMask1 |= 1 << col;
                        count1++;
                    }
                }
                if (placed1 || count1 != 2) continue;

                for (int r2 = r1 + 1; r2 < SIZE; r2++)
                {
                    int colMask2 = 0;
                    int count2 = 0;
                    bool placed2 = false;
                    for (int col = 0; col < SIZE; col++)
                    {
                        int cell = r2 * SIZE + col;
                        if (solution[cell] == val) { placed2 = true; break; }
                        if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                        {
                            colMask2 |= 1 << col;
                            count2++;
                        }
                    }
                    if (placed2 || count2 != 2 || colMask2 != colMask1) continue;

                    for (int row = 0; row < SIZE; row++)
                    {
                        if (row == r1 || row == r2) continue;
                        for (int col = 0; col < SIZE; col++)
                        {
                            if ((colMask1 & (1 << col)) == 0) continue;
                            int cell = row * SIZE + col;
                            if (solution[cell] != 0) continue;
                            int before = candidates[cell];
                            candidates[cell] &= ~bit;
                            if (candidates[cell] != before) progress = true;
                            if (candidates[cell] == 0) return false;
                        }
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool XWingInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        for (int val = 1; val <= SIZE; val++)
        {
            int bit = 1 << (val - 1);
            for (int c1 = 0; c1 < SIZE - 1; c1++)
            {
                int rowMask1 = 0;
                int count1 = 0;
                bool placed1 = false;
                for (int row = 0; row < SIZE; row++)
                {
                    int cell = row * SIZE + c1;
                    if (solution[cell] == val) { placed1 = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        rowMask1 |= 1 << row;
                        count1++;
                    }
                }
                if (placed1 || count1 != 2) continue;

                for (int c2 = c1 + 1; c2 < SIZE; c2++)
                {
                    int rowMask2 = 0;
                    int count2 = 0;
                    bool placed2 = false;
                    for (int row = 0; row < SIZE; row++)
                    {
                        int cell = row * SIZE + c2;
                        if (solution[cell] == val) { placed2 = true; break; }
                        if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                        {
                            rowMask2 |= 1 << row;
                            count2++;
                        }
                    }
                    if (placed2 || count2 != 2 || rowMask2 != rowMask1) continue;

                    for (int col = 0; col < SIZE; col++)
                    {
                        if (col == c1 || col == c2) continue;
                        for (int row = 0; row < SIZE; row++)
                        {
                            if ((rowMask1 & (1 << row)) == 0) continue;
                            int cell = row * SIZE + col;
                            if (solution[cell] != 0) continue;
                            int before = candidates[cell];
                            candidates[cell] &= ~bit;
                            if (candidates[cell] != before) progress = true;
                            if (candidates[cell] == 0) return false;
                        }
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool YWing(int[] solution, int[] candidates, ref bool progress)
    {
        for (int pivot = 0; pivot < BOARD_SIZE; pivot++)
        {
            if (solution[pivot] != 0 || PopCount(candidates[pivot]) != 2) continue;

            int pivotCands = candidates[pivot];
            int valA = -1, valB = -1;
            for (int v = 0; v < SIZE; v++)
            {
                if ((pivotCands & (1 << v)) != 0)
                {
                    if (valA == -1) valA = v;
                    else valB = v;
                }
            }

            List<int> peers = GetPeers(pivot);

            for (int wi = 0; wi < peers.Count; wi++)
            {
                int wing1 = peers[wi];
                if (solution[wing1] != 0 || PopCount(candidates[wing1]) != 2) continue;

                int w1Cands = candidates[wing1];
                bool w1HasA = (w1Cands & (1 << valA)) != 0;
                bool w1HasB = (w1Cands & (1 << valB)) != 0;
                if (w1HasA == w1HasB) continue;

                int sharedVal = w1HasA ? valA : valB;
                int otherPivotVal = w1HasA ? valB : valA;
                int valC = -1;
                for (int v = 0; v < SIZE; v++)
                {
                    if ((w1Cands & (1 << v)) != 0 && v != sharedVal)
                    {
                        valC = v;
                        break;
                    }
                }

                for (int wj = wi + 1; wj < peers.Count; wj++)
                {
                    int wing2 = peers[wj];
                    if (solution[wing2] != 0 || PopCount(candidates[wing2]) != 2) continue;

                    int w2Cands = candidates[wing2];
                    if ((w2Cands & (1 << otherPivotVal)) == 0 || (w2Cands & (1 << valC)) == 0) continue;
                    if (PopCount(w2Cands) != 2) continue;

                    int bitC = 1 << valC;
                    for (int target = 0; target < BOARD_SIZE; target++)
                    {
                        if (target == pivot || target == wing1 || target == wing2) continue;
                        if (solution[target] != 0) continue;
                        if ((candidates[target] & bitC) == 0) continue;

                        if (SharesUnit(target, wing1) && SharesUnit(target, wing2))
                        {
                            int before = candidates[target];
                            candidates[target] &= ~bitC;
                            if (candidates[target] != before) progress = true;
                            if (candidates[target] == 0) return false;
                        }
                    }
                    if (progress) return true;
                }
            }
        }
        return true;
    }

    private static bool XyzWing(int[] solution, int[] candidates, ref bool progress)
    {
        for (int pivot = 0; pivot < BOARD_SIZE; pivot++)
        {
            if (solution[pivot] != 0 || PopCount(candidates[pivot]) != 3) continue;

            int pivotCands = candidates[pivot];
            int[] pivotVals = new int[3];
            int pIdx = 0;
            for (int v = 0; v < SIZE; v++)
            {
                if ((pivotCands & (1 << v)) != 0)
                    pivotVals[pIdx++] = v;
            }

            List<int> peers = GetPeers(pivot);

            for (int ci = 0; ci < 3; ci++)
            {
                int valC = pivotVals[ci];
                int valA = pivotVals[(ci + 1) % 3];
                int valB = pivotVals[(ci + 2) % 3];

                for (int wi = 0; wi < peers.Count; wi++)
                {
                    int wing1 = peers[wi];
                    if (solution[wing1] != 0 || PopCount(candidates[wing1]) != 2) continue;
                    if ((candidates[wing1] & (1 << valA)) == 0 || (candidates[wing1] & (1 << valC)) == 0) continue;

                    for (int wj = wi + 1; wj < peers.Count; wj++)
                    {
                        int wing2 = peers[wj];
                        if (solution[wing2] != 0 || PopCount(candidates[wing2]) != 2) continue;
                        if ((candidates[wing2] & (1 << valB)) == 0 || (candidates[wing2] & (1 << valC)) == 0) continue;

                        int bitC = 1 << valC;
                        for (int target = 0; target < BOARD_SIZE; target++)
                        {
                            if (target == pivot || target == wing1 || target == wing2) continue;
                            if (solution[target] != 0) continue;
                            if ((candidates[target] & bitC) == 0) continue;

                            if (SharesUnit(target, pivot) && SharesUnit(target, wing1) && SharesUnit(target, wing2))
                            {
                                int before = candidates[target];
                                candidates[target] &= ~bitC;
                                if (candidates[target] != before) progress = true;
                                if (candidates[target] == 0) return false;
                            }
                        }
                        if (progress) return true;
                    }
                }
            }
        }
        return true;
    }

    private static bool SwordfishInRows(int[] solution, int[] candidates, ref bool progress)
    {
        return FishInRows(solution, candidates, 3, ref progress);
    }

    private static bool SwordfishInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        return FishInColumns(solution, candidates, 3, ref progress);
    }

    private static bool JellyfishInRows(int[] solution, int[] candidates, ref bool progress)
    {
        return FishInRows(solution, candidates, 4, ref progress);
    }

    private static bool JellyfishInColumns(int[] solution, int[] candidates, ref bool progress)
    {
        return FishInColumns(solution, candidates, 4, ref progress);
    }

    private static bool FishInRows(int[] solution, int[] candidates, int fishSize, ref bool progress)
    {
        for (int val = 1; val <= SIZE; val++)
        {
            int bit = 1 << (val - 1);
            int[] rowColMask = new int[SIZE];
            int[] rowColCount = new int[SIZE];
            for (int row = 0; row < SIZE; row++)
            {
                bool placed = false;
                for (int col = 0; col < SIZE; col++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == val) { placed = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        rowColMask[row] |= 1 << col;
                        rowColCount[row]++;
                    }
                }
                if (placed) { rowColMask[row] = 0; rowColCount[row] = 0; }
            }

            int eligCount = 0;
            int[] eligible = new int[SIZE];
            for (int row = 0; row < SIZE; row++)
            {
                if (rowColCount[row] >= 2 && rowColCount[row] <= fishSize)
                    eligible[eligCount++] = row;
            }
            if (eligCount < fishSize) continue;

            int[] combo = new int[fishSize];
            if (!FishRowRecurse(solution, candidates, eligible, eligCount, rowColMask, fishSize, 0, 0, combo, bit, ref progress))
                return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool FishRowRecurse(int[] solution, int[] candidates, int[] eligible, int eligCount,
        int[] rowColMask, int fishSize, int start, int depth, int[] combo, int bit, ref bool progress)
    {
        if (depth == fishSize)
        {
            int unionMask = 0;
            for (int d = 0; d < fishSize; d++)
                unionMask |= rowColMask[combo[d]];
            if (PopCount(unionMask) != fishSize) return true;

            for (int row = 0; row < SIZE; row++)
            {
                bool inCombo = false;
                for (int d = 0; d < fishSize; d++)
                    if (combo[d] == row) { inCombo = true; break; }
                if (inCombo) continue;

                for (int col = 0; col < SIZE; col++)
                {
                    if ((unionMask & (1 << col)) == 0) continue;
                    int cell = row * SIZE + col;
                    if (solution[cell] != 0) continue;
                    int before = candidates[cell];
                    candidates[cell] &= ~bit;
                    if (candidates[cell] != before) progress = true;
                    if (candidates[cell] == 0) return false;
                }
            }
            return true;
        }

        for (int i = start; i <= eligCount - (fishSize - depth); i++)
        {
            combo[depth] = eligible[i];
            if (depth > 0)
            {
                int partialUnion = 0;
                for (int d = 0; d <= depth; d++)
                    partialUnion |= rowColMask[combo[d]];
                if (PopCount(partialUnion) > fishSize) continue;
            }
            if (!FishRowRecurse(solution, candidates, eligible, eligCount, rowColMask, fishSize, i + 1, depth + 1, combo, bit, ref progress))
                return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool FishInColumns(int[] solution, int[] candidates, int fishSize, ref bool progress)
    {
        for (int val = 1; val <= SIZE; val++)
        {
            int bit = 1 << (val - 1);
            int[] colRowMask = new int[SIZE];
            int[] colRowCount = new int[SIZE];
            for (int col = 0; col < SIZE; col++)
            {
                bool placed = false;
                for (int row = 0; row < SIZE; row++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == val) { placed = true; break; }
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        colRowMask[col] |= 1 << row;
                        colRowCount[col]++;
                    }
                }
                if (placed) { colRowMask[col] = 0; colRowCount[col] = 0; }
            }

            int eligCount = 0;
            int[] eligible = new int[SIZE];
            for (int col = 0; col < SIZE; col++)
            {
                if (colRowCount[col] >= 2 && colRowCount[col] <= fishSize)
                    eligible[eligCount++] = col;
            }
            if (eligCount < fishSize) continue;

            int[] combo = new int[fishSize];
            if (!FishColRecurse(solution, candidates, eligible, eligCount, colRowMask, fishSize, 0, 0, combo, bit, ref progress))
                return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool FishColRecurse(int[] solution, int[] candidates, int[] eligible, int eligCount,
        int[] colRowMask, int fishSize, int start, int depth, int[] combo, int bit, ref bool progress)
    {
        if (depth == fishSize)
        {
            int unionMask = 0;
            for (int d = 0; d < fishSize; d++)
                unionMask |= colRowMask[combo[d]];
            if (PopCount(unionMask) != fishSize) return true;

            for (int col = 0; col < SIZE; col++)
            {
                bool inCombo = false;
                for (int d = 0; d < fishSize; d++)
                    if (combo[d] == col) { inCombo = true; break; }
                if (inCombo) continue;

                for (int row = 0; row < SIZE; row++)
                {
                    if ((unionMask & (1 << row)) == 0) continue;
                    int cell = row * SIZE + col;
                    if (solution[cell] != 0) continue;
                    int before = candidates[cell];
                    candidates[cell] &= ~bit;
                    if (candidates[cell] != before) progress = true;
                    if (candidates[cell] == 0) return false;
                }
            }
            return true;
        }

        for (int i = start; i <= eligCount - (fishSize - depth); i++)
        {
            combo[depth] = eligible[i];
            if (depth > 0)
            {
                int partialUnion = 0;
                for (int d = 0; d <= depth; d++)
                    partialUnion |= colRowMask[combo[d]];
                if (PopCount(partialUnion) > fishSize) continue;
            }
            if (!FishColRecurse(solution, candidates, eligible, eligCount, colRowMask, fishSize, i + 1, depth + 1, combo, bit, ref progress))
                return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool SimpleColoring(int[] solution, int[] candidates, ref bool progress)
    {
        for (int val = 1; val <= SIZE; val++)
        {
            int bit = 1 << (val - 1);

            int candCount = 0;
            int[] candCells = new int[BOARD_SIZE];
            for (int cell = 0; cell < BOARD_SIZE; cell++)
            {
                if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    candCells[candCount++] = cell;
            }
            if (candCount < 3) continue;

            // Build conjugate pair links
            Dictionary<int, List<int>> links = [];
            for (int i = 0; i < candCount; i++)
                links[candCells[i]] = [];

            for (int row = 0; row < SIZE; row++)
            {
                int c1 = -1, c2 = -1, count = 0;
                for (int col = 0; col < SIZE; col++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        if (c1 == -1) c1 = cell;
                        else if (c2 == -1) c2 = cell;
                        count++;
                    }
                }
                if (count == 2) { links[c1].Add(c2); links[c2].Add(c1); }
            }

            for (int col = 0; col < SIZE; col++)
            {
                int c1 = -1, c2 = -1, count = 0;
                for (int row = 0; row < SIZE; row++)
                {
                    int cell = row * SIZE + col;
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        if (c1 == -1) c1 = cell;
                        else if (c2 == -1) c2 = cell;
                        count++;
                    }
                }
                if (count == 2)
                {
                    if (!links[c1].Contains(c2)) links[c1].Add(c2);
                    if (!links[c2].Contains(c1)) links[c2].Add(c1);
                }
            }

            for (int sec = 0; sec < SIZE; sec++)
            {
                int c1 = -1, c2 = -1, count = 0;
                foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
                {
                    if (solution[cell] == 0 && (candidates[cell] & bit) != 0)
                    {
                        if (c1 == -1) c1 = cell;
                        else if (c2 == -1) c2 = cell;
                        count++;
                    }
                }
                if (count == 2)
                {
                    if (!links[c1].Contains(c2)) links[c1].Add(c2);
                    if (!links[c2].Contains(c1)) links[c2].Add(c1);
                }
            }

            // BFS coloring for each connected component
            HashSet<int> visited = [];
            for (int ci = 0; ci < candCount; ci++)
            {
                int startCell = candCells[ci];
                if (visited.Contains(startCell) || links[startCell].Count == 0) continue;

                Dictionary<int, int> color = [];
                Queue<int> queue = new();
                color[startCell] = 0;
                queue.Enqueue(startCell);
                visited.Add(startCell);

                while (queue.Count > 0)
                {
                    int cell = queue.Dequeue();
                    int nextColor = 1 - color[cell];
                    foreach (int linked in links[cell])
                    {
                        if (!color.ContainsKey(linked))
                        {
                            color[linked] = nextColor;
                            visited.Add(linked);
                            queue.Enqueue(linked);
                        }
                    }
                }

                if (color.Count < 2) continue;

                List<int> color0 = [];
                List<int> color1 = [];
                foreach (var kvp in color)
                {
                    if (kvp.Value == 0) color0.Add(kvp.Key);
                    else color1.Add(kvp.Key);
                }

                // Rule 2: Color contradiction
                bool color0Invalid = HasSameColorConflict(color0);
                bool color1Invalid = HasSameColorConflict(color1);
                if (color0Invalid && color1Invalid) continue;

                if (color0Invalid || color1Invalid)
                {
                    List<int> invalidCells = color0Invalid ? color0 : color1;
                    foreach (int cell in invalidCells)
                    {
                        int before = candidates[cell];
                        candidates[cell] &= ~bit;
                        if (candidates[cell] != before) progress = true;
                        if (candidates[cell] == 0) return false;
                    }
                    if (progress) return true;
                }

                // Rule 4: Color elimination
                for (int ci2 = 0; ci2 < candCount; ci2++)
                {
                    int cell = candCells[ci2];
                    if (color.ContainsKey(cell)) continue;

                    bool seesColor0 = false, seesColor1 = false;
                    foreach (int c0 in color0)
                        if (SharesUnit(cell, c0)) { seesColor0 = true; break; }
                    if (!seesColor0) continue;
                    foreach (int c1 in color1)
                        if (SharesUnit(cell, c1)) { seesColor1 = true; break; }

                    if (seesColor0 && seesColor1)
                    {
                        int before = candidates[cell];
                        candidates[cell] &= ~bit;
                        if (candidates[cell] != before) progress = true;
                        if (candidates[cell] == 0) return false;
                    }
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

    private static int[] GetRowCells(int row)
    {
        int[] cells = new int[SIZE];
        for (int col = 0; col < SIZE; col++)
            cells[col] = row * SIZE + col;
        return cells;
    }

    private static int[] GetColumnCells(int col)
    {
        int[] cells = new int[SIZE];
        for (int row = 0; row < SIZE; row++)
            cells[row] = row * SIZE + col;
        return cells;
    }

    private static int[] GetSectionCells(int sec)
    {
        int[] cells = new int[SIZE];
        int i = 0;
        foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
            cells[i++] = cell;
        return cells;
    }

    private static bool NakedSubsetInUnit(int[] solution, int[] candidates, int[] unitCells, int subsetSize, ref bool progress)
    {
        int eligibleCount = 0;
        int[] eligible = new int[SIZE];
        for (int i = 0; i < unitCells.Length; i++)
        {
            int cell = unitCells[i];
            if (solution[cell] != 0) continue;
            int pc = PopCount(candidates[cell]);
            if (pc >= 2 && pc <= subsetSize)
                eligible[eligibleCount++] = cell;
        }
        if (eligibleCount < subsetSize) return true;

        int[] combo = new int[subsetSize];
        return NakedSubsetRecurse(solution, candidates, unitCells, eligible, eligibleCount, subsetSize, 0, 0, combo, ref progress);
    }

    private static bool NakedSubsetRecurse(int[] solution, int[] candidates, int[] unitCells,
        int[] eligible, int eligibleCount, int subsetSize, int start, int depth, int[] combo, ref bool progress)
    {
        if (depth == subsetSize)
        {
            int unionMask = 0;
            for (int d = 0; d < subsetSize; d++)
                unionMask |= candidates[combo[d]];
            if (PopCount(unionMask) != subsetSize) return true;

            for (int i = 0; i < unitCells.Length; i++)
            {
                int cell = unitCells[i];
                if (solution[cell] != 0) continue;
                bool inCombo = false;
                for (int d = 0; d < subsetSize; d++)
                    if (combo[d] == cell) { inCombo = true; break; }
                if (inCombo) continue;

                int before = candidates[cell];
                candidates[cell] &= ~unionMask;
                if (candidates[cell] != before) progress = true;
                if (candidates[cell] == 0) return false;
            }
            return true;
        }

        for (int i = start; i <= eligibleCount - (subsetSize - depth); i++)
        {
            combo[depth] = eligible[i];
            if (depth > 0)
            {
                int partialUnion = 0;
                for (int d = 0; d <= depth; d++)
                    partialUnion |= candidates[combo[d]];
                if (PopCount(partialUnion) > subsetSize) continue;
            }
            if (!NakedSubsetRecurse(solution, candidates, unitCells, eligible, eligibleCount, subsetSize, i + 1, depth + 1, combo, ref progress))
                return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool HiddenSubsetInUnit(int[] solution, int[] candidates, int[] unitCells, int subsetSize, ref bool progress)
    {
        int[] valPosMask = new int[SIZE];
        int[] valPosCount = new int[SIZE];
        for (int val = 1; val <= SIZE; val++)
        {
            bool placed = false;
            for (int i = 0; i < unitCells.Length; i++)
            {
                int cell = unitCells[i];
                if (solution[cell] == val) { placed = true; break; }
                if (solution[cell] == 0 && (candidates[cell] & (1 << (val - 1))) != 0)
                {
                    valPosMask[val - 1] |= 1 << i;
                    valPosCount[val - 1]++;
                }
            }
            if (placed) { valPosMask[val - 1] = 0; valPosCount[val - 1] = 0; }
        }

        int eligCount = 0;
        int[] eligible = new int[SIZE];
        for (int v = 0; v < SIZE; v++)
        {
            if (valPosCount[v] >= 2 && valPosCount[v] <= subsetSize)
                eligible[eligCount++] = v;
        }
        if (eligCount < subsetSize) return true;

        int[] combo = new int[subsetSize];
        return HiddenSubsetRecurse(candidates, unitCells, valPosMask, eligible, eligCount, subsetSize, 0, 0, combo, ref progress);
    }

    private static bool HiddenSubsetRecurse(int[] candidates, int[] unitCells,
        int[] valPosMask, int[] eligible, int eligCount, int subsetSize, int start, int depth, int[] combo, ref bool progress)
    {
        if (depth == subsetSize)
        {
            int unionMask = 0;
            for (int d = 0; d < subsetSize; d++)
                unionMask |= valPosMask[combo[d]];
            if (PopCount(unionMask) != subsetSize) return true;

            int keepMask = 0;
            for (int d = 0; d < subsetSize; d++)
                keepMask |= 1 << combo[d];

            for (int bit = 0; bit < SIZE; bit++)
            {
                if ((unionMask & (1 << bit)) == 0) continue;
                int cell = unitCells[bit];
                int before = candidates[cell];
                candidates[cell] &= keepMask;
                if (candidates[cell] != before) progress = true;
                if (candidates[cell] == 0) return false;
            }
            return true;
        }

        for (int i = start; i <= eligCount - (subsetSize - depth); i++)
        {
            combo[depth] = eligible[i];
            if (depth > 0)
            {
                int partialUnion = 0;
                for (int d = 0; d <= depth; d++)
                    partialUnion |= valPosMask[combo[d]];
                if (PopCount(partialUnion) > subsetSize) continue;
            }
            if (!HiddenSubsetRecurse(candidates, unitCells, valPosMask, eligible, eligCount, subsetSize, i + 1, depth + 1, combo, ref progress))
                return false;
            if (progress) return true;
        }
        return true;
    }

    private static bool SharesUnit(int cell1, int cell2)
    {
        if (cell1 / SIZE == cell2 / SIZE) return true;
        if (cell1 % SIZE == cell2 % SIZE) return true;
        if (QQWing.SectionLayout.CellToSection(cell1) == QQWing.SectionLayout.CellToSection(cell2)) return true;
        return false;
    }

    private static List<int> GetPeers(int cell)
    {
        HashSet<int> peers = [];
        int row = cell / SIZE;
        int col = cell % SIZE;
        int section = QQWing.SectionLayout.CellToSection(cell);

        for (int c = 0; c < SIZE; c++)
        {
            int pos = row * SIZE + c;
            if (pos != cell) peers.Add(pos);
        }
        for (int r = 0; r < SIZE; r++)
        {
            int pos = r * SIZE + col;
            if (pos != cell) peers.Add(pos);
        }
        foreach (int pos in QQWing.SectionLayout.SectionToSectionCells(section))
        {
            if (pos != cell) peers.Add(pos);
        }
        return [.. peers];
    }

    private static bool HasSameColorConflict(List<int> cells)
    {
        for (int i = 0; i < cells.Count - 1; i++)
            for (int j = i + 1; j < cells.Count; j++)
                if (SharesUnit(cells[i], cells[j]))
                    return true;
        return false;
    }

    #endregion
}
