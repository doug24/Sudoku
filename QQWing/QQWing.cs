/*
 * qqwing - Sudoku solver and generator
 * Copyright (C) 2006-2014 Stephen Ostermiller http://ostermiller.org/
 * Copyright (C) 2007 Jacques Bensimon (jacques@ipm.com)
 * Copyright (C) 2007 Joel Yarde (joel.yarde - gmail.com)
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

[assembly: InternalsVisibleTo("QQWing.Tests")]

namespace QQWingLib;

/// <summary>
/// The board containing all the memory structures and methods for solving or generating sudoku puzzles.
/// </summary>
public class QQWing
{
    public readonly static string QQWING_VERSION = "n1.3.4";

    private readonly static string NL = Environment.NewLine;

    /// <summary>nominal value = 3</summary>
    internal readonly static int GRID_SIZE = 3;

    /// <summary>nominal value = 9</summary>
    internal readonly static int ROW_COL_SEC_SIZE = GRID_SIZE * GRID_SIZE;

    /// <summary>nominal value = 27</summary>
    internal readonly static int SEC_GROUP_SIZE = ROW_COL_SEC_SIZE * GRID_SIZE;

    /// <summary>nominal value = 81</summary>
    internal readonly static int BOARD_SIZE = ROW_COL_SEC_SIZE * ROW_COL_SEC_SIZE;

    /// <summary>nominal value = 729</summary>
    internal readonly static int POSSIBILITY_SIZE = BOARD_SIZE * ROW_COL_SEC_SIZE;

    private readonly static Random random = new();

    /// <summary>
    /// The last round of solving
    /// </summary>
    private int lastSolveRound;

    /// <summary>
    /// The section layout for this sudoku: classic 3x3 or an irregular pattern. Made static to avoid changing the class
    /// interface.
    /// </summary>
    public static ISectionLayout SectionLayout { get; set; } = new RegularLayout();

    /// <summary>value = -1</summary>
    public readonly static int ClassicLayout = -1;

    public static bool IsClassicLayout => SectionLayout.Layout == ClassicLayout;

    /// <summary>value = 999</summary>
    public readonly static int RandomLayout = 999;

    /// <summary>
    /// The 81 integers that make up a sudoku puzzle. Givens are 1-9, unknowns are 0. Once initialized, this puzzle
    /// remains as is. The answer is worked out in "solution".
    /// </summary>
    private readonly int[] puzzle = new int[BOARD_SIZE];

    /// <summary>
    /// The 81 integers that make up a sudoku puzzle. The solution is built here, after completion all will be 1-9.
    /// </summary>
    private readonly int[] solution = new int[BOARD_SIZE];

    /// <summary>
    /// Recursion depth at which each of the numbers in the solution were placed. Useful for backing out solve branches
    /// that don't lead to a solution.
    /// </summary>
    private readonly int[] solutionRound = new int[BOARD_SIZE];

    /// <summary>
    /// The 729 integers that make up a the possible values for a Sudoku puzzle. (9 possibilities for each of 81
    /// squares). If possibilities[i] is zero, then the possibility could still be filled in according to the Sudoku
    /// rules. When a possibility is eliminated, possibilities[i] is assigned the round (recursion level) at which it
    /// was determined that it could not be a possibility.
    /// </summary>
    private readonly int[] possibilities = new int[POSSIBILITY_SIZE];

    /// <summary>
    /// An array the size of the board (81) containing each of the numbers 0-n exactly once. This array may be shuffled
    /// so that operations that need to look at each cell can do so in a random order.
    /// </summary>
    private readonly int[] randomBoardArray = FillIncrementing(new int[BOARD_SIZE]);

    /// <summary>
    /// An array with one element for each position (9), in some random order to be used when trying each position in
    /// turn during guesses.
    /// </summary>
    private readonly int[] randomPossibilityArray = FillIncrementing(new int[ROW_COL_SEC_SIZE]);

    /// <summary>
    /// Whether or not to record history
    /// </summary>
    private bool recordHistory = false;

    /// <summary>
    /// Whether or not to print history as it happens
    /// </summary>
    private bool logHistory = false;

    /// <summary>
    /// A list of moves used to solve the puzzle. This list contains all moves, even on solve branches that did not lead
    /// to a solution.
    /// </summary>
    private readonly List<LogItem> solveHistory = [];

    /// <summary>
    /// A list of moves used to solve the puzzle. This list contains only the moves needed to solve the puzzle, but
    /// doesn't contain information about bad guesses.
    /// </summary>
    private readonly List<LogItem> solveInstructions = [];

    /// <summary>
    /// The style with which to print puzzles and solutions
    /// </summary>
    private PrintStyle printStyle = PrintStyle.READABLE;

    /// <summary>
    /// Create a new Sudoku board
    /// </summary>
    public QQWing()
    {
    }

    private static int[] FillIncrementing(int[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = i;
        }
        return arr;
    }

    /// <summary>
    /// Get the number of cells that are set in the puzzle (as opposed to figured out in the solution
    /// </summary>
    public int GetGivenCount()
    {
        int count = 0;
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            if (puzzle[i] != 0) count++;
        }
        return count;
    }

    /// <summary>
    /// Set the board to the given puzzle. The given puzzle must be an array of 81 integers.
    /// </summary>
    public bool SetPuzzle(int[] initPuzzle)
    {
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            puzzle[i] = (initPuzzle == null) ? 0 : initPuzzle[i];
        }
        return Reset();
    }

    /// <summary>
    /// Reset the board to its initial state with only the givens. This method clears any solution, resets statistics,
    /// and clears any history messages.
    /// </summary>
    private bool Reset()
    {
        Array.Clear(solution, 0, solution.Length);
        Array.Clear(solutionRound, 0, solutionRound.Length);
        Array.Clear(possibilities, 0, possibilities.Length);
        solveHistory.Clear();
        solveInstructions.Clear();

        int round = 1;
        for (int position = 0; position < BOARD_SIZE; position++)
        {
            if (puzzle[position] > 0)
            {
                int valIndex = puzzle[position] - 1;
                int valPos = GetPossibilityIndex(valIndex, position);
                int value = puzzle[position];
                if (possibilities[valPos] != 0) return false;
                Mark(position, round, value);
                if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.GIVEN, value, position));
            }
        }

        return true;
    }

    /// <summary>
    /// Get the difficulty rating.
    /// </summary>
    public Difficulty GetDifficulty()
    {
        if (GetGuessCount() > 0) return Difficulty.EXPERT;
        if (GetSimpleColoringCount() > 0) return Difficulty.TOUGH;
        if (GetJellyfishCount() > 0) return Difficulty.TOUGH;
        if (GetSwordfishCount() > 0) return Difficulty.TOUGH;
        if (GetXyzWingCount() > 0) return Difficulty.TOUGH;
        if (GetYWingCount() > 0) return Difficulty.TOUGH;
        if (GetXWingCount() > 0) return Difficulty.TOUGH;
        if (GetHiddenQuadCount() > 0) return Difficulty.TOUGH;
        if (GetNakedQuadCount() > 0) return Difficulty.TOUGH;
        if (GetBoxLineReductionCount() > 0) return Difficulty.INTERMEDIATE;
        if (GetPointingPairTripleCount() > 0) return Difficulty.INTERMEDIATE;
        if (GetHiddenTripleCount() > 0) return Difficulty.INTERMEDIATE;
        if (GetHiddenPairCount() > 0) return Difficulty.INTERMEDIATE;
        if (GetNakedTripleCount() > 0) return Difficulty.INTERMEDIATE;
        if (GetNakedPairCount() > 0) return Difficulty.INTERMEDIATE;
        if (GetHiddenSingleCount() > 0) return Difficulty.EASY;
        if (GetSingleCount() > 0) return Difficulty.SIMPLE;
        return Difficulty.UNKNOWN;
    }

    /// <summary>
    /// Get the difficulty rating.
    /// </summary>
    public string GetDifficultyAsString()
    {
        return GetDifficulty().ToString();
    }

    /// <summary>
    /// Get the number of cells for which the solution was determined because there was only one possible value for that
    /// cell.
    /// </summary>
    public int GetSingleCount()
    {
        return GetLogCount(solveInstructions, LogType.SINGLE);
    }

    /// <summary>
    /// Get the number of cells for which the solution was determined because that cell had the only possibility for
    /// some value in the row, column, or section.
    /// </summary>
    public int GetHiddenSingleCount()
    {
        return (GetLogCount(solveInstructions, LogType.HIDDEN_SINGLE_ROW) +
            GetLogCount(solveInstructions, LogType.HIDDEN_SINGLE_COLUMN) + GetLogCount(solveInstructions, LogType.HIDDEN_SINGLE_SECTION));
    }

    /// <summary>
    /// Get the number of naked pair reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetNakedPairCount()
    {
        return (GetLogCount(solveInstructions, LogType.NAKED_PAIR_ROW) +
            GetLogCount(solveInstructions, LogType.NAKED_PAIR_COLUMN) + GetLogCount(solveInstructions, LogType.NAKED_PAIR_SECTION));
    }

    /// <summary>
    /// Get the number of naked triple reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetNakedTripleCount()
    {
        return (GetLogCount(solveInstructions, LogType.NAKED_TRIPLE_ROW) +
            GetLogCount(solveInstructions, LogType.NAKED_TRIPLE_COLUMN) + GetLogCount(solveInstructions, LogType.NAKED_TRIPLE_SECTION));
    }

    /// <summary>
    /// Get the number of naked quad reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetNakedQuadCount()
    {
        return (GetLogCount(solveInstructions, LogType.NAKED_QUAD_ROW) +
            GetLogCount(solveInstructions, LogType.NAKED_QUAD_COLUMN) + GetLogCount(solveInstructions, LogType.NAKED_QUAD_SECTION));
    }

    /// <summary>
    /// Get the number of hidden pair reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetHiddenPairCount()
    {
        return (GetLogCount(solveInstructions, LogType.HIDDEN_PAIR_ROW) +
            GetLogCount(solveInstructions, LogType.HIDDEN_PAIR_COLUMN) + GetLogCount(solveInstructions, LogType.HIDDEN_PAIR_SECTION));
    }

    /// <summary>
    /// Get the number of pointing pair/triple reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetPointingPairTripleCount()
    {
        return (GetLogCount(solveInstructions, LogType.POINTING_PAIR_TRIPLE_ROW) + GetLogCount(solveInstructions, LogType.POINTING_PAIR_TRIPLE_COLUMN));
    }

    /// <summary>
    /// Get the number of box/line reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetBoxLineReductionCount()
    {
        return (GetLogCount(solveInstructions, LogType.ROW_BOX) + GetLogCount(solveInstructions, LogType.COLUMN_BOX));
    }

    /// <summary>
    /// Get the number of hidden triple reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetHiddenTripleCount()
    {
        return (GetLogCount(solveInstructions, LogType.HIDDEN_TRIPLE_ROW) +
            GetLogCount(solveInstructions, LogType.HIDDEN_TRIPLE_COLUMN) + GetLogCount(solveInstructions, LogType.HIDDEN_TRIPLE_SECTION));
    }

    /// <summary>
    /// Get the number of hidden quad reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetHiddenQuadCount()
    {
        return (GetLogCount(solveInstructions, LogType.HIDDEN_QUAD_ROW) +
            GetLogCount(solveInstructions, LogType.HIDDEN_QUAD_COLUMN) + GetLogCount(solveInstructions, LogType.HIDDEN_QUAD_SECTION));
    }

    /// <summary>
    /// Get the number of XYZ-Wing reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetXyzWingCount()
    {
        return GetLogCount(solveInstructions, LogType.XYZ_WING);
    }

    /// <summary>
    /// Get the number of Jellyfish reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetJellyfishCount()
    {
        return (GetLogCount(solveInstructions, LogType.JELLYFISH_ROW) +
            GetLogCount(solveInstructions, LogType.JELLYFISH_COLUMN));
    }

    /// <summary>
    /// Get the number of X-Wing reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetXWingCount()
    {
        return (GetLogCount(solveInstructions, LogType.X_WING_ROW) +
            GetLogCount(solveInstructions, LogType.X_WING_COLUMN));
    }

    /// <summary>
    /// Get the number of Swordfish reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetSwordfishCount()
    {
        return (GetLogCount(solveInstructions, LogType.SWORDFISH_ROW) +
            GetLogCount(solveInstructions, LogType.SWORDFISH_COLUMN));
    }

    /// <summary>
    /// Get the number of Y-Wing reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetYWingCount()
    {
        return GetLogCount(solveInstructions, LogType.Y_WING);
    }

    /// <summary>
    /// Get the number of simple coloring reductions that were performed in solving this puzzle.
    /// </summary>
    public int GetSimpleColoringCount()
    {
        return GetLogCount(solveInstructions, LogType.SIMPLE_COLORING);
    }

    /// <summary>
    /// Get the number of lucky guesses in solving this puzzle.
    /// </summary>
    public int GetGuessCount()
    {
        return GetLogCount(solveInstructions, LogType.GUESS);
    }

    /// <summary>
    /// Get the number of backtracks (unlucky guesses) required when solving this puzzle.
    /// </summary>
    public int GetBacktrackCount()
    {
        return GetLogCount(solveHistory, LogType.ROLLBACK);
    }

    private void ShuffleRandomArrays()
    {
        ShuffleArray(randomBoardArray, BOARD_SIZE);
        ShuffleArray(randomPossibilityArray, ROW_COL_SEC_SIZE);
    }

    private void ClearPuzzle()
    {
        // Clear any existing puzzle
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            puzzle[i] = 0;
        }
        Reset();
    }

    public bool GeneratePuzzle(CancellationToken token)
    {
        return GeneratePuzzleSymmetry(Symmetry.NONE, token);
    }

    public bool GeneratePuzzleSymmetry(Symmetry symmetry, CancellationToken token)
    {
        if (symmetry == Symmetry.RANDOM) symmetry = GetRandomSymmetry();

        // Don't record history while generating.
        bool recHistory = recordHistory;
        SetRecordHistory(false);
        bool lHistory = logHistory;
        SetLogHistory(false);

        ClearPuzzle();

        // Start by getting the randomness in order so that
        // each puzzle will be different from the last.
        ShuffleRandomArrays();

        // Now solve the puzzle the whole way. The solve
        // uses random algorithms, so we should have a
        // really randomly totally filled sudoku
        // Even when starting from an empty grid
        Solve(token);

        if (symmetry == Symmetry.NONE)
        {
            // Rollback any square for which it is obvious that
            // the square doesn't contribute to a unique solution
            // (ie, squares that were filled by logic rather
            // than by guess)
            RollbackNonGuesses();
        }

        // Record all marked squares as the puzzle so
        // that we can call countSolutions without losing it.
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            puzzle[i] = solution[i];
        }

        // Rerandomize everything so that we test squares
        // in a different order than they were added.
        ShuffleRandomArrays();

        // Remove one value at a time and see if
        // the puzzle still has only one solution.
        // If it does, leave it out the point because
        // it is not needed.
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            token.ThrowIfCancellationRequested();

            // check all the positions, but in shuffled order
            int position = randomBoardArray[i];
            if (puzzle[position] > 0)
            {
                int positionsym1 = -1;
                int positionsym2 = -1;
                int positionsym3 = -1;
                switch (symmetry)
                {
                    case Symmetry.ROTATE90:
                        positionsym2 = RowColumnToCell(ROW_COL_SEC_SIZE - 1 - CellToColumn(position), CellToRow(position));
                        positionsym3 = RowColumnToCell(CellToColumn(position), ROW_COL_SEC_SIZE - 1 - CellToRow(position));
                        positionsym1 = RowColumnToCell(ROW_COL_SEC_SIZE - 1 - CellToRow(position), ROW_COL_SEC_SIZE - 1 - CellToColumn(position));
                        break;
                    case Symmetry.ROTATE180:
                        positionsym1 = RowColumnToCell(ROW_COL_SEC_SIZE - 1 - CellToRow(position), ROW_COL_SEC_SIZE - 1 - CellToColumn(position));
                        break;
                    case Symmetry.MIRROR:
                        positionsym1 = RowColumnToCell(CellToRow(position), ROW_COL_SEC_SIZE - 1 - CellToColumn(position));
                        break;
                    case Symmetry.FLIP:
                        positionsym1 = RowColumnToCell(ROW_COL_SEC_SIZE - 1 - CellToRow(position), CellToColumn(position));
                        break;
                    default:
                        break;
                }
                // try backing out the value and
                // counting solutions to the puzzle
                int savedValue = puzzle[position];
                puzzle[position] = 0;
                int savedSym1 = 0;
                if (positionsym1 >= 0)
                {
                    savedSym1 = puzzle[positionsym1];
                    puzzle[positionsym1] = 0;
                }
                int savedSym2 = 0;
                if (positionsym2 >= 0)
                {
                    savedSym2 = puzzle[positionsym2];
                    puzzle[positionsym2] = 0;
                }
                int savedSym3 = 0;
                if (positionsym3 >= 0)
                {
                    savedSym3 = puzzle[positionsym3];
                    puzzle[positionsym3] = 0;
                }
                Reset();
                if (CountSolutions(2, true) > 1)
                {
                    // Put it back in, it is needed
                    puzzle[position] = savedValue;
                    if (positionsym1 >= 0 && savedSym1 != 0) puzzle[positionsym1] = savedSym1;
                    if (positionsym2 >= 0 && savedSym2 != 0) puzzle[positionsym2] = savedSym2;
                    if (positionsym3 >= 0 && savedSym3 != 0) puzzle[positionsym3] = savedSym3;
                }
            }
        }

        // Clear all solution info, leaving just the puzzle.
        Reset();

        // Restore recording history.
        SetRecordHistory(recHistory);
        SetLogHistory(lHistory);

        return true;
    }

    private void RollbackNonGuesses()
    {
        // Guesses are odd rounds
        // Non-guesses are even rounds
        for (int i = 2; i <= lastSolveRound; i += 2)
        {
            RollbackRound(i);
        }
    }

    public void SetPrintStyle(PrintStyle ps)
    {
        printStyle = ps;
    }

    public void SetRecordHistory(bool recHistory)
    {
        recordHistory = recHistory;
    }

    public void SetLogHistory(bool logHist)
    {
        logHistory = logHist;
    }

    private void AddHistoryItem(LogItem l)
    {
        if (logHistory)
        {
            l.Print();
        }
        if (recordHistory)
        {
            solveHistory.Add(l);
            solveInstructions.Add(l);
        }
    }

    private void PrintHistory(List<LogItem> v)
    {
        Debug.Write(HistoryToString(v));
    }

    private string HistoryToString(List<LogItem> v)
    {
        if (!recordHistory)
        {
            return "History was not recorded.";
        }

        StringBuilder sb = new();
        for (int i = 0; i < v.Count; i++)
        {
            sb.Append($"{i + 1}.".PadLeft(4)).Append(' ');
            sb.Append(v[i].GetDescription());
            sb.Append(NL);
        }
        return sb.ToString();
    }

    public void PrintSolveInstructions()
    {
        Debug.Write(GetSolveInstructionsString());
    }

    public string GetSolveInstructionsString()
    {
        if (IsSolved())
        {
            return HistoryToString(solveInstructions);
        }
        else
        {
            return "No solve instructions - Puzzle is not possible to solve.";
        }
    }

    public string[] GetCompactSolveInstructions()
    {
        if (IsSolved())
        {
            if (!recordHistory)
            {
                return ["History was not recorded."];
            }

            List<string> result = [];
            for (int i = 0; i < solveInstructions.Count; i++)
            {
                result.Add(solveInstructions[i].GetCompactString());
            }
            return [.. result];
        }
        else
        {
            return ["No solve instructions - Puzzle is not possible to solve."];
        }
    }

    public IEnumerable<LogItem> GetSolveInstructions()
    {
        if (IsSolved())
        {
            return new ReadOnlyCollection<LogItem>(solveInstructions);
        }
        else
        {
            return Enumerable.Empty<LogItem>();
        }
    }

    public void PrintSolveHistory()
    {
        PrintHistory(solveHistory);
    }

    public string GetSolveHistoryString()
    {
        return HistoryToString(solveHistory);
    }

    public IEnumerable<LogItem> GetSolveHistory()
    {
        return new ReadOnlyCollection<LogItem>(solveHistory);
    }

    /// <summary>
    /// Get a distinct list of strategy names used to solve the puzzle, in the order they were first used.
    /// </summary>
    public List<string> GetStrategiesUsed()
    {
        List<string> strategies = [];
        HashSet<string> seen = [];
        foreach (LogItem item in GetSolveInstructions())
        {
            string name = GetStrategyName(item.GetLogType());
            if (name != null && seen.Add(name))
            {
                strategies.Add(name);
            }
        }
        return strategies;
    }

    /// <summary>
    /// Maps a LogType to a human-readable strategy name, or null for non-strategy log types like GIVEN and ROLLBACK.
    /// </summary>
    public static string GetStrategyName(LogType type) => type switch
    {
        LogType.SINGLE => "Naked Single",
        LogType.HIDDEN_SINGLE_ROW or
        LogType.HIDDEN_SINGLE_COLUMN or
        LogType.HIDDEN_SINGLE_SECTION => "Hidden Single",
        LogType.NAKED_PAIR_ROW or
        LogType.NAKED_PAIR_COLUMN or
        LogType.NAKED_PAIR_SECTION => "Naked Pair",
        LogType.NAKED_TRIPLE_ROW or
        LogType.NAKED_TRIPLE_COLUMN or
        LogType.NAKED_TRIPLE_SECTION => "Naked Triple",
        LogType.NAKED_QUAD_ROW or
        LogType.NAKED_QUAD_COLUMN or
        LogType.NAKED_QUAD_SECTION => "Naked Quad",
        LogType.POINTING_PAIR_TRIPLE_ROW or
        LogType.POINTING_PAIR_TRIPLE_COLUMN => "Pointing Pair/Triple",
        LogType.ROW_BOX or
        LogType.COLUMN_BOX => "Box/Line Reduction",
        LogType.HIDDEN_PAIR_ROW or
        LogType.HIDDEN_PAIR_COLUMN or
        LogType.HIDDEN_PAIR_SECTION => "Hidden Pair",
        LogType.HIDDEN_TRIPLE_ROW or
        LogType.HIDDEN_TRIPLE_COLUMN or
        LogType.HIDDEN_TRIPLE_SECTION => "Hidden Triple",
        LogType.HIDDEN_QUAD_ROW or
        LogType.HIDDEN_QUAD_COLUMN or
        LogType.HIDDEN_QUAD_SECTION => "Hidden Quad",
        LogType.X_WING_ROW or
        LogType.X_WING_COLUMN => "X-Wing",
        LogType.Y_WING => "Y-Wing",
        LogType.XYZ_WING => "XYZ-Wing",
        LogType.SWORDFISH_ROW or
        LogType.SWORDFISH_COLUMN => "Swordfish",
        LogType.JELLYFISH_ROW or
        LogType.JELLYFISH_COLUMN => "Jellyfish",
        LogType.SIMPLE_COLORING => "Simple Coloring",
        LogType.GUESS => "Guess",
        _ => null
    };

    /// <summary>
    /// Eliminate a specific candidate value for a cell, as if the player has already removed it. Must be called after
    /// SetPuzzle and before Solve.
    /// </summary>
    /// <param name="cell">Cell position (0-80)</param>
    /// <param name="value">Candidate value to eliminate (1-9)</param>
    public void EliminatePossibility(int cell, int value)
    {
        int valIndex = value - 1;
        int valPos = GetPossibilityIndex(valIndex, cell);
        if (possibilities[valPos] == 0)
        {
            // Use round 1 (same as givens) so these eliminations are
            // never rolled back during guess backtracking.
            possibilities[valPos] = 1;
        }
    }

    /// <summary>
    /// Given a partially solved board and the player's current candidate state, solve with history recording and return
    /// a non-GIVEN LogItem describing a strategy and move. Returns null if the puzzle is already solved or unsolvable.
    /// </summary>
    /// <param name="currentBoard">81-element array: 1-9 for placed values, 0 for empty cells.</param>
    /// <param name="playerCandidates">
    /// For each empty cell, the set of candidates the player currently has visible. Null means the player has not set
    /// any candidates (use solver defaults). When provided, any solver-possible candidate NOT in this set is
    /// pre-eliminated.
    /// </param>
    /// <param name="hintIndex">
    /// Zero-based index of the hint to return, allowing the player to skip past unhelpful hints. 0 returns the first
    /// applicable hint, 1 the second, etc.
    /// </param>
    public LogItem GetHint(int[] currentBoard, HashSet<int>[] playerCandidates = null, int hintIndex = 0)
    {
        // useful for creating a unit test from a player's board state,
        //string board = string.Join(",", currentBoard);
        //string can = Util.SerializeCandidates(playerCandidates);

        SetPuzzle(currentBoard);

        // Apply the player's candidate eliminations before solving
        if (playerCandidates != null)
        {
            for (int cell = 0; cell < BOARD_SIZE; cell++)
            {
                if (currentBoard[cell] != 0)
                    continue; // cell is filled, no candidates to consider

                HashSet<int> candidates = playerCandidates[cell];
                if (candidates == null || candidates.Count == 0)
                    continue; // player has no pencil marks in this cell, use solver defaults

                for (int val = 1; val <= ROW_COL_SEC_SIZE; val++)
                {
                    if (!candidates.Contains(val))
                    {
                        EliminatePossibility(cell, val);
                    }
                }
            }
        }

        SetRecordHistory(true);
        // Call the private Solve(round, token) overload directly so that
        // the public Solve's Reset() does not discard the player's
        // candidate eliminations applied above.
        Solve(2, CancellationToken.None);

        if (!IsSolved())
            return null;

        int skipped = 0;
        foreach (var item in GetSolveInstructions())
        {
            if (item.GetLogType() == LogType.GIVEN)
                continue;

            if (skipped < hintIndex)
            {
                skipped++;
                continue;
            }

            if (item.GetLogType() == LogType.GUESS)
            {
                return null; // Don't return Guess as a hint
            }

            return item;
        }
        return null;
    }

    public bool Solve(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        Reset();
        ShuffleRandomArrays();
        return Solve(2, token);
    }

    private bool Solve(int round, CancellationToken token)
    {
        lastSolveRound = round;

        token.ThrowIfCancellationRequested();

        // When not recording history, use the fast solver with only cheap
        // strategies. The expensive strategies (triples, quads, fish, wings,
        // coloring) are unnecessary because guessing handles the rest.
        if (recordHistory)
        {
            while (SingleSolveMove(round))
            {
                if (IsSolved()) return true;
                if (IsImpossible()) return false;
            }
        }
        else
        {
            while (SingleSolveMoveFast(round))
            {
                if (IsSolved()) return true;
                if (IsImpossible()) return false;
            }
        }

        int nextGuessRound = round + 1;
        int nextRound = round + 2;
        for (int guessNumber = 0; Guess(nextGuessRound, guessNumber); guessNumber++)
        {
            if (IsImpossible() || !Solve(nextRound, token))
            {
                RollbackRound(nextRound);
                RollbackRound(nextGuessRound);
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// return true if the puzzle has no solutions at all
    /// </summary>
    public bool HasNoSolution()
    {
        return CountSolutionsLimited() == 0;
    }

    /// <summary>
    /// return true if the puzzle has a solution and only a single solution
    /// </summary>
    public bool HasUniqueSolution()
    {
        return CountSolutionsLimited() == 1;
    }

    /// <summary>
    /// return true if the puzzle has more than one solution
    /// </summary>
    public bool HasMultipleSolutions()
    {
        return CountSolutionsLimited() > 1;
    }

    /// <summary>
    /// Count the number of solutions to the puzzle
    /// </summary>
    public int CountSolutions()
    {
        return CountSolutions(false);
    }

    /// <summary>
    /// Count the number of solutions to the puzzle but return two any time there are two or more solutions. This method
    /// will run much faster than countSolutions() when there are many possible solutions and can be used when you are
    /// interested in knowing if the puzzle has zero, one, or multiple solutions.
    /// </summary>
    public int CountSolutionsLimited()
    {
        return CountSolutions(true);
    }

    private int CountSolutions(bool limitToTwo)
    {
        // Don't record history while generating.
        bool recHistory = recordHistory;
        SetRecordHistory(false);
        bool lHistory = logHistory;
        SetLogHistory(false);

        Reset();
        int solutionCount = CountSolutions(2, limitToTwo);

        // Restore recording history.
        SetRecordHistory(recHistory);
        SetLogHistory(lHistory);

        return solutionCount;
    }

    private int CountSolutions(int round, bool limitToTwo)
    {
        while (SingleSolveMoveFast(round))
        {
            if (IsSolved())
            {
                RollbackRound(round);
                return 1;
            }
            if (IsImpossible())
            {
                RollbackRound(round);
                return 0;
            }
        }

        int solutions = 0;
        int nextRound = round + 1;
        for (int guessNumber = 0; Guess(nextRound, guessNumber); guessNumber++)
        {
            solutions += CountSolutions(nextRound, limitToTwo);
            if (limitToTwo && solutions >= 2)
            {
                RollbackRound(round);
                return solutions;
            }
        }
        RollbackRound(round);
        return solutions;
    }

    private void RollbackRound(int round)
    {
        if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.ROLLBACK));
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            if (solutionRound[i] == round)
            {
                solutionRound[i] = 0;
                solution[i] = 0;
            }
        }
        for (int i = 0; i < POSSIBILITY_SIZE; i++)
        {
            if (possibilities[i] == round)
            {
                possibilities[i] = 0;
            }
        }
        while (solveInstructions.Count > 0 && solveInstructions[^1].GetRound() == round)
        {
            int i = solveInstructions.Count - 1;
            solveInstructions.RemoveAt(i);
        }
    }

    public bool IsSolved()
    {
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            if (solution[i] == 0)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsImpossible()
    {
        for (int position = 0; position < BOARD_SIZE; position++)
        {
            if (solution[position] == 0)
            {
                int count = 0;
                for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                {
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0) count++;
                }
                if (count == 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private int FindPositionWithFewestPossibilities()
    {
        int minPossibilities = 10;
        int bestPosition = 0;
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            int position = randomBoardArray[i];
            if (solution[position] == 0)
            {
                int count = 0;
                for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                {
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0) count++;
                }
                if (count < minPossibilities)
                {
                    minPossibilities = count;
                    bestPosition = position;
                }
            }
        }
        return bestPosition;
    }

    private bool Guess(int round, int guessNumber)
    {
        int localGuessCount = 0;
        int position = FindPositionWithFewestPossibilities();
        {
            for (int i = 0; i < ROW_COL_SEC_SIZE; i++)
            {
                int valIndex = randomPossibilityArray[i];
                int valPos = GetPossibilityIndex(valIndex, position);
                if (possibilities[valPos] == 0)
                {
                    if (localGuessCount == guessNumber)
                    {
                        int value = valIndex + 1;
                        if (logHistory || recordHistory)
                            AddHistoryItem(new LogItem(round, LogType.GUESS, value, position));
                        Mark(position, round, value);
                        return true;
                    }
                    localGuessCount++;
                }
            }
        }
        return false;
    }


    /// <summary>
    /// Lightweight solve move used during solution counting and generation. Only uses cheap strategies that don't
    /// allocate heap objects. The expensive strategies (hidden triples/quads, fish patterns, wings, coloring) are
    /// skipped since guessing handles the rest efficiently.
    /// </summary>
    private bool SingleSolveMoveFast(int round)
    {
        if (OnlyPossibilityForCell(round)) return true;
        if (OnlyValueInSection(round)) return true;
        if (OnlyValueInRow(round)) return true;
        if (OnlyValueInColumn(round)) return true;
        if (HandleNakedPairs(round)) return true;
        if (PointingRowReduction(round)) return true;
        if (PointingColumnReduction(round)) return true;
        if (RowBoxReduction(round)) return true;
        if (ColBoxReduction(round)) return true;
        if (HiddenPairInRow(round)) return true;
        if (HiddenPairInColumn(round)) return true;
        if (HiddenPairInSection(round)) return true;
        return false;
    }

    private bool SingleSolveMove(int round)
    {
        if (OnlyPossibilityForCell(round)) return true;        // Naked Single
        if (OnlyValueInSection(round)) return true;            // Hidden Single
        if (OnlyValueInRow(round)) return true;                // Hidden Single
        if (OnlyValueInColumn(round)) return true;             // Hidden Single
        if (HandleNakedPairs(round)) return true;              // Naked Pair
        if (PointingRowReduction(round)) return true;          // Pointing Pair/Triple
        if (PointingColumnReduction(round)) return true;       // Pointing Pair/Triple
        if (RowBoxReduction(round)) return true;               // Box/Line Reduction
        if (ColBoxReduction(round)) return true;               // Box/Line Reduction
        if (HiddenPairInRow(round)) return true;               // Hidden Pair
        if (HiddenPairInColumn(round)) return true;            // Hidden Pair
        if (HiddenPairInSection(round)) return true;           // Hidden Pair
        if (HandleNakedTriples(round)) return true;            // Naked Triple
        if (HandleNakedQuads(round)) return true;              // Naked Quad
        if (HiddenTripleInRow(round)) return true;             // Hidden Triple
        if (HiddenTripleInColumn(round)) return true;          // Hidden Triple
        if (HiddenTripleInSection(round)) return true;         // Hidden Triple
        if (HiddenQuadInRow(round)) return true;               // Hidden Quad
        if (HiddenQuadInColumn(round)) return true;            // Hidden Quad
        if (HiddenQuadInSection(round)) return true;           // Hidden Quad
        if (XWingInRows(round)) return true;                   // X-Wing
        if (XWingInColumns(round)) return true;                // X-Wing
        if (YWing(round)) return true;                         // Y-Wing
        if (XyzWing(round)) return true;                       // XYZ-Wing
        if (SwordfishInRows(round)) return true;               // Swordfish
        if (SwordfishInColumns(round)) return true;            // Swordfish
        if (JellyfishInRows(round)) return true;               // Jellyfish
        if (JellyfishInColumns(round)) return true;            // Jellyfish
        if (SimpleColoring(round)) return true;                // Simple Coloring
        return false;
    }

    /// <summary>
    /// Looking at columns, if a value in found only in one section (box), then that candidate value is eliminated from
    /// the other cells in the other columns in that section.
    /// </summary>
    private bool ColBoxReduction(int round)
    {
        // valIndex: index to possibilities in a cell (0 - 8) for the values (1 - 9) 
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            // check each column in the puzzle
            for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
            {
                // cell at the top of the column
                int colStart = ColumnToFirstCell(col);
                bool inOneBox = true;
                int colBox = -1;
                // for each section (box) intersecting the column
                foreach (int section in SectionLayout.ColumnToSections(col))
                {
                    // for each row in this section
                    foreach (var row in SectionLayout.SectionToSectionRowsByCol(section, col))
                    {
                        int position = RowColumnToCell(row, col);
                        int valPos = GetPossibilityIndex(valIndex, position);
                        if (possibilities[valPos] == 0)
                        {
                            if (colBox == -1 || colBox == section)
                            {
                                colBox = section;
                            }
                            else
                            {
                                inOneBox = false;
                            }
                        }
                    }
                }
                if (inOneBox && colBox != -1)
                {
                    bool doneSomething = false;
                    // for each cell in the section
                    foreach (int position in SectionLayout.SectionToSectionCells(colBox))
                    {
                        int col2 = CellToColumn(position);
                        int valPos = GetPossibilityIndex(valIndex, position);
                        if (col != col2 && possibilities[valPos] == 0)
                        {
                            possibilities[valPos] = round;
                            doneSomething = true;
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.COLUMN_BOX, valIndex + 1, colStart));
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Looking at rows, if a value in found only in one section (box), then that possibility value is eliminated from
    /// the other cells in other rows in that section.
    /// </summary>
    private bool RowBoxReduction(int round)
    {
        // valIndex: index to possibilities in a cell (0 - 8) for the values (1 - 9) 
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            // check each row in the puzzle
            for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
            {
                // cell index of the first cell in the row
                int rowStart = RowToFirstCell(row);
                bool inOneBox = true;
                int rowBox = -1;
                // for each section (box) intersecting the row
                foreach (int section in SectionLayout.RowToSections(row))
                {
                    // for each column within the section
                    foreach (int col in SectionLayout.SectionToSectionColsByRow(section, row))
                    {
                        int position = RowColumnToCell(row, col);
                        int valPos = GetPossibilityIndex(valIndex, position);
                        // if the possibility has not been eliminated
                        // see if it is in the same section as the same possibility in another section
                        if (possibilities[valPos] == 0)
                        {
                            if (rowBox == -1 || rowBox == section)
                            {
                                rowBox = section;
                            }
                            else
                            {
                                inOneBox = false;
                            }
                        }
                    }
                }
                if (inOneBox && rowBox != -1)
                {
                    bool doneSomething = false;
                    // for each cell in the section
                    foreach (int position in SectionLayout.SectionToSectionCells(rowBox))
                    {
                        int row2 = CellToRow(position);
                        int valPos = GetPossibilityIndex(valIndex, position);
                        if (row != row2 && possibilities[valPos] == 0)
                        {
                            possibilities[valPos] = round;
                            doneSomething = true;
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.ROW_BOX, valIndex + 1, rowStart));
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Pointing pairs and pointing triples across rows. If a section contains a possibility value only in a single row,
    /// then that possibility value is eliminated from the other cells in that row.
    /// </summary>
    private bool PointingRowReduction(int round)
    {
        // valIndex: index to possibilities in a cell (0 - 8) for the values (1 - 9) 
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            // check in each of the nine sections
            for (int section = 0; section < ROW_COL_SEC_SIZE; section++)
            {
                bool inOneRow = true;
                int boxRow = -1;
                // for each row in the section
                foreach (int row in SectionLayout.SectionToSectionRows(section))
                {
                    // for each column in the section-row
                    foreach (int col in SectionLayout.SectionToSectionColsByRow(section, row))
                    {
                        int cell = RowColumnToCell(row, col);
                        int valPos = GetPossibilityIndex(valIndex, cell);
                        if (possibilities[valPos] == 0)
                        {
                            if (boxRow == -1 || boxRow == row)
                            {
                                boxRow = row;
                            }
                            else
                            {
                                inOneRow = false;
                            }
                        }
                    }
                }
                if (inOneRow && boxRow != -1)
                {
                    bool doneSomething = false;
                    int rowStart = RowToFirstCell(boxRow);

                    // across all the cells in the row
                    for (int i = 0; i < ROW_COL_SEC_SIZE; i++)
                    {
                        int position = rowStart + i;
                        int section2 = CellToSection(position);
                        int valPos = GetPossibilityIndex(valIndex, position);
                        if (section != section2 && possibilities[valPos] == 0)
                        {
                            possibilities[valPos] = round;
                            doneSomething = true;
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory)
                        {
                            var logItem = new LogItem(round, LogType.POINTING_PAIR_TRIPLE_ROW, valIndex + 1, rowStart)
                            {
                                DetailedMessage = $"Pointing Pair/Triple: candidate {valIndex + 1} in box {section + 1} is confined to row {boxRow + 1}. " +
                                    $"Eliminate {valIndex + 1} from other cells in row {boxRow + 1}."
                            };
                            AddHistoryItem(logItem);
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Pointing pairs and pointing triples down columns
    /// column, then that possibility value is eliminated from the other cells in that column.
    /// </summary>
    private bool PointingColumnReduction(int round)
    {
        // valIndex: index to possibilities in a cell (0 - 8) for the values (1 - 9) 
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            // check in each of the nine sections
            for (int section = 0; section < ROW_COL_SEC_SIZE; section++)
            {
                bool inOneCol = true;
                int boxCol = -1;
                // for each column in the section
                foreach (int col in SectionLayout.SectionToSectionCols(section))
                {
                    // for each row in the section
                    foreach (int row in SectionLayout.SectionToSectionRowsByCol(section, col))
                    {
                        int cell = RowColumnToCell(row, col);
                        int valPos = GetPossibilityIndex(valIndex, cell);
                        if (possibilities[valPos] == 0)
                        {
                            if (boxCol == -1 || boxCol == col)
                            {
                                boxCol = col;
                            }
                            else
                            {
                                inOneCol = false;
                            }
                        }
                    }
                }
                if (inOneCol && boxCol != -1)
                {
                    bool doneSomething = false;
                    int colStart = ColumnToFirstCell(boxCol);

                    // down all the cells in this column
                    for (int i = 0; i < ROW_COL_SEC_SIZE; i++)
                    {
                        int position = colStart + (ROW_COL_SEC_SIZE * i);
                        int section2 = CellToSection(position);
                        int valPos = GetPossibilityIndex(valIndex, position);
                        if (section != section2 && possibilities[valPos] == 0)
                        {
                            possibilities[valPos] = round;
                            doneSomething = true;
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory)
                        {
                            var logItem = new LogItem(round, LogType.POINTING_PAIR_TRIPLE_COLUMN, valIndex + 1, colStart)
                            {
                                DetailedMessage = $"Pointing Pair/Triple: candidate {valIndex + 1} in box {section + 1} is confined to column {boxCol + 1}. " +
                                    $"Eliminate {valIndex + 1} from other cells in column {boxCol + 1}."
                            };
                            AddHistoryItem(logItem);
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private int CountPossibilities(int position)
    {
        int count = 0;
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            int valPos = GetPossibilityIndex(valIndex, position);
            if (possibilities[valPos] == 0) count++;
        }
        return count;
    }

    /// <summary>
    /// Returns a comma-separated string of candidate values (1-9) for the given cell position.
    /// </summary>
    private string GetCandidatesString(int position)
    {
        StringBuilder sb = new();
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            int valPos = GetPossibilityIndex(valIndex, position);
            if (possibilities[valPos] == 0)
            {
                if (sb.Length > 0) sb.Append(',');
                sb.Append(valIndex + 1);
            }
        }
        return sb.ToString();
    }

    private bool ArePossibilitiesSame(int position1, int position2)
    {
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            int valPos1 = GetPossibilityIndex(valIndex, position1);
            int valPos2 = GetPossibilityIndex(valIndex, position2);
            if ((possibilities[valPos1] == 0 || possibilities[valPos2] == 0) && (possibilities[valPos1] != 0 || possibilities[valPos2] != 0))
            {
                return false;
            }
        }
        return true;
    }

    private bool RemovePossibilitiesInOneFromTwo(int position1, int position2, int round)
    {
        bool doneSomething = false;
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            int valPos1 = GetPossibilityIndex(valIndex, position1);
            int valPos2 = GetPossibilityIndex(valIndex, position2);
            if (possibilities[valPos1] == 0 && possibilities[valPos2] == 0)
            {
                possibilities[valPos2] = round;
                doneSomething = true;
            }
        }
        return doneSomething;
    }

    private bool HiddenPairInColumn(int round)
    {
        for (int column = 0; column < ROW_COL_SEC_SIZE; column++)
        {
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                int r1 = -1;
                int r2 = -1;
                int valCount = 0;
                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                {
                    int position = RowColumnToCell(row, column);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        if (r1 == -1 || r1 == row)
                        {
                            r1 = row;
                        }
                        else if (r2 == -1 || r2 == row)
                        {
                            r2 = row;
                        }
                        valCount++;
                    }
                }
                if (valCount == 2)
                {
                    for (int valIndex2 = valIndex + 1; valIndex2 < ROW_COL_SEC_SIZE; valIndex2++)
                    {
                        int r3 = -1;
                        int r4 = -1;
                        int valCount2 = 0;
                        for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                        {
                            int position = RowColumnToCell(row, column);
                            int valPos = GetPossibilityIndex(valIndex2, position);
                            if (possibilities[valPos] == 0)
                            {
                                if (r3 == -1 || r3 == row)
                                {
                                    r3 = row;
                                }
                                else if (r4 == -1 || r4 == row)
                                {
                                    r4 = row;
                                }
                                valCount2++;
                            }
                        }
                        if (valCount2 == 2 && r1 == r3 && r2 == r4)
                        {
                            bool doneSomething = false;
                            for (int valIndex3 = 0; valIndex3 < ROW_COL_SEC_SIZE; valIndex3++)
                            {
                                if (valIndex3 != valIndex && valIndex3 != valIndex2)
                                {
                                    int position1 = RowColumnToCell(r1, column);
                                    int position2 = RowColumnToCell(r2, column);
                                    int valPos1 = GetPossibilityIndex(valIndex3, position1);
                                    int valPos2 = GetPossibilityIndex(valIndex3, position2);
                                    if (possibilities[valPos1] == 0)
                                    {
                                        possibilities[valPos1] = round;
                                        doneSomething = true;
                                    }
                                    if (possibilities[valPos2] == 0)
                                    {
                                        possibilities[valPos2] = round;
                                        doneSomething = true;
                                    }
                                }
                            }
                            if (doneSomething)
                            {
                                if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.HIDDEN_PAIR_COLUMN, valIndex + 1, RowColumnToCell(r1, column)));
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool HiddenPairInSection(int round)
    {
        // for each section in the puzzle
        for (int section = 0; section < ROW_COL_SEC_SIZE; section++)
        {
            // valIndex: index to possibilities in a cell (0 - 8) for the values (1 - 9) 
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                int si1 = -1;
                int si2 = -1;
                int valCount = 0;
                // for each position (cell) in the section
                for (int secInd = 0; secInd < ROW_COL_SEC_SIZE; secInd++)
                {
                    int position = SectionToCell(section, secInd);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        if (si1 == -1 || si1 == secInd)
                        {
                            si1 = secInd;
                        }
                        else if (si2 == -1 || si2 == secInd)
                        {
                            si2 = secInd;
                        }
                        valCount++;
                    }
                }
                if (valCount == 2)
                {
                    for (int valIndex2 = valIndex + 1; valIndex2 < ROW_COL_SEC_SIZE; valIndex2++)
                    {
                        int si3 = -1;
                        int si4 = -1;
                        int valCount2 = 0;
                        for (int secInd = 0; secInd < ROW_COL_SEC_SIZE; secInd++)
                        {
                            int position = SectionToCell(section, secInd);
                            int valPos = GetPossibilityIndex(valIndex2, position);
                            if (possibilities[valPos] == 0)
                            {
                                if (si3 == -1 || si3 == secInd)
                                {
                                    si3 = secInd;
                                }
                                else if (si4 == -1 || si4 == secInd)
                                {
                                    si4 = secInd;
                                }
                                valCount2++;
                            }
                        }
                        if (valCount2 == 2 && si1 == si3 && si2 == si4)
                        {
                            bool doneSomething = false;
                            for (int valIndex3 = 0; valIndex3 < ROW_COL_SEC_SIZE; valIndex3++)
                            {
                                if (valIndex3 != valIndex && valIndex3 != valIndex2)
                                {
                                    int position1 = SectionToCell(section, si1);
                                    int position2 = SectionToCell(section, si2);
                                    int valPos1 = GetPossibilityIndex(valIndex3, position1);
                                    int valPos2 = GetPossibilityIndex(valIndex3, position2);
                                    if (possibilities[valPos1] == 0)
                                    {
                                        possibilities[valPos1] = round;
                                        doneSomething = true;
                                    }
                                    if (possibilities[valPos2] == 0)
                                    {
                                        possibilities[valPos2] = round;
                                        doneSomething = true;
                                    }
                                }
                            }
                            if (doneSomething)
                            {
                                if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.HIDDEN_PAIR_SECTION, valIndex + 1, SectionToCell(section, si1)));
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool HiddenPairInRow(int round)
    {
        for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
        {
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                int c1 = -1;
                int c2 = -1;
                int valCount = 0;
                for (int column = 0; column < ROW_COL_SEC_SIZE; column++)
                {
                    int position = RowColumnToCell(row, column);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        if (c1 == -1 || c1 == column)
                        {
                            c1 = column;
                        }
                        else if (c2 == -1 || c2 == column)
                        {
                            c2 = column;
                        }
                        valCount++;
                    }
                }
                if (valCount == 2)
                {
                    for (int valIndex2 = valIndex + 1; valIndex2 < ROW_COL_SEC_SIZE; valIndex2++)
                    {
                        int c3 = -1;
                        int c4 = -1;
                        int valCount2 = 0;
                        for (int column = 0; column < ROW_COL_SEC_SIZE; column++)
                        {
                            int position = RowColumnToCell(row, column);
                            int valPos = GetPossibilityIndex(valIndex2, position);
                            if (possibilities[valPos] == 0)
                            {
                                if (c3 == -1 || c3 == column)
                                {
                                    c3 = column;
                                }
                                else if (c4 == -1 || c4 == column)
                                {
                                    c4 = column;
                                }
                                valCount2++;
                            }
                        }
                        if (valCount2 == 2 && c1 == c3 && c2 == c4)
                        {
                            bool doneSomething = false;
                            for (int valIndex3 = 0; valIndex3 < ROW_COL_SEC_SIZE; valIndex3++)
                            {
                                if (valIndex3 != valIndex && valIndex3 != valIndex2)
                                {
                                    int position1 = RowColumnToCell(row, c1);
                                    int position2 = RowColumnToCell(row, c2);
                                    int valPos1 = GetPossibilityIndex(valIndex3, position1);
                                    int valPos2 = GetPossibilityIndex(valIndex3, position2);
                                    if (possibilities[valPos1] == 0)
                                    {
                                        possibilities[valPos1] = round;
                                        doneSomething = true;
                                    }
                                    if (possibilities[valPos2] == 0)
                                    {
                                        possibilities[valPos2] = round;
                                        doneSomething = true;
                                    }
                                }
                            }
                            if (doneSomething)
                            {
                                if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.HIDDEN_PAIR_ROW, valIndex + 1, RowColumnToCell(row, c1)));
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Hidden Triple in Row: Find three values that each appear in only 2 or 3 cells within a row, and the union of
    /// those cells is exactly 3. All other candidates can be eliminated from those 3 cells.
    /// </summary>
    private bool HiddenTripleInRow(int round)
    {
        for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
        {
            // For each value, build a bitmask of columns where it appears in this row
            int[] valColMask = new int[ROW_COL_SEC_SIZE];
            int[] valColCount = new int[ROW_COL_SEC_SIZE];
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                {
                    int position = RowColumnToCell(row, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        valColMask[valIndex] |= (1 << col);
                        valColCount[valIndex]++;
                    }
                }
            }

            if (HiddenSubsetInUnit(valColMask, valColCount, 3, round, (col) => RowColumnToCell(row, col), LogType.HIDDEN_TRIPLE_ROW, RowColumnToCell(row, 0)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Hidden Triple in Column: Find three values that each appear in only 2 or 3 cells within a column, and the union
    /// of those cells is exactly 3.
    /// </summary>
    private bool HiddenTripleInColumn(int round)
    {
        for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
        {
            int[] valRowMask = new int[ROW_COL_SEC_SIZE];
            int[] valRowCount = new int[ROW_COL_SEC_SIZE];
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                {
                    int position = RowColumnToCell(row, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        valRowMask[valIndex] |= (1 << row);
                        valRowCount[valIndex]++;
                    }
                }
            }

            if (HiddenSubsetInUnit(valRowMask, valRowCount, 3, round, (row) => RowColumnToCell(row, col), LogType.HIDDEN_TRIPLE_COLUMN, RowColumnToCell(0, col)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Hidden Triple in Section: Find three values that each appear in only 2 or 3 cells within a section, and the
    /// union of those cells is exactly 3.
    /// </summary>
    private bool HiddenTripleInSection(int round)
    {
        for (int sec = 0; sec < ROW_COL_SEC_SIZE; sec++)
        {
            int[] valCellMask = new int[ROW_COL_SEC_SIZE];
            int[] valCellCount = new int[ROW_COL_SEC_SIZE];
            int secIndex = 0;
            foreach (int position in SectionLayout.SectionToSectionCells(sec))
            {
                for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                {
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        valCellMask[valIndex] |= (1 << secIndex);
                        valCellCount[valIndex]++;
                    }
                }
                secIndex++;
            }

            int localSec = sec;
            if (HiddenSubsetInUnit(valCellMask, valCellCount, 3, round, (idx) => SectionToCell(localSec, idx), LogType.HIDDEN_TRIPLE_SECTION, SectionToFirstCell(sec)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Hidden Quad in Row: Find four values that each appear in only 2, 3, or 4 cells within a row, and the union of
    /// those cells is exactly 4.
    /// </summary>
    private bool HiddenQuadInRow(int round)
    {
        for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
        {
            int[] valColMask = new int[ROW_COL_SEC_SIZE];
            int[] valColCount = new int[ROW_COL_SEC_SIZE];
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                {
                    int position = RowColumnToCell(row, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        valColMask[valIndex] |= (1 << col);
                        valColCount[valIndex]++;
                    }
                }
            }

            if (HiddenSubsetInUnit(valColMask, valColCount, 4, round, (col) => RowColumnToCell(row, col), LogType.HIDDEN_QUAD_ROW, RowColumnToCell(row, 0)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Hidden Quad in Column: Find four values that each appear in only 2, 3, or 4 cells within a column, and the union
    /// of those cells is exactly 4.
    /// </summary>
    private bool HiddenQuadInColumn(int round)
    {
        for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
        {
            int[] valRowMask = new int[ROW_COL_SEC_SIZE];
            int[] valRowCount = new int[ROW_COL_SEC_SIZE];
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                {
                    int position = RowColumnToCell(row, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        valRowMask[valIndex] |= (1 << row);
                        valRowCount[valIndex]++;
                    }
                }
            }

            if (HiddenSubsetInUnit(valRowMask, valRowCount, 4, round, (row) => RowColumnToCell(row, col), LogType.HIDDEN_QUAD_COLUMN, RowColumnToCell(0, col)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Hidden Quad in Section: Find four values that each appear in only 2, 3, or 4 cells within a section, and the
    /// union of those cells is exactly 4.
    /// </summary>
    private bool HiddenQuadInSection(int round)
    {
        for (int sec = 0; sec < ROW_COL_SEC_SIZE; sec++)
        {
            int[] valCellMask = new int[ROW_COL_SEC_SIZE];
            int[] valCellCount = new int[ROW_COL_SEC_SIZE];
            int secIndex = 0;
            foreach (int position in SectionLayout.SectionToSectionCells(sec))
            {
                for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                {
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        valCellMask[valIndex] |= (1 << secIndex);
                        valCellCount[valIndex]++;
                    }
                }
                secIndex++;
            }

            int localSec = sec;
            if (HiddenSubsetInUnit(valCellMask, valCellCount, 4, round, (idx) => SectionToCell(localSec, idx), LogType.HIDDEN_QUAD_SECTION, SectionToFirstCell(sec)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Generalized hidden subset finder. Given bitmasks of cell positions per value in a unit, find N values
    /// (subsetSize) whose combined cell positions span exactly N cells. Eliminate all other candidates from those N
    /// cells.
    /// </summary>
    /// <param name="valPosMask">Bitmask of cell positions per value index (0-8).</param>
    /// <param name="valPosCount">Count of cell positions per value index.</param>
    /// <param name="subsetSize">Size of the hidden subset (3 for triple, 4 for quad).</param>
    /// <param name="round">Round for marking eliminations.</param>
    /// <param name="indexToCell">Converts a bit index (0-8) to a board cell position (0-80).</param>
    /// <param name="logType">Log type for history.</param>
    /// <param name="logPosition">Position to log in history.</param>
    private bool HiddenSubsetInUnit(int[] valPosMask, int[] valPosCount, int subsetSize, int round, Func<int, int> indexToCell, LogType logType, int logPosition)
    {
        // Collect value indices that have 2..subsetSize positions in this unit
        List<int> candidates = [];
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            if (valPosCount[valIndex] >= 2 && valPosCount[valIndex] <= subsetSize)
            {
                candidates.Add(valIndex);
            }
        }

        if (candidates.Count < subsetSize) return false;

        // Try all combinations of subsetSize values from the candidates
        int[] combo = new int[subsetSize];
        return HiddenSubsetRecurse(candidates, valPosMask, subsetSize, 0, 0, combo, round, indexToCell, logType, logPosition);
    }

    /// <summary>
    /// Recursive combination generator for hidden subset search.
    /// </summary>
    private bool HiddenSubsetRecurse(List<int> candidates, int[] valPosMask, int subsetSize, int start, int depth, int[] combo, int round, Func<int, int> indexToCell, LogType logType, int logPosition)
    {
        if (depth == subsetSize)
        {
            // Compute the union of cell positions for the selected values
            int unionMask = 0;
            for (int d = 0; d < subsetSize; d++)
            {
                unionMask |= valPosMask[combo[d]];
            }

            if (BitCount(unionMask) != subsetSize) return false;

            // Found a hidden subset: eliminate all OTHER candidates from these cells
            bool doneSomething = false;
            for (int bit = 0; bit < ROW_COL_SEC_SIZE; bit++)
            {
                if ((unionMask & (1 << bit)) == 0) continue;

                int position = indexToCell(bit);
                for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                {
                    // Skip values that are part of the hidden subset
                    bool inSubset = false;
                    for (int d = 0; d < subsetSize; d++)
                    {
                        if (combo[d] == valIndex)
                        {
                            inSubset = true;
                            break;
                        }
                    }
                    if (inSubset) continue;

                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        possibilities[valPos] = round;
                        doneSomething = true;
                    }
                }
            }
            if (doneSomething)
            {
                if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, logType, combo[0] + 1, logPosition));
            }
            return doneSomething;
        }

        for (int i = start; i <= candidates.Count - (subsetSize - depth); i++)
        {
            combo[depth] = candidates[i];

            // Early pruning: check if union so far already exceeds subsetSize
            if (depth > 0)
            {
                int partialUnion = 0;
                for (int d = 0; d <= depth; d++)
                {
                    partialUnion |= valPosMask[combo[d]];
                }
                if (BitCount(partialUnion) > subsetSize) continue;
            }

            if (HiddenSubsetRecurse(candidates, valPosMask, subsetSize, i + 1, depth + 1, combo, round, indexToCell, logType, logPosition))
                return true;
        }
        return false;
    }

    /// <summary>
    /// X-Wing strategy scanning rows: For a given candidate value, if it appears in exactly two columns in each of two
    /// different rows, and those columns are the same, then that candidate can be eliminated from all other cells in
    /// those two columns.
    /// </summary>
    private bool XWingInRows(int round)
    {
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            // For each row, find which columns still have this candidate
            for (int row1 = 0; row1 < ROW_COL_SEC_SIZE - 1; row1++)
            {
                int col1 = -1;
                int col2 = -1;
                int count = 0;
                for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                {
                    int position = RowColumnToCell(row1, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        if (col1 == -1)
                        {
                            col1 = col;
                        }
                        else if (col2 == -1)
                        {
                            col2 = col;
                        }
                        count++;
                    }
                }

                // The candidate must appear in exactly two columns in this row
                if (count != 2) continue;

                // Look for a second row with the same two columns
                for (int row2 = row1 + 1; row2 < ROW_COL_SEC_SIZE; row2++)
                {
                    int col3 = -1;
                    int col4 = -1;
                    int count2 = 0;
                    for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                    {
                        int position = RowColumnToCell(row2, col);
                        int valPos = GetPossibilityIndex(valIndex, position);
                        if (possibilities[valPos] == 0)
                        {
                            if (col3 == -1)
                            {
                                col3 = col;
                            }
                            else if (col4 == -1)
                            {
                                col4 = col;
                            }
                            count2++;
                        }
                    }

                    // Must also have exactly two, in the same columns
                    if (count2 != 2 || col1 != col3 || col2 != col4) continue;

                    // Eliminate this candidate from all other cells in col1 and col2
                    bool doneSomething = false;
                    for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                    {
                        if (row == row1 || row == row2) continue;

                        int posA = RowColumnToCell(row, col1);
                        int valPosA = GetPossibilityIndex(valIndex, posA);
                        if (possibilities[valPosA] == 0)
                        {
                            possibilities[valPosA] = round;
                            doneSomething = true;
                        }

                        int posB = RowColumnToCell(row, col2);
                        int valPosB = GetPossibilityIndex(valIndex, posB);
                        if (possibilities[valPosB] == 0)
                        {
                            possibilities[valPosB] = round;
                            doneSomething = true;
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory)
                        {
                            var logItem = new LogItem(round, LogType.X_WING_ROW, valIndex + 1, RowColumnToCell(row1, col1))
                            {
                                DetailedMessage = $"X-Wing (rows): candidate {valIndex + 1} in rows {row1 + 1} and {row2 + 1}, " +
                                    $"columns {col1 + 1} and {col2 + 1}. " +
                                    $"Eliminate {valIndex + 1} from other cells in columns {col1 + 1} and {col2 + 1}."
                            };
                            AddHistoryItem(logItem);
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// X-Wing strategy scanning columns
    /// different columns, and those rows are the same, then that candidate can be eliminated from all other cells in
    /// those two rows.
    /// </summary>
    private bool XWingInColumns(int round)
    {
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            // For each column, find which rows still have this candidate
            for (int col1 = 0; col1 < ROW_COL_SEC_SIZE - 1; col1++)
            {
                int row1 = -1;
                int row2 = -1;
                int count = 0;
                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                {
                    int position = RowColumnToCell(row, col1);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        if (row1 == -1)
                        {
                            row1 = row;
                        }
                        else if (row2 == -1)
                        {
                            row2 = row;
                        }
                        count++;
                    }
                }

                // The candidate must appear in exactly two rows in this column
                if (count != 2) continue;

                // Look for a second column with the same two rows
                for (int col2 = col1 + 1; col2 < ROW_COL_SEC_SIZE; col2++)
                {
                    int row3 = -1;
                    int row4 = -1;
                    int count2 = 0;
                    for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                    {
                        int position = RowColumnToCell(row, col2);
                        int valPos = GetPossibilityIndex(valIndex, position);
                        if (possibilities[valPos] == 0)
                        {
                            if (row3 == -1)
                            {
                                row3 = row;
                            }
                            else if (row4 == -1)
                            {
                                row4 = row;
                            }
                            count2++;
                        }
                    }

                    // Must also have exactly two, in the same rows
                    if (count2 != 2 || row1 != row3 || row2 != row4) continue;

                    // Eliminate this candidate from all other cells in row1 and row2
                    bool doneSomething = false;
                    for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                    {
                        if (col == col1 || col == col2) continue;

                        int posA = RowColumnToCell(row1, col);
                        int valPosA = GetPossibilityIndex(valIndex, posA);
                        if (possibilities[valPosA] == 0)
                        {
                            possibilities[valPosA] = round;
                            doneSomething = true;
                        }

                        int posB = RowColumnToCell(row2, col);
                        int valPosB = GetPossibilityIndex(valIndex, posB);
                        if (possibilities[valPosB] == 0)
                        {
                            possibilities[valPosB] = round;
                            doneSomething = true;
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory)
                        {
                            var logItem = new LogItem(round, LogType.X_WING_COLUMN, valIndex + 1, RowColumnToCell(row1, col1))
                            {
                                DetailedMessage = $"X-Wing (columns): candidate {valIndex + 1} in columns {col1 + 1} and {col2 + 1}, " +
                                    $"rows {row1 + 1} and {row2 + 1}. " +
                                    $"Eliminate {valIndex + 1} from other cells in rows {row1 + 1} and {row2 + 1}."
                            };
                            AddHistoryItem(logItem);
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Swordfish strategy scanning rows: For a given candidate value, find three rows where the candidate appears in
    /// only 2 or 3 columns, and the union of those columns across all three rows is exactly 3. The candidate can then
    /// be eliminated from all other cells in those 3 columns.
    /// </summary>
    private bool SwordfishInRows(int round)
    {
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            // For each row, compute a bitmask of columns containing this candidate
            int[] rowColMask = new int[ROW_COL_SEC_SIZE];
            int[] rowColCount = new int[ROW_COL_SEC_SIZE];
            for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
            {
                for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                {
                    int position = RowColumnToCell(row, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        rowColMask[row] |= (1 << col);
                        rowColCount[row]++;
                    }
                }
            }

            // Find three rows each with 2 or 3 candidate columns whose union is exactly 3 columns
            for (int r1 = 0; r1 < ROW_COL_SEC_SIZE - 2; r1++)
            {
                if (rowColCount[r1] < 2 || rowColCount[r1] > 3) continue;

                for (int r2 = r1 + 1; r2 < ROW_COL_SEC_SIZE - 1; r2++)
                {
                    if (rowColCount[r2] < 2 || rowColCount[r2] > 3) continue;

                    int union12 = rowColMask[r1] | rowColMask[r2];
                    // Early exit: if first two rows already span more than 3 columns, skip
                    if (BitCount(union12) > 3) continue;

                    for (int r3 = r2 + 1; r3 < ROW_COL_SEC_SIZE; r3++)
                    {
                        if (rowColCount[r3] < 2 || rowColCount[r3] > 3) continue;

                        int unionMask = union12 | rowColMask[r3];
                        if (BitCount(unionMask) != 3) continue;

                        // We have a Swordfish: eliminate this candidate from the 3 columns
                        // in all rows except r1, r2, r3
                        bool doneSomething = false;
                        for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                        {
                            if (row == r1 || row == r2 || row == r3) continue;

                            for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                            {
                                if ((unionMask & (1 << col)) == 0) continue;

                                int position = RowColumnToCell(row, col);
                                int valPos = GetPossibilityIndex(valIndex, position);
                                if (possibilities[valPos] == 0)
                                {
                                    possibilities[valPos] = round;
                                    doneSomething = true;
                                }
                            }
                        }
                        if (doneSomething)
                        {
                            if (logHistory || recordHistory)
                            {
                                var logItem = new LogItem(round, LogType.SWORDFISH_ROW, valIndex + 1, RowColumnToCell(r1, 0))
                                {
                                    DetailedMessage = $"Swordfish (rows): candidate {valIndex + 1} in rows {r1 + 1}, {r2 + 1}, and {r3 + 1}, " +
                                        $"columns {BitmaskToString(unionMask)}. " +
                                        $"Eliminate {valIndex + 1} from other cells in columns {BitmaskToString(unionMask)}."
                                };
                                AddHistoryItem(logItem);
                            }
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Swordfish strategy scanning columns
    /// in only 2 or 3 rows, and the union of those rows across all three columns is exactly 3. The candidate can then
    /// be eliminated from all other cells in those 3 rows.
    /// </summary>
    private bool SwordfishInColumns(int round)
    {
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            // For each column, compute a bitmask of rows containing this candidate
            int[] colRowMask = new int[ROW_COL_SEC_SIZE];
            int[] colRowCount = new int[ROW_COL_SEC_SIZE];
            for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
            {
                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                {
                    int position = RowColumnToCell(row, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        colRowMask[col] |= (1 << row);
                        colRowCount[col]++;
                    }
                }
            }

            // Find three columns each with 2 or 3 candidate rows whose union is exactly 3 rows
            for (int c1 = 0; c1 < ROW_COL_SEC_SIZE - 2; c1++)
            {
                if (colRowCount[c1] < 2 || colRowCount[c1] > 3) continue;

                for (int c2 = c1 + 1; c2 < ROW_COL_SEC_SIZE - 1; c2++)
                {
                    if (colRowCount[c2] < 2 || colRowCount[c2] > 3) continue;

                    int union12 = colRowMask[c1] | colRowMask[c2];
                    if (BitCount(union12) > 3) continue;

                    for (int c3 = c2 + 1; c3 < ROW_COL_SEC_SIZE; c3++)
                    {
                        if (colRowCount[c3] < 2 || colRowCount[c3] > 3) continue;

                        int unionMask = union12 | colRowMask[c3];
                        if (BitCount(unionMask) != 3) continue;

                        // We have a Swordfish: eliminate this candidate from the 3 rows
                        // in all columns except c1, c2, c3
                        bool doneSomething = false;
                        for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                        {
                            if (col == c1 || col == c2 || col == c3) continue;

                            for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                            {
                                if ((unionMask & (1 << row)) == 0) continue;

                                int position = RowColumnToCell(row, col);
                                int valPos = GetPossibilityIndex(valIndex, position);
                                if (possibilities[valPos] == 0)
                                {
                                    possibilities[valPos] = round;
                                    doneSomething = true;
                                }
                            }
                        }
                        if (doneSomething)
                        {
                            if (logHistory || recordHistory)
                            {
                                var logItem = new LogItem(round, LogType.SWORDFISH_COLUMN, valIndex + 1, RowColumnToCell(0, c1))
                                {
                                    DetailedMessage = $"Swordfish (columns): candidate {valIndex + 1} in columns {c1 + 1}, {c2 + 1}, and {c3 + 1}, " +
                                        $"rows {BitmaskToString(unionMask)}. " +
                                        $"Eliminate {valIndex + 1} from other cells in rows {BitmaskToString(unionMask)}."
                                };
                                AddHistoryItem(logItem);
                            }
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Count the number of set bits
    /// </summary>
    private static int BitCount(int value)
    {
        int count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }

    /// <summary>
    /// Converts a bitmask of positions (0-8) to a comma-separated string of 1-indexed values.
    /// </summary>
    private static string BitmaskToString(int mask)
    {
        StringBuilder sb = new();
        for (int i = 0; i < ROW_COL_SEC_SIZE; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(i + 1);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Jellyfish strategy scanning rows: For a given candidate value, find four rows where the candidate appears in
    /// only 2, 3, or 4 columns, and the union of those columns across all four rows is exactly 4. The candidate can
    /// then be eliminated from all other cells in those 4 columns.
    /// </summary>
    private bool JellyfishInRows(int round)
    {
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            int[] rowColMask = new int[ROW_COL_SEC_SIZE];
            int[] rowColCount = new int[ROW_COL_SEC_SIZE];
            for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
            {
                for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                {
                    int position = RowColumnToCell(row, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        rowColMask[row] |= (1 << col);
                        rowColCount[row]++;
                    }
                }
            }

            for (int r1 = 0; r1 < ROW_COL_SEC_SIZE - 3; r1++)
            {
                if (rowColCount[r1] < 2 || rowColCount[r1] > 4) continue;

                for (int r2 = r1 + 1; r2 < ROW_COL_SEC_SIZE - 2; r2++)
                {
                    if (rowColCount[r2] < 2 || rowColCount[r2] > 4) continue;
                    int union12 = rowColMask[r1] | rowColMask[r2];
                    if (BitCount(union12) > 4) continue;

                    for (int r3 = r2 + 1; r3 < ROW_COL_SEC_SIZE - 1; r3++)
                    {
                        if (rowColCount[r3] < 2 || rowColCount[r3] > 4) continue;
                        int union123 = union12 | rowColMask[r3];
                        if (BitCount(union123) > 4) continue;

                        for (int r4 = r3 + 1; r4 < ROW_COL_SEC_SIZE; r4++)
                        {
                            if (rowColCount[r4] < 2 || rowColCount[r4] > 4) continue;
                            int unionMask = union123 | rowColMask[r4];
                            if (BitCount(unionMask) != 4) continue;

                            bool doneSomething = false;
                            for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                            {
                                if (row == r1 || row == r2 || row == r3 || row == r4) continue;

                                for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                                {
                                    if ((unionMask & (1 << col)) == 0) continue;

                                    int position = RowColumnToCell(row, col);
                                    int valPos = GetPossibilityIndex(valIndex, position);
                                    if (possibilities[valPos] == 0)
                                    {
                                        possibilities[valPos] = round;
                                        doneSomething = true;
                                    }
                                }
                            }
                            if (doneSomething)
                            {
                                if (logHistory || recordHistory)
                                {
                                    var logItem = new LogItem(round, LogType.JELLYFISH_ROW, valIndex + 1, RowColumnToCell(r1, 0))
                                    {
                                        DetailedMessage = $"Jellyfish (rows): candidate {valIndex + 1} in rows {r1 + 1}, {r2 + 1}, {r3 + 1}, and {r4 + 1}, " +
                                            $"columns {BitmaskToString(unionMask)}. " +
                                            $"Eliminate {valIndex + 1} from other cells in columns {BitmaskToString(unionMask)}."
                                    };
                                    AddHistoryItem(logItem);
                                }
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Jellyfish strategy scanning columns
    /// in only 2, 3, or 4 rows, and the union of those rows across all four columns is exactly 4. The candidate can
    /// then be eliminated from all other cells in those 4 rows.
    /// </summary>
    private bool JellyfishInColumns(int round)
    {
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            int[] colRowMask = new int[ROW_COL_SEC_SIZE];
            int[] colRowCount = new int[ROW_COL_SEC_SIZE];
            for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
            {
                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                {
                    int position = RowColumnToCell(row, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        colRowMask[col] |= (1 << row);
                        colRowCount[col]++;
                    }
                }
            }

            for (int c1 = 0; c1 < ROW_COL_SEC_SIZE - 3; c1++)
            {
                if (colRowCount[c1] < 2 || colRowCount[c1] > 4) continue;

                for (int c2 = c1 + 1; c2 < ROW_COL_SEC_SIZE - 2; c2++)
                {
                    if (colRowCount[c2] < 2 || colRowCount[c2] > 4) continue;
                    int union12 = colRowMask[c1] | colRowMask[c2];
                    if (BitCount(union12) > 4) continue;

                    for (int c3 = c2 + 1; c3 < ROW_COL_SEC_SIZE - 1; c3++)
                    {
                        if (colRowCount[c3] < 2 || colRowCount[c3] > 4) continue;
                        int union123 = union12 | colRowMask[c3];
                        if (BitCount(union123) > 4) continue;

                        for (int c4 = c3 + 1; c4 < ROW_COL_SEC_SIZE; c4++)
                        {
                            if (colRowCount[c4] < 2 || colRowCount[c4] > 4) continue;
                            int unionMask = union123 | colRowMask[c4];
                            if (BitCount(unionMask) != 4) continue;

                            bool doneSomething = false;
                            for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                            {
                                if (col == c1 || col == c2 || col == c3 || col == c4) continue;

                                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                                {
                                    if ((unionMask & (1 << row)) == 0) continue;

                                    int position = RowColumnToCell(row, col);
                                    int valPos = GetPossibilityIndex(valIndex, position);
                                    if (possibilities[valPos] == 0)
                                    {
                                        possibilities[valPos] = round;
                                        doneSomething = true;
                                    }
                                }
                            }
                            if (doneSomething)
                            {
                                if (logHistory || recordHistory)
                                {
                                    var logItem = new LogItem(round, LogType.JELLYFISH_COLUMN, valIndex + 1, RowColumnToCell(0, c1))
                                    {
                                        DetailedMessage = $"Jellyfish (columns): candidate {valIndex + 1} in columns {c1 + 1}, {c2 + 1}, {c3 + 1}, and {c4 + 1}, " +
                                            $"rows {BitmaskToString(unionMask)}. " +
                                            $"Eliminate {valIndex + 1} from other cells in rows {BitmaskToString(unionMask)}."
                                    };
                                    AddHistoryItem(logItem);
                                }
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Y-Wing (XY-Wing) strategy
    /// share a unit (row, column, or section) with the pivot: - Wing1 has candidates {A, C} (shares candidate A with
    /// the pivot) - Wing2 has candidates {B, C} (shares candidate B with the pivot) The shared candidate C can be
    /// eliminated from any cell that sees both wings (i.e., shares a row, column, or section with both Wing1 and
    /// Wing2).
    /// </summary>
    private bool YWing(int round)
    {
        for (int pivot = 0; pivot < BOARD_SIZE; pivot++)
        {
            if (solution[pivot] != 0) continue;
            if (CountPossibilities(pivot) != 2) continue;

            // Get the two candidates for the pivot
            int pivotVal1 = -1;
            int pivotVal2 = -1;
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                int valPos = GetPossibilityIndex(valIndex, pivot);
                if (possibilities[valPos] == 0)
                {
                    if (pivotVal1 == -1)
                    {
                        pivotVal1 = valIndex;
                    }
                    else
                    {
                        pivotVal2 = valIndex;
                    }
                }
            }

            // Collect all bi-value cells that share a unit with the pivot
            // and share exactly one candidate with the pivot
            List<int> peers = GetPeers(pivot);

            for (int wi = 0; wi < peers.Count; wi++)
            {
                int wing1 = peers[wi];
                if (solution[wing1] != 0) continue;
                if (CountPossibilities(wing1) != 2) continue;

                // Wing1 must share exactly one candidate with the pivot
                bool wing1HasVal1 = possibilities[GetPossibilityIndex(pivotVal1, wing1)] == 0;
                bool wing1HasVal2 = possibilities[GetPossibilityIndex(pivotVal2, wing1)] == 0;

                // Must have exactly one of the pivot's candidates (not both, not neither)
                if (wing1HasVal1 == wing1HasVal2) continue;

                // Determine which pivot candidate wing1 shares, and find wing1's other candidate (C)
                int sharedVal1 = wing1HasVal1 ? pivotVal1 : pivotVal2;
                int otherPivotVal = wing1HasVal1 ? pivotVal2 : pivotVal1;
                int wingVal1C = -1;
                for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                {
                    int valPos = GetPossibilityIndex(valIndex, wing1);
                    if (possibilities[valPos] == 0 && valIndex != sharedVal1)
                    {
                        wingVal1C = valIndex;
                        break;
                    }
                }

                for (int wj = wi + 1; wj < peers.Count; wj++)
                {
                    int wing2 = peers[wj];
                    if (solution[wing2] != 0) continue;
                    if (CountPossibilities(wing2) != 2) continue;

                    // Wing2 must not share a unit with wing1's same unit as pivot
                    // (they must be in different units relative to the pivot)
                    // But most importantly: wing2 must have {otherPivotVal, C}
                    bool wing2HasOtherVal = possibilities[GetPossibilityIndex(otherPivotVal, wing2)] == 0;
                    bool wing2HasC = possibilities[GetPossibilityIndex(wingVal1C, wing2)] == 0;

                    if (!wing2HasOtherVal || !wing2HasC) continue;
                    if (CountPossibilities(wing2) != 2) continue;

                    // We have a valid Y-Wing: pivot={A,B}, wing1={A,C}, wing2={B,C}
                    // Eliminate C from all cells that can see both wing1 and wing2
                    bool doneSomething = false;
                    for (int target = 0; target < BOARD_SIZE; target++)
                    {
                        if (target == pivot || target == wing1 || target == wing2) continue;
                        if (solution[target] != 0) continue;

                        int valPos = GetPossibilityIndex(wingVal1C, target);
                        if (possibilities[valPos] != 0) continue;

                        // Target must see both wings
                        if (SharesUnit(target, wing1) && SharesUnit(target, wing2))
                        {
                            possibilities[valPos] = round;
                            doneSomething = true;
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory)
                        {
                            var logItem = new LogItem(round, LogType.Y_WING, wingVal1C + 1, pivot)
                            {
                                DetailedMessage = $"Y-Wing: pivot R{CellToRow(pivot) + 1}C{CellToColumn(pivot) + 1}{{{GetCandidatesString(pivot)}}}, " +
                                    $"wing1 R{CellToRow(wing1) + 1}C{CellToColumn(wing1) + 1}{{{GetCandidatesString(wing1)}}}, " +
                                    $"wing2 R{CellToRow(wing2) + 1}C{CellToColumn(wing2) + 1}{{{GetCandidatesString(wing2)}}}. " +
                                    $"Eliminate {wingVal1C + 1} from cells that see both wings."
                            };
                            AddHistoryItem(logItem);
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// XYZ-Wing strategy: Find a pivot cell with exactly three candidates {A, B, C}. Find two wing cells that each
    /// share a unit with the pivot: - Wing1 is a bi-value cell with candidates {A, C} (subset of pivot) - Wing2 is a
    /// bi-value cell with candidates {B, C} (subset of pivot) Wing1 and Wing2 must NOT share a unit with each other
    /// (they connect through different units of the pivot). The shared candidate C can be eliminated from any cell that
    /// sees all three: pivot, wing1, and wing2.
    /// </summary>
    private bool XyzWing(int round)
    {
        for (int pivot = 0; pivot < BOARD_SIZE; pivot++)
        {
            if (solution[pivot] != 0) continue;
            if (CountPossibilities(pivot) != 3) continue;

            // Get the three candidates for the pivot
            int[] pivotVals = new int[3];
            int pIdx = 0;
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                int valPos = GetPossibilityIndex(valIndex, pivot);
                if (possibilities[valPos] == 0)
                {
                    pivotVals[pIdx++] = valIndex;
                }
            }

            List<int> peers = GetPeers(pivot);

            // Try each pair of pivot values as the "non-shared" values for the two wings.
            // The remaining pivot value is C (the shared elimination candidate).
            for (int ci = 0; ci < 3; ci++)
            {
                int valC = pivotVals[ci];
                int valA = pivotVals[(ci + 1) % 3];
                int valB = pivotVals[(ci + 2) % 3];

                // Find wing1 with {A, C} among peers
                for (int wi = 0; wi < peers.Count; wi++)
                {
                    int wing1 = peers[wi];
                    if (solution[wing1] != 0) continue;
                    if (CountPossibilities(wing1) != 2) continue;

                    bool w1HasA = possibilities[GetPossibilityIndex(valA, wing1)] == 0;
                    bool w1HasC = possibilities[GetPossibilityIndex(valC, wing1)] == 0;
                    if (!w1HasA || !w1HasC) continue;

                    // Find wing2 with {B, C} among peers
                    for (int wj = wi + 1; wj < peers.Count; wj++)
                    {
                        int wing2 = peers[wj];
                        if (solution[wing2] != 0) continue;
                        if (CountPossibilities(wing2) != 2) continue;

                        bool w2HasB = possibilities[GetPossibilityIndex(valB, wing2)] == 0;
                        bool w2HasC = possibilities[GetPossibilityIndex(valC, wing2)] == 0;
                        if (!w2HasB || !w2HasC) continue;

                        // Eliminate C from any cell that sees all three: pivot, wing1, wing2
                        bool doneSomething = false;
                        for (int target = 0; target < BOARD_SIZE; target++)
                        {
                            if (target == pivot || target == wing1 || target == wing2) continue;
                            if (solution[target] != 0) continue;

                            int valPos = GetPossibilityIndex(valC, target);
                            if (possibilities[valPos] != 0) continue;

                            if (SharesUnit(target, pivot) && SharesUnit(target, wing1) && SharesUnit(target, wing2))
                            {
                                possibilities[valPos] = round;
                                doneSomething = true;
                            }
                        }
                        if (doneSomething)
                        {
                            if (logHistory || recordHistory)
                            {
                                var logItem = new LogItem(round, LogType.XYZ_WING, valC + 1, pivot)
                                {
                                    DetailedMessage = $"XYZ-Wing: pivot R{CellToRow(pivot) + 1}C{CellToColumn(pivot) + 1}{{{GetCandidatesString(pivot)}}}, " +
                                        $"wing1 R{CellToRow(wing1) + 1}C{CellToColumn(wing1) + 1}{{{GetCandidatesString(wing1)}}}, " +
                                        $"wing2 R{CellToRow(wing2) + 1}C{CellToColumn(wing2) + 1}{{{GetCandidatesString(wing2)}}}. " +
                                        $"Eliminate {valC + 1} from cells that see all three."
                                };
                                AddHistoryItem(logItem);
                            }
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if two cells share a row, column, or section.
    /// </summary>
    private static bool SharesUnit(int cell1, int cell2)
    {
        if (CellToRow(cell1) == CellToRow(cell2)) return true;
        if (CellToColumn(cell1) == CellToColumn(cell2)) return true;
        if (CellToSection(cell1) == CellToSection(cell2)) return true;
        return false;
    }

    /// <summary>
    /// Returns all cells that share a row, column, or section with the given cell (excluding the cell itself), without
    /// duplicates.
    /// </summary>
    private static List<int> GetPeers(int cell)
    {
        HashSet<int> peers = [];
        int row = CellToRow(cell);
        int col = CellToColumn(cell);
        int section = CellToSection(cell);

        // Row peers
        for (int c = 0; c < ROW_COL_SEC_SIZE; c++)
        {
            int pos = RowColumnToCell(row, c);
            if (pos != cell) peers.Add(pos);
        }

        // Column peers
        for (int r = 0; r < ROW_COL_SEC_SIZE; r++)
        {
            int pos = RowColumnToCell(r, col);
            if (pos != cell) peers.Add(pos);
        }

        // Section peers
        foreach (int pos in SectionLayout.SectionToSectionCells(section))
        {
            if (pos != cell) peers.Add(pos);
        }

        return [.. peers];
    }

    /// <summary>
    /// Simple Coloring (Singles Chains) strategy: For a given candidate value, build chains of conjugate pairs (cells
    /// in a unit where the candidate appears in exactly two places). Alternate two colors along the chain. Rule 2
    /// (Color Contradiction): If two cells of the same color share a unit, that color is invalid and the candidate is
    /// eliminated from all cells of that color. Rule 4 (Color Elimination): If an uncolored cell with the candidate can
    /// see cells of both colors, the candidate is eliminated from that cell.
    /// </summary>
    private bool SimpleColoring(int round)
    {
        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            // Find all unsolved cells that have this candidate
            List<int> candidateCells = [];
            for (int pos = 0; pos < BOARD_SIZE; pos++)
            {
                if (solution[pos] == 0 && possibilities[GetPossibilityIndex(valIndex, pos)] == 0)
                {
                    candidateCells.Add(pos);
                }
            }

            if (candidateCells.Count < 3) continue;

            // Build conjugate pair links: two cells in a unit where the candidate
            // appears in exactly those two cells
            Dictionary<int, List<int>> conjugateLinks = [];
            foreach (int cell in candidateCells)
            {
                conjugateLinks[cell] = [];
            }

            // Check rows for conjugate pairs
            for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
            {
                int c1 = -1;
                int c2 = -1;
                int count = 0;
                for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                {
                    int pos = RowColumnToCell(row, col);
                    if (solution[pos] == 0 && possibilities[GetPossibilityIndex(valIndex, pos)] == 0)
                    {
                        if (c1 == -1) c1 = pos;
                        else if (c2 == -1) c2 = pos;
                        count++;
                    }
                }
                if (count == 2)
                {
                    conjugateLinks[c1].Add(c2);
                    conjugateLinks[c2].Add(c1);
                }
            }

            // Check columns for conjugate pairs
            for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
            {
                int c1 = -1;
                int c2 = -1;
                int count = 0;
                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                {
                    int pos = RowColumnToCell(row, col);
                    if (solution[pos] == 0 && possibilities[GetPossibilityIndex(valIndex, pos)] == 0)
                    {
                        if (c1 == -1) c1 = pos;
                        else if (c2 == -1) c2 = pos;
                        count++;
                    }
                }
                if (count == 2)
                {
                    if (!conjugateLinks[c1].Contains(c2)) conjugateLinks[c1].Add(c2);
                    if (!conjugateLinks[c2].Contains(c1)) conjugateLinks[c2].Add(c1);
                }
            }

            // Check sections for conjugate pairs
            for (int sec = 0; sec < ROW_COL_SEC_SIZE; sec++)
            {
                int c1 = -1;
                int c2 = -1;
                int count = 0;
                foreach (int pos in SectionLayout.SectionToSectionCells(sec))
                {
                    if (solution[pos] == 0 && possibilities[GetPossibilityIndex(valIndex, pos)] == 0)
                    {
                        if (c1 == -1) c1 = pos;
                        else if (c2 == -1) c2 = pos;
                        count++;
                    }
                }
                if (count == 2)
                {
                    if (!conjugateLinks[c1].Contains(c2)) conjugateLinks[c1].Add(c2);
                    if (!conjugateLinks[c2].Contains(c1)) conjugateLinks[c2].Add(c1);
                }
            }

            // For each connected component, color with BFS using two colors (0 and 1)
            HashSet<int> visited = [];
            foreach (int startCell in candidateCells)
            {
                if (visited.Contains(startCell)) continue;
                if (conjugateLinks[startCell].Count == 0) continue;

                // BFS to color the chain
                // color[cell] = 0 or 1
                Dictionary<int, int> color = [];
                Queue<int> queue = new();
                color[startCell] = 0;
                queue.Enqueue(startCell);
                visited.Add(startCell);

                while (queue.Count > 0)
                {
                    int cell = queue.Dequeue();
                    int nextColor = 1 - color[cell];
                    foreach (int linked in conjugateLinks[cell])
                    {
                        if (!color.ContainsKey(linked))
                        {
                            color[linked] = nextColor;
                            visited.Add(linked);
                            queue.Enqueue(linked);
                        }
                    }
                }

                // Need at least 2 cells in the chain for any elimination
                if (color.Count < 2) continue;

                // Separate cells by color
                List<int> color0 = [];
                List<int> color1 = [];
                foreach (var kvp in color)
                {
                    if (kvp.Value == 0) color0.Add(kvp.Key);
                    else color1.Add(kvp.Key);
                }

                // Rule 2: Color contradiction - if two cells of the same color
                // share a unit, that color is invalid and the candidate is eliminated from all cells of
                // that color.
                bool color0Invalid = HasSameColorConflict(color0);
                bool color1Invalid = HasSameColorConflict(color1);

                if (color0Invalid && color1Invalid)
                {
                    // Both colors have contradictions - this shouldn't happen
                    // in a valid puzzle, skip this chain
                    continue;
                }

                if (color0Invalid || color1Invalid)
                {
                    // Eliminate the candidate from all cells of the invalid color
                    List<int> invalidCells = color0Invalid ? color0 : color1;
                    bool doneSomething = false;
                    foreach (int cell in invalidCells)
                    {
                        int valPos = GetPossibilityIndex(valIndex, cell);
                        if (possibilities[valPos] == 0)
                        {
                            possibilities[valPos] = round;
                            doneSomething = true;
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.SIMPLE_COLORING, valIndex + 1, invalidCells[0]));
                        return true;
                    }
                }

                // Rule 4: Color elimination - if an uncolored cell sees both colors,
                // eliminate the candidate from that cell
                bool doneSomethingR4 = false;
                foreach (int cell in candidateCells)
                {
                    if (color.ContainsKey(cell)) continue;

                    bool seesColor0 = false;
                    bool seesColor1 = false;
                    foreach (int c0 in color0)
                    {
                        if (SharesUnit(cell, c0))
                        {
                            seesColor0 = true;
                            break;
                        }
                    }
                    if (!seesColor0) continue;
                    foreach (int c1 in color1)
                    {
                        if (SharesUnit(cell, c1))
                        {
                            seesColor1 = true;
                            break;
                        }
                    }
                    if (seesColor0 && seesColor1)
                    {
                        int valPos = GetPossibilityIndex(valIndex, cell);
                        if (possibilities[valPos] == 0)
                        {
                            possibilities[valPos] = round;
                            doneSomethingR4 = true;
                        }
                    }
                }
                if (doneSomethingR4)
                {
                    if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.SIMPLE_COLORING, valIndex + 1, startCell));
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if any two cells in the list share a row, column, or section, indicating a color contradiction in
    /// Simple Coloring.
    /// </summary>
    private static bool HasSameColorConflict(List<int> cells)
    {
        for (int i = 0; i < cells.Count - 1; i++)
        {
            for (int j = i + 1; j < cells.Count; j++)
            {
                if (SharesUnit(cells[i], cells[j]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool HandleNakedPairs(int round)
    {
        for (int position = 0; position < BOARD_SIZE; position++)
        {
            int possibilities = CountPossibilities(position);
            if (possibilities == 2)
            {
                int row = CellToRow(position);
                int column = CellToColumn(position);
                int section = CellToSectionStartCell(position);
                for (int position2 = position; position2 < BOARD_SIZE; position2++)
                {
                    if (position != position2)
                    {
                        int possibilities2 = CountPossibilities(position2);
                        if (possibilities2 == 2 && ArePossibilitiesSame(position, position2))
                        {
                            if (row == CellToRow(position2))
                            {
                                bool doneSomething = false;
                                for (int column2 = 0; column2 < ROW_COL_SEC_SIZE; column2++)
                                {
                                    int position3 = RowColumnToCell(row, column2);
                                    if (position3 != position && position3 != position2 && RemovePossibilitiesInOneFromTwo(position, position3, round))
                                    {
                                        doneSomething = true;
                                    }
                                }
                                if (doneSomething)
                                {
                                    if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.NAKED_PAIR_ROW, 0, position));
                                    return true;
                                }
                            }
                            if (column == CellToColumn(position2))
                            {
                                bool doneSomething = false;
                                for (int row2 = 0; row2 < ROW_COL_SEC_SIZE; row2++)
                                {
                                    int position3 = RowColumnToCell(row2, column);
                                    if (position3 != position && position3 != position2 && RemovePossibilitiesInOneFromTwo(position, position3, round))
                                    {
                                        doneSomething = true;
                                    }
                                }
                                if (doneSomething)
                                {
                                    if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.NAKED_PAIR_COLUMN, 0, position));
                                    return true;
                                }
                            }
                            if (section == CellToSectionStartCell(position2))
                            {
                                bool doneSomething = false;
                                foreach (int position3 in CellToSectionCells(position))
                                {
                                    if (position3 != position && position3 != position2 && RemovePossibilitiesInOneFromTwo(position, position3, round))
                                    {
                                        doneSomething = true;
                                    }
                                }
                                if (doneSomething)
                                {
                                    if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.NAKED_PAIR_SECTION, 0, position));
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Naked Triples: Find three cells in the same unit (row, column, or section) whose combined candidates contain
    /// exactly three values, with each cell having 2 or 3 of those values. Those three candidates can be eliminated
    /// from all other cells in the unit.
    /// </summary>
    private bool HandleNakedTriples(int round)
    {
        // Check rows
        for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
        {
            // Collect unsolved cell positions in this row
            List<int> cells = [];
            for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
            {
                int position = RowColumnToCell(row, col);
                if (solution[position] == 0)
                {
                    int count = CountPossibilities(position);
                    if (count == 2 || count == 3)
                    {
                        cells.Add(position);
                    }
                }
            }
            if (NakedTripleInUnit(cells, round, LogType.NAKED_TRIPLE_ROW, row, true))
                return true;
        }

        // Check columns
        for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
        {
            List<int> cells = [];
            for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
            {
                int position = RowColumnToCell(row, col);
                if (solution[position] == 0)
                {
                    int count = CountPossibilities(position);
                    if (count == 2 || count == 3)
                    {
                        cells.Add(position);
                    }
                }
            }
            if (NakedTripleInUnit(cells, round, LogType.NAKED_TRIPLE_COLUMN, col, false))
                return true;
        }

        // Check sections
        for (int sec = 0; sec < ROW_COL_SEC_SIZE; sec++)
        {
            List<int> cells = [];
            foreach (int position in SectionLayout.SectionToSectionCells(sec))
            {
                if (solution[position] == 0)
                {
                    int count = CountPossibilities(position);
                    if (count == 2 || count == 3)
                    {
                        cells.Add(position);
                    }
                }
            }
            if (NakedTripleSectionUnit(cells, round, sec))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Search for a naked triple among the candidate cells in a row or column unit.
    /// </summary>
    /// <param name="cells">Cells in the unit with 2 or 3 possibilities.</param>
    /// <param name="round">Round for marking eliminations.</param>
    /// <param name="logType">The log type for history.</param>
    /// <param name="unitIndex">Row or column index.</param>
    /// <param name="isRow">True for row, false for column.</param>
    private bool NakedTripleInUnit(List<int> cells, int round, LogType logType, int unitIndex, bool isRow)
    {
        if (cells.Count < 3) return false;

        for (int i = 0; i < cells.Count - 2; i++)
        {
            for (int j = i + 1; j < cells.Count - 1; j++)
            {
                for (int k = j + 1; k < cells.Count; k++)
                {
                    int pos1 = cells[i];
                    int pos2 = cells[j];
                    int pos3 = cells[k];

                    // Compute the union of candidates for these three cells
                    int unionCount = 0;
                    int unionMask = 0;
                    for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                    {
                        bool in1 = possibilities[GetPossibilityIndex(valIndex, pos1)] == 0;
                        bool in2 = possibilities[GetPossibilityIndex(valIndex, pos2)] == 0;
                        bool in3 = possibilities[GetPossibilityIndex(valIndex, pos3)] == 0;
                        if (in1 || in2 || in3)
                        {
                            unionCount++;
                            unionMask |= (1 << valIndex);
                        }
                    }

                    // A naked triple requires exactly 3 distinct candidates across all three cells
                    if (unionCount != 3) continue;

                    // Eliminate those three candidates from all other cells in the unit
                    bool doneSomething = false;
                    for (int idx = 0; idx < ROW_COL_SEC_SIZE; idx++)
                    {
                        int position = isRow ? RowColumnToCell(unitIndex, idx) : RowColumnToCell(idx, unitIndex);
                        if (position == pos1 || position == pos2 || position == pos3) continue;
                        if (solution[position] != 0) continue;

                        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                        {
                            if ((unionMask & (1 << valIndex)) != 0)
                            {
                                int valPos = GetPossibilityIndex(valIndex, position);
                                if (possibilities[valPos] == 0)
                                {
                                    possibilities[valPos] = round;
                                    doneSomething = true;
                                }
                            }
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, logType, 0, pos1));
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Search for a naked triple among the candidate cells in a section unit.
    /// </summary>
    private bool NakedTripleSectionUnit(List<int> cells, int round, int section)
    {
        if (cells.Count < 3) return false;

        for (int i = 0; i < cells.Count - 2; i++)
        {
            for (int j = i + 1; j < cells.Count - 1; j++)
            {
                for (int k = j + 1; k < cells.Count; k++)
                {
                    int pos1 = cells[i];
                    int pos2 = cells[j];
                    int pos3 = cells[k];

                    // Compute the union of candidates for these three cells
                    int unionCount = 0;
                    int unionMask = 0;
                    for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                    {
                        bool in1 = possibilities[GetPossibilityIndex(valIndex, pos1)] == 0;
                        bool in2 = possibilities[GetPossibilityIndex(valIndex, pos2)] == 0;
                        bool in3 = possibilities[GetPossibilityIndex(valIndex, pos3)] == 0;
                        if (in1 || in2 || in3)
                        {
                            unionCount++;
                            unionMask |= (1 << valIndex);
                        }
                    }

                    if (unionCount != 3) continue;

                    // Eliminate those three candidates from all other cells in the section
                    bool doneSomething = false;
                    foreach (int position in SectionLayout.SectionToSectionCells(section))
                    {
                        if (position == pos1 || position == pos2 || position == pos3) continue;
                        if (solution[position] != 0) continue;

                        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                        {
                            if ((unionMask & (1 << valIndex)) != 0)
                            {
                                int valPos = GetPossibilityIndex(valIndex, position);
                                if (possibilities[valPos] == 0)
                                {
                                    possibilities[valPos] = round;
                                    doneSomething = true;
                                }
                            }
                        }
                    }
                    if (doneSomething)
                    {
                        if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.NAKED_TRIPLE_SECTION, 0, pos1));
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Naked Quads: Find four cells in the same unit (row, column, or section) whose combined candidates contain
    /// exactly four values, with each cell having 2, 3, or 4 of those values. Those four candidates can be eliminated
    /// from all other cells in the unit.
    /// </summary>
    private bool HandleNakedQuads(int round)
    {
        // Check rows
        for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
        {
            List<int> cells = [];
            for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
            {
                int position = RowColumnToCell(row, col);
                if (solution[position] == 0)
                {
                    int count = CountPossibilities(position);
                    if (count >= 2 && count <= 4)
                    {
                        cells.Add(position);
                    }
                }
            }
            if (NakedQuadInUnit(cells, round, LogType.NAKED_QUAD_ROW, row, true))
                return true;
        }

        // Check columns
        for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
        {
            List<int> cells = [];
            for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
            {
                int position = RowColumnToCell(row, col);
                if (solution[position] == 0)
                {
                    int count = CountPossibilities(position);
                    if (count >= 2 && count <= 4)
                    {
                        cells.Add(position);
                    }
                }
            }
            if (NakedQuadInUnit(cells, round, LogType.NAKED_QUAD_COLUMN, col, false))
                return true;
        }

        // Check sections
        for (int sec = 0; sec < ROW_COL_SEC_SIZE; sec++)
        {
            List<int> cells = [];
            foreach (int position in SectionLayout.SectionToSectionCells(sec))
            {
                if (solution[position] == 0)
                {
                    int count = CountPossibilities(position);
                    if (count >= 2 && count <= 4)
                    {
                        cells.Add(position);
                    }
                }
            }
            if (NakedQuadSectionUnit(cells, round, sec))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Search for a naked quad among the candidate cells in a row or column unit.
    /// </summary>
    /// <param name="cells">Cells in the unit with 2, 3, or 4 possibilities.</param>
    /// <param name="round">Round for marking eliminations.</param>
    /// <param name="logType">The log type for history.</param>
    /// <param name="unitIndex">Row or column index.</param>
    /// <param name="isRow">True for row, false for column.</param>
    private bool NakedQuadInUnit(List<int> cells, int round, LogType logType, int unitIndex, bool isRow)
    {
        if (cells.Count < 4) return false;

        for (int i = 0; i < cells.Count - 3; i++)
        {
            for (int j = i + 1; j < cells.Count - 2; j++)
            {
                for (int k = j + 1; k < cells.Count - 1; k++)
                {
                    for (int l = k + 1; l < cells.Count; l++)
                    {
                        int pos1 = cells[i];
                        int pos2 = cells[j];
                        int pos3 = cells[k];
                        int pos4 = cells[l];

                        // Compute the union of candidates for these four cells
                        int unionCount = 0;
                        int unionMask = 0;
                        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                        {
                            bool in1 = possibilities[GetPossibilityIndex(valIndex, pos1)] == 0;
                            bool in2 = possibilities[GetPossibilityIndex(valIndex, pos2)] == 0;
                            bool in3 = possibilities[GetPossibilityIndex(valIndex, pos3)] == 0;
                            bool in4 = possibilities[GetPossibilityIndex(valIndex, pos4)] == 0;
                            if (in1 || in2 || in3 || in4)
                            {
                                unionCount++;
                                unionMask |= (1 << valIndex);
                            }
                        }

                        // A naked quad requires exactly 4 distinct candidates across all four cells
                        if (unionCount != 4) continue;

                        // Eliminate those four candidates from all other cells in the unit
                        bool doneSomething = false;
                        for (int idx = 0; idx < ROW_COL_SEC_SIZE; idx++)
                        {
                            int position = isRow ? RowColumnToCell(unitIndex, idx) : RowColumnToCell(idx, unitIndex);
                            if (position == pos1 || position == pos2 || position == pos3 || position == pos4) continue;
                            if (solution[position] != 0) continue;

                            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                            {
                                if ((unionMask & (1 << valIndex)) != 0)
                                {
                                    int valPos = GetPossibilityIndex(valIndex, position);
                                    if (possibilities[valPos] == 0)
                                    {
                                        possibilities[valPos] = round;
                                        doneSomething = true;
                                    }
                                }
                            }
                        }
                        if (doneSomething)
                        {
                            if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, logType, 0, pos1));
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Search for a naked quad among the candidate cells in a section unit.
    /// </summary>
    private bool NakedQuadSectionUnit(List<int> cells, int round, int section)
    {
        if (cells.Count < 4) return false;

        for (int i = 0; i < cells.Count - 3; i++)
        {
            for (int j = i + 1; j < cells.Count - 2; j++)
            {
                for (int k = j + 1; k < cells.Count - 1; k++)
                {
                    for (int l = k + 1; l < cells.Count; l++)
                    {
                        int pos1 = cells[i];
                        int pos2 = cells[j];
                        int pos3 = cells[k];
                        int pos4 = cells[l];

                        // Compute the union of candidates for these four cells
                        int unionCount = 0;
                        int unionMask = 0;
                        for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                        {
                            bool in1 = possibilities[GetPossibilityIndex(valIndex, pos1)] == 0;
                            bool in2 = possibilities[GetPossibilityIndex(valIndex, pos2)] == 0;
                            bool in3 = possibilities[GetPossibilityIndex(valIndex, pos3)] == 0;
                            bool in4 = possibilities[GetPossibilityIndex(valIndex, pos4)] == 0;
                            if (in1 || in2 || in3 || in4)
                            {
                                unionCount++;
                                unionMask |= (1 << valIndex);
                            }
                        }

                        if (unionCount != 4) continue;

                        // Eliminate those four candidates from all other cells in the section
                        bool doneSomething = false;
                        foreach (int position in SectionLayout.SectionToSectionCells(section))
                        {
                            if (position == pos1 || position == pos2 || position == pos3 || position == pos4) continue;
                            if (solution[position] != 0) continue;

                            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                            {
                                if ((unionMask & (1 << valIndex)) != 0)
                                {
                                    int valPos = GetPossibilityIndex(valIndex, position);
                                    if (possibilities[valPos] == 0)
                                    {
                                        possibilities[valPos] = round;
                                        doneSomething = true;
                                    }
                                }
                            }
                        }
                        if (doneSomething)
                        {
                            if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.NAKED_QUAD_SECTION, 0, pos1));
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Mark exactly one cell which is the only possible value for some row, if such a cell exists. This method will
    /// look in a row for a possibility that is only listed for one cell. This type of cell is often called a "hidden
    /// single"
    /// </summary>
    private bool OnlyValueInRow(int round)
    {
        for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
        {
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                int count = 0;
                int lastPosition = 0;
                for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                {
                    int position = (row * ROW_COL_SEC_SIZE) + col;
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        count++;
                        lastPosition = position;
                    }
                }
                if (count == 1)
                {
                    int value = valIndex + 1;
                    if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.HIDDEN_SINGLE_ROW, value, lastPosition));
                    Mark(lastPosition, round, value);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Mark exactly one cell which is the only possible value for some column, if such a cell exists. This method will
    /// look in a column for a possibility that is only listed for one cell. This type of cell is often called a "hidden
    /// single"
    /// </summary>
    private bool OnlyValueInColumn(int round)
    {
        for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
        {
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                int count = 0;
                int lastPosition = 0;
                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                {
                    int position = RowColumnToCell(row, col);
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        count++;
                        lastPosition = position;
                    }
                }
                if (count == 1)
                {
                    int value = valIndex + 1;
                    if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.HIDDEN_SINGLE_COLUMN, value, lastPosition));
                    Mark(lastPosition, round, value);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Mark exactly one cell which is the only possible value for some section, if such a cell exists. This method will
    /// look in a section for a possibility that is only listed for one cell. This type of cell is often called a
    /// "hidden single"
    /// </summary>
    private bool OnlyValueInSection(int round)
    {
        // check each section in the puzzle
        for (int sec = 0; sec < ROW_COL_SEC_SIZE; sec++)
        {
            // valIndex: index to possibilities in a cell (0 - 8) for the values (1 - 9) 
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                int count = 0;
                int lastPosition = 0;
                foreach (int position in SectionLayout.SectionToSectionCells(sec))
                {
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        count++;
                        lastPosition = position;
                    }
                }
                if (count == 1)
                {
                    int value = valIndex + 1;
                    if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.HIDDEN_SINGLE_SECTION, value, lastPosition));
                    Mark(lastPosition, round, value);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Mark exactly one cell that has a single possibility, if such a cell exists. This method will look for a cell
    /// that has only one possibility. This type of cell is often called a "single"
    /// </summary>
    private bool OnlyPossibilityForCell(int round)
    {
        for (int position = 0; position < BOARD_SIZE; position++)
        {
            if (solution[position] == 0)
            {
                int count = 0;
                int lastValue = 0;
                for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                {
                    int valPos = GetPossibilityIndex(valIndex, position);
                    if (possibilities[valPos] == 0)
                    {
                        count++;
                        lastValue = valIndex + 1;
                    }
                }
                if (count == 1)
                {
                    Mark(position, round, lastValue);
                    if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.SINGLE, lastValue, position));
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Mark the given value at the given position. Go through the row, column, and section for the position and remove
    /// the value from the possibilities.
    /// </summary>
    /// <param name="position">Position into the board (0-80)</param>
    /// <param name="round">Round to mark for rollback purposes</param>
    /// <param name="value">The value to go in the square at the given position</param>
    private void Mark(int position, int round, int value)
    {
        if (solution[position] != 0) throw new ArgumentException("Marking position that already has been marked.");
        if (solutionRound[position] != 0) throw new ArgumentException("Marking position that was marked another round.");
        int valIndex = value - 1;
        solution[position] = value;

        int possInd = GetPossibilityIndex(valIndex, position);
        if (possibilities[possInd] != 0) throw new ArgumentException("Marking impossible position.");

        // Take this value out of the possibilities for everything in the row
        solutionRound[position] = round;
        int rowStart = CellToRow(position) * ROW_COL_SEC_SIZE;
        for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
        {
            int rowVal = rowStart + col;
            int valPos = GetPossibilityIndex(valIndex, rowVal);
            // Debug.WriteLine("Row Start: "+rowStart+" Row Value: "+rowVal+" Value Position: "+valPos);
            if (possibilities[valPos] == 0)
            {
                possibilities[valPos] = round;
            }
        }

        // Take this value out of the possibilities for everything in the column
        int colStart = CellToColumn(position);
        for (int i = 0; i < ROW_COL_SEC_SIZE; i++)
        {
            int colVal = colStart + (ROW_COL_SEC_SIZE * i);
            int valPos = GetPossibilityIndex(valIndex, colVal);
            // Debug.WriteLine("Col Start: "+colStart+" Col Value: "+colVal+" Value Position: "+valPos);
            if (possibilities[valPos] == 0)
            {
                possibilities[valPos] = round;
            }
        }

        // Take this value out of the possibilities for everything in section
        int section = CellToSection(position);
        foreach (int secVal in SectionLayout.SectionToSectionCells(section))
        {
            int valPos = GetPossibilityIndex(valIndex, secVal);
            // Debug.WriteLine("Sec Start: "+secStart+" Sec Value: "+secVal+" Value Position: "+valPos);
            if (possibilities[valPos] == 0)
            {
                possibilities[valPos] = round;
            }
        }

        // This position itself is determined, it should have possibilities.
        for (valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
        {
            int valPos = GetPossibilityIndex(valIndex, position);
            if (possibilities[valPos] == 0)
            {
                possibilities[valPos] = round;
            }
        }
    }

    /// <summary>
    /// print the given BOARD_SIZEd array of ints as a sudoku puzzle. Use print options from member variables.
    /// </summary>
    private void Print(int[] sudoku)
    {
        Debug.Write(PuzzleToString(sudoku));
    }

    private string PuzzleToString(int[] sudoku)
    {
        StringBuilder sb = new();
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            if (printStyle == PrintStyle.READABLE)
            {
                sb.Append(' ');
            }
            if (sudoku[i] == 0)
            {
                sb.Append('.');
            }
            else
            {
                sb.Append(sudoku[i]);
            }
            if (i == BOARD_SIZE - 1)
            {
                if (printStyle == PrintStyle.CSV)
                {
                    sb.Append(',');
                }
                else
                {
                    sb.Append(NL);
                }
                if (printStyle == PrintStyle.READABLE || printStyle == PrintStyle.COMPACT)
                {
                    sb.Append(NL);
                }
            }
            else if (i % ROW_COL_SEC_SIZE == ROW_COL_SEC_SIZE - 1)
            {
                if (printStyle == PrintStyle.READABLE || printStyle == PrintStyle.COMPACT)
                {
                    sb.Append(NL);
                }
                if (i % SEC_GROUP_SIZE == SEC_GROUP_SIZE - 1)
                {
                    if (printStyle == PrintStyle.READABLE)
                    {
                        sb.Append("-------|-------|-------").Append(NL);
                    }
                }
            }
            else if (i % GRID_SIZE == GRID_SIZE - 1)
            {
                if (printStyle == PrintStyle.READABLE)
                {
                    sb.Append(" |");
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Creates a string for an int array initializer: { 0, 1, 0, 0, 5 ...}
    /// </summary>
    private static string ToIntArrayString(int[] array)
    {
        StringBuilder sb = new();
        sb.Append("{ ");
        for (int idx = 0; idx < array.Length; idx++)
        {
            sb.Append(array[idx]).Append(", ");
        }
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Print the sudoku puzzle.
    /// </summary>
    public void PrintPuzzle()
    {
        Print(puzzle);
    }

    public string GetPuzzleString()
    {
        return PuzzleToString(puzzle);
    }

    public string GetPuzzleArray()
    {
        return ToIntArrayString(puzzle);
    }

    public int[] GetPuzzle()
    {
        int[] clone = new int[puzzle.Length];
        Array.Copy(puzzle, clone, puzzle.Length);
        return clone;
    }

    /// <summary>
    /// Print the sudoku solution.
    /// </summary>
    public void PrintSolution()
    {
        Print(solution);
    }

    public string GetSolutionString()
    {
        return PuzzleToString(solution);
    }

    public string GetSolutionArray()
    {
        return ToIntArrayString(solution);
    }

    public int[] GetSolution()
    {
        int[] clone = new int[solution.Length];
        Array.Copy(solution, clone, solution.Length);
        return clone;
    }

    /// <summary>
    /// Given a vector of LogItems, determine how many log items in the vector are of the specified type.
    /// </summary>
    private static int GetLogCount(List<LogItem> v, LogType type)
    {
        int count = 0;
        for (int i = 0; i < v.Count; i++)
        {
            if (v[i].GetLogType() == type) count++;
        }
        return count;
    }

    /// <summary>
    /// Shuffle the values in an array of integers.
    /// </summary>
    private static void ShuffleArray(int[] array, int size)
    {
        for (int i = 0; i < size; i++)
        {
            int tailSize = size - i;
            int randTailPos = random.Next() % tailSize + i;
            (array[randTailPos], array[i]) = (array[i], array[randTailPos]);
        }
    }

    private static Symmetry GetRandomSymmetry()
    {
        Symmetry[] values = SymmetryExtensions.Values();
        // not the first and last value which are NONE and RANDOM
        return values[(random.Next() % (values.Length - 1)) + 1];
    }

    /// <summary>
    /// Given the index of a cell (0-80) calculate the column (0-8) in which that cell resides.
    /// </summary>
    public static int CellToColumn(int cell)
    {
        return cell % ROW_COL_SEC_SIZE;
    }

    /// <summary>
    /// Given the index of a cell (0-80) calculate the row (0-8) in which it resides.
    /// </summary>
    public static int CellToRow(int cell)
    {
        return cell / ROW_COL_SEC_SIZE;
    }

    /// <summary>
    /// Given the index of a cell (0-80) calculate the section (0-8) in which it resides.
    /// </summary>
    public static int CellToSection(int cell)
    {
        return SectionLayout.CellToSection(cell);
    }

    /// <summary>
    /// Given the index of a cell (0-80) calculate the cell (0-80) that is the upper left start cell of that section.
    /// </summary>
    public static int CellToSectionStartCell(int cell)
    {
        return SectionLayout.CellToSectionStartCell(cell);
    }

    /// <summary>
    /// Given a cell (0-80), iterate over all cells in the section
    /// </summary>
    private static IEnumerable<int> CellToSectionCells(int cell)
    {
        int section = SectionLayout.CellToSection(cell);
        return SectionLayout.SectionToSectionCells(section);
    }

    /// <summary>
    /// Given a row (0-8) calculate the first cell (0-80) of that row.
    /// </summary>
    public static int RowToFirstCell(int row)
    {
        return 9 * row;
    }

    /// <summary>
    /// Given a column (0-8) calculate the first cell (0-80) of that column.
    /// </summary>
    public static int ColumnToFirstCell(int column)
    {
        return column;
    }

    /// <summary>
    /// Given a section (0-8) calculate the first cell (0-80) of that section.
    /// </summary>
    public static int SectionToFirstCell(int section)
    {
        return SectionLayout.SectionToFirstCell(section);
    }

    /// <summary>
    /// Given a value for a cell (0-8) and a cell number (0-80) calculate the offset into the possibility array (0-728).
    /// </summary>
    public static int GetPossibilityIndex(int valueIndex, int cell)
    {
        return valueIndex + (ROW_COL_SEC_SIZE * cell);
    }

    /// <summary>
    /// Given a row (0-8) and a column (0-8) calculate the cell (0-80).
    /// </summary>
    public static int RowColumnToCell(int row, int column)
    {
        return (row * ROW_COL_SEC_SIZE) + column;
    }

    /// <summary>
    /// Given a section (0-8) and an offset into that section (0-8) calculate the cell (0-80)
    /// </summary>
    public static int SectionToCell(int section, int offset)
    {
        return SectionLayout.SectionToCell(section, offset);
    }
}
