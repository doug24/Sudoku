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
using System.Text;

namespace QQWingLib
{
    /// <summary>
    /// The board containing all the memory structures and methods for solving or
    /// generating sudoku puzzles.
    /// </summary>
    public class QQWing
    {
        public static readonly string QQWING_VERSION = "n1.3.4";

        private static readonly string NL = Environment.NewLine;

        private static readonly int GRID_SIZE = 3;

        private static readonly int ROW_COL_SEC_SIZE = GRID_SIZE * GRID_SIZE;

        private static readonly int SEC_GROUP_SIZE = ROW_COL_SEC_SIZE * GRID_SIZE;

        private static readonly int BOARD_SIZE = ROW_COL_SEC_SIZE * ROW_COL_SEC_SIZE;

        private static readonly int POSSIBILITY_SIZE = BOARD_SIZE * ROW_COL_SEC_SIZE;

        private static readonly Random random = new Random();

        /// <summary>
        /// The last round of solving
        /// </summary>
        private int lastSolveRound;

        /// <summary>
        /// The 81 integers that make up a sudoku puzzle. Givens are 1-9, unknowns
        /// are 0. Once initialized, this puzzle remains as is. The answer is worked
        /// out in "solution".
        /// </summary>
        private readonly int[] puzzle = new int[BOARD_SIZE];

        /// <summary>
        /// The 81 integers that make up a sudoku puzzle. The solution is built here,
        /// after completion all will be 1-9.
        /// </summary>
        private readonly int[] solution = new int[BOARD_SIZE];

        /// <summary>
        /// Recursion depth at which each of the numbers in the solution were placed.
        /// Useful for backing out solve branches that don't lead to a solution.
        /// </summary>
        private readonly int[] solutionRound = new int[BOARD_SIZE];

        /// <summary>
        /// The 729 integers that make up a the possible values for a Sudoku puzzle.
        /// (9 possibilities for each of 81 squares). If possibilities[i] is zero,
        /// then the possibility could still be filled in according to the Sudoku
        /// rules. When a possibility is eliminated, possibilities[i] is assigned the
        /// round (recursion level) at which it was determined that it could not be a
        /// possibility.
        /// </summary>
        private readonly int[] possibilities = new int[POSSIBILITY_SIZE];

        /// <summary>
        /// An array the size of the board (81) containing each of the numbers 0-n
        /// exactly once. This array may be shuffled so that operations that need to
        /// look at each cell can do so in a random order.
        /// </summary>
        private readonly int[] randomBoardArray = FillIncrementing(new int[BOARD_SIZE]);

        /// <summary>
        /// An array with one element for each position (9), in some random order to
        /// be used when trying each position in turn during guesses.
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
        /// A list of moves used to solve the puzzle. This list contains all moves,
        /// even on solve branches that did not lead to a solution.
        /// </summary>
        private readonly List<LogItem> solveHistory = new List<LogItem>();

        /// <summary>
        /// A list of moves used to solve the puzzle. This list contains only the
        /// moves needed to solve the puzzle, but doesn't contain information about
        /// bad guesses.
        /// </summary>
        private readonly List<LogItem> solveInstructions = new List<LogItem>();

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
        /// Get the number of cells that are set in the puzzle (as opposed to figured
        /// out in the solution
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
        /// Set the board to the given puzzle. The given puzzle must be an array of
        /// 81 integers.
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
        /// Reset the board to its initial state with only the givens. This method
        /// clears any solution, resets statistics, and clears any history messages.
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
            if (GetBoxLineReductionCount() > 0) return Difficulty.INTERMEDIATE;
            if (GetPointingPairTripleCount() > 0) return Difficulty.INTERMEDIATE;
            if (GetHiddenPairCount() > 0) return Difficulty.INTERMEDIATE;
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
        /// Get the number of cells for which the solution was determined because
        /// there was only one possible value for that cell.
        /// </summary>
        public int GetSingleCount()
        {
            return GetLogCount(solveInstructions, LogType.SINGLE);
        }

        /// <summary>
        /// Get the number of cells for which the solution was determined because
        /// that cell had the only possibility for some value in the row, column, or
        /// section.
        /// </summary>
        public int GetHiddenSingleCount()
        {
            return (GetLogCount(solveInstructions, LogType.HIDDEN_SINGLE_ROW) +
                GetLogCount(solveInstructions, LogType.HIDDEN_SINGLE_COLUMN) + GetLogCount(solveInstructions, LogType.HIDDEN_SINGLE_SECTION));
        }

        /// <summary>
        /// Get the number of naked pair reductions that were performed in solving
        /// this puzzle.
        /// </summary>
        public int GetNakedPairCount()
        {
            return (GetLogCount(solveInstructions, LogType.NAKED_PAIR_ROW) +
                GetLogCount(solveInstructions, LogType.NAKED_PAIR_COLUMN) + GetLogCount(solveInstructions, LogType.NAKED_PAIR_SECTION));
        }

        /// <summary>
        /// Get the number of hidden pair reductions that were performed in solving
        /// this puzzle.
        /// </summary>
        public int GetHiddenPairCount()
        {
            return (GetLogCount(solveInstructions, LogType.HIDDEN_PAIR_ROW) +
                GetLogCount(solveInstructions, LogType.HIDDEN_PAIR_COLUMN) + GetLogCount(solveInstructions, LogType.HIDDEN_PAIR_SECTION));
        }

        /// <summary>
        /// Get the number of pointing pair/triple reductions that were performed in
        /// solving this puzzle.
        /// </summary>
        public int GetPointingPairTripleCount()
        {
            return (GetLogCount(solveInstructions, LogType.POINTING_PAIR_TRIPLE_ROW) + GetLogCount(solveInstructions, LogType.POINTING_PAIR_TRIPLE_COLUMN));
        }

        /// <summary>
        /// Get the number of box/line reductions that were performed in solving this
        /// puzzle.
        /// </summary>
        public int GetBoxLineReductionCount()
        {
            return (GetLogCount(solveInstructions, LogType.ROW_BOX) + GetLogCount(solveInstructions, LogType.COLUMN_BOX));
        }

        /// <summary>
        /// Get the number lucky guesses in solving this puzzle.
        /// </summary>
        public int GetGuessCount()
        {
            return GetLogCount(solveInstructions, LogType.GUESS);
        }

        /// <summary>
        /// Get the number of backtracks (unlucky guesses) required when solving this
        /// puzzle.
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

        public bool GeneratePuzzle()
        {
            return GeneratePuzzleSymmetry(Symmetry.NONE);
        }

        public bool GeneratePuzzleSymmetry(Symmetry symmetry)
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
            Solve();

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
            StringBuilder sb = new StringBuilder();
            if (!recordHistory)
            {
                sb.Append("History was not recorded.").Append(NL);
                if (printStyle == PrintStyle.CSV)
                {
                    sb.Append(" -- ").Append(NL);
                }
                else
                {
                    sb.Append(NL);
                }
            }
            for (int i = 0; i < v.Count; i++)
            {
                sb.Append(i + 1 + ". ").Append(NL);
                v[i].Print();
                if (printStyle == PrintStyle.CSV)
                {
                    sb.Append(" -- ").Append(NL);
                }
                else
                {
                    sb.Append(NL);
                }
            }
            if (printStyle == PrintStyle.CSV)
            {
                sb.Append(",").Append(NL);
            }
            else
            {
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

        public bool Solve()
        {
            Reset();
            ShuffleRandomArrays();
            return Solve(2);
        }

        private bool Solve(int round)
        {
            lastSolveRound = round;

            while (SingleSolveMove(round))
            {
                if (IsSolved()) return true;
                if (IsImpossible()) return false;
            }

            int nextGuessRound = round + 1;
            int nextRound = round + 2;
            for (int guessNumber = 0; Guess(nextGuessRound, guessNumber); guessNumber++)
            {
                if (IsImpossible() || !Solve(nextRound))
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
        /// return true if the puzzle has a solution
        /// and only a single solution
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
        /// Count the number of solutions to the puzzle
        /// but return two any time there are two or
        /// more solutions.  This method will run much
        /// faster than countSolutions() when there
        /// are many possible solutions and can be used
        /// when you are interested in knowing if the
        /// puzzle has zero, one, or multiple solutions.
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
            while (SingleSolveMove(round))
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
            while (solveInstructions.Count > 0 && solveInstructions[solveInstructions.Count - 1].GetRound() == round)
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

        private bool SingleSolveMove(int round)
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

        private bool ColBoxReduction(int round)
        {
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                for (int col = 0; col < ROW_COL_SEC_SIZE; col++)
                {
                    int colStart = ColumnToFirstCell(col);
                    bool inOneBox = true;
                    int colBox = -1;
                    for (int i = 0; i < GRID_SIZE; i++)
                    {
                        for (int j = 0; j < GRID_SIZE; j++)
                        {
                            int row = i * GRID_SIZE + j;
                            int position = RowColumnToCell(row, col);
                            int valPos = GetPossibilityIndex(valIndex, position);
                            if (possibilities[valPos] == 0)
                            {
                                if (colBox == -1 || colBox == i)
                                {
                                    colBox = i;
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
                        int row = GRID_SIZE * colBox;
                        int secStart = CellToSectionStartCell(RowColumnToCell(row, col));
                        int secStartRow = CellToRow(secStart);
                        int secStartCol = CellToColumn(secStart);
                        for (int i = 0; i < GRID_SIZE; i++)
                        {
                            for (int j = 0; j < GRID_SIZE; j++)
                            {
                                int row2 = secStartRow + i;
                                int col2 = secStartCol + j;
                                int position = RowColumnToCell(row2, col2);
                                int valPos = GetPossibilityIndex(valIndex, position);
                                if (col != col2 && possibilities[valPos] == 0)
                                {
                                    possibilities[valPos] = round;
                                    doneSomething = true;
                                }
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

        private bool RowBoxReduction(int round)
        {
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                for (int row = 0; row < ROW_COL_SEC_SIZE; row++)
                {
                    int rowStart = RowToFirstCell(row);
                    bool inOneBox = true;
                    int rowBox = -1;
                    for (int i = 0; i < GRID_SIZE; i++)
                    {
                        for (int j = 0; j < GRID_SIZE; j++)
                        {
                            int column = i * GRID_SIZE + j;
                            int position = RowColumnToCell(row, column);
                            int valPos = GetPossibilityIndex(valIndex, position);
                            if (possibilities[valPos] == 0)
                            {
                                if (rowBox == -1 || rowBox == i)
                                {
                                    rowBox = i;
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
                        int column = GRID_SIZE * rowBox;
                        int secStart = CellToSectionStartCell(RowColumnToCell(row, column));
                        int secStartRow = CellToRow(secStart);
                        int secStartCol = CellToColumn(secStart);
                        for (int i = 0; i < GRID_SIZE; i++)
                        {
                            for (int j = 0; j < GRID_SIZE; j++)
                            {
                                int row2 = secStartRow + i;
                                int col2 = secStartCol + j;
                                int position = RowColumnToCell(row2, col2);
                                int valPos = GetPossibilityIndex(valIndex, position);
                                if (row != row2 && possibilities[valPos] == 0)
                                {
                                    possibilities[valPos] = round;
                                    doneSomething = true;
                                }
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

        private bool PointingRowReduction(int round)
        {
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                for (int section = 0; section < ROW_COL_SEC_SIZE; section++)
                {
                    int secStart = SectionToFirstCell(section);
                    bool inOneRow = true;
                    int boxRow = -1;
                    for (int j = 0; j < GRID_SIZE; j++)
                    {
                        for (int i = 0; i < GRID_SIZE; i++)
                        {
                            int secVal = secStart + i + (ROW_COL_SEC_SIZE * j);
                            int valPos = GetPossibilityIndex(valIndex, secVal);
                            if (possibilities[valPos] == 0)
                            {
                                if (boxRow == -1 || boxRow == j)
                                {
                                    boxRow = j;
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
                        int row = CellToRow(secStart) + boxRow;
                        int rowStart = RowToFirstCell(row);

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
                            if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.POINTING_PAIR_TRIPLE_ROW, valIndex + 1, rowStart));
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool PointingColumnReduction(int round)
        {
            for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
            {
                for (int section = 0; section < ROW_COL_SEC_SIZE; section++)
                {
                    int secStart = SectionToFirstCell(section);
                    bool inOneCol = true;
                    int boxCol = -1;
                    for (int i = 0; i < GRID_SIZE; i++)
                    {
                        for (int j = 0; j < GRID_SIZE; j++)
                        {
                            int secVal = secStart + i + (ROW_COL_SEC_SIZE * j);
                            int valPos = GetPossibilityIndex(valIndex, secVal);
                            if (possibilities[valPos] == 0)
                            {
                                if (boxCol == -1 || boxCol == i)
                                {
                                    boxCol = i;
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
                        int col = CellToColumn(secStart) + boxCol;
                        int colStart = ColumnToFirstCell(col);

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
                            if (logHistory || recordHistory) AddHistoryItem(new LogItem(round, LogType.POINTING_PAIR_TRIPLE_COLUMN, valIndex + 1, colStart));
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
            for (int section = 0; section < ROW_COL_SEC_SIZE; section++)
            {
                for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                {
                    int si1 = -1;
                    int si2 = -1;
                    int valCount = 0;
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
                                    int secStart = CellToSectionStartCell(position);
                                    for (int i = 0; i < 3; i++)
                                    {
                                        for (int j = 0; j < 3; j++)
                                        {
                                            int position3 = secStart + i + (ROW_COL_SEC_SIZE * j);
                                            if (position3 != position && position3 != position2 && RemovePossibilitiesInOneFromTwo(position, position3, round))
                                            {
                                                doneSomething = true;
                                            }
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
        /// Mark exactly one cell which is the only possible value for some row, if
        /// such a cell exists. This method will look in a row for a possibility that
        /// is only listed for one cell. This type of cell is often called a
        /// "hidden single"
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
        /// Mark exactly one cell which is the only possible value for some column,
        /// if such a cell exists. This method will look in a column for a
        /// possibility that is only listed for one cell. This type of cell is often
        /// called a "hidden single"
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
        /// Mark exactly one cell which is the only possible value for some section,
        /// if such a cell exists. This method will look in a section for a
        /// possibility that is only listed for one cell. This type of cell is often
        /// called a "hidden single"
        /// </summary>
        private bool OnlyValueInSection(int round)
        {
            for (int sec = 0; sec < ROW_COL_SEC_SIZE; sec++)
            {
                int secPos = SectionToFirstCell(sec);
                for (int valIndex = 0; valIndex < ROW_COL_SEC_SIZE; valIndex++)
                {
                    int count = 0;
                    int lastPosition = 0;
                    for (int i = 0; i < GRID_SIZE; i++)
                    {
                        for (int j = 0; j < GRID_SIZE; j++)
                        {
                            int position = secPos + i + ROW_COL_SEC_SIZE * j;
                            int valPos = GetPossibilityIndex(valIndex, position);
                            if (possibilities[valPos] == 0)
                            {
                                count++;
                                lastPosition = position;
                            }
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
        /// Mark exactly one cell that has a single possibility, if such a cell
        /// exists. This method will look for a cell that has only one possibility.
        /// This type of cell is often called a "single"
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
        /// Mark the given value at the given position. Go through the row, column,
        /// and section for the position and remove the value from the possibilities.
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
                // System.out.println("Row Start: "+rowStart+" Row Value: "+rowVal+" Value Position: "+valPos);
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
                // System.out.println("Col Start: "+colStart+" Col Value: "+colVal+" Value Position: "+valPos);
                if (possibilities[valPos] == 0)
                {
                    possibilities[valPos] = round;
                }
            }

            // Take this value out of the possibilities for everything in section
            int secStart = CellToSectionStartCell(position);
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    int secVal = secStart + i + (ROW_COL_SEC_SIZE * j);
                    int valPos = GetPossibilityIndex(valIndex, secVal);
                    // System.out.println("Sec Start: "+secStart+" Sec Value: "+secVal+" Value Position: "+valPos);
                    if (possibilities[valPos] == 0)
                    {
                        possibilities[valPos] = round;
                    }
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
        /// print the given BOARD_SIZEd array of ints as a sudoku puzzle. Use print
        /// options from member variables.
        /// </summary>
        private void Print(int[] sudoku)
        {
            Debug.Write(PuzzleToString(sudoku));
        }

        private string PuzzleToString(int[] sudoku)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                if (printStyle == PrintStyle.READABLE)
                {
                    sb.Append(" ");
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
                        sb.Append(",");
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

        public int[] GetSolution()
        {
            int[] clone = new int[solution.Length];
            Array.Copy(solution, clone, solution.Length);
            return clone;
        }

        /// <summary>
        /// Given a vector of LogItems, determine how many log items in the vector
        /// are of the specified type.
        /// </summary>
        private int GetLogCount(List<LogItem> v, LogType type)
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
                int temp = array[i];
                array[i] = array[randTailPos];
                array[randTailPos] = temp;
            }
        }

        private static Symmetry GetRandomSymmetry()
        {
            Symmetry[] values = SymmetryExtensions.Values();
            // not the first and last value which are NONE and RANDOM
            return values[(random.Next() % (values.Length - 1)) + 1];
        }

        /// <summary>
        /// Given the index of a cell (0-80) calculate the column (0-8) in which that
        /// cell resides.
        /// </summary>
        public static int CellToColumn(int cell)
        {
            return cell % ROW_COL_SEC_SIZE;
        }

        /// <summary>
        /// Given the index of a cell (0-80) calculate the row (0-8) in which it
        /// resides.
        /// </summary>
        public static int CellToRow(int cell)
        {
            return cell / ROW_COL_SEC_SIZE;
        }

        /// <summary>
        /// Given the index of a cell (0-80) calculate the section (0-8) in which it
        /// resides.
        /// </summary>
        public static int CellToSection(int cell)
        {
            return (cell / SEC_GROUP_SIZE * GRID_SIZE)
            + (CellToColumn(cell) / GRID_SIZE);
        }

        /// <summary>
        /// Given the index of a cell (0-80) calculate the cell (0-80) that is the
        /// upper left start cell of that section.
        /// </summary>
        public static int CellToSectionStartCell(int cell)
        {
            return (cell / SEC_GROUP_SIZE * SEC_GROUP_SIZE)
            + (CellToColumn(cell) / GRID_SIZE * GRID_SIZE);
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
            return (section % GRID_SIZE * GRID_SIZE)
            + (section / GRID_SIZE * SEC_GROUP_SIZE);
        }

        /// <summary>
        /// Given a value for a cell (0-8) and a cell number (0-80) calculate the
        /// offset into the possibility array (0-728).
        /// </summary>
        static int GetPossibilityIndex(int valueIndex, int cell)
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
        /// Given a section (0-8) and an offset into that section (0-8) calculate the
        /// cell (0-80)
        /// </summary>
        public static int SectionToCell(int section, int offset)
        {
            return SectionToFirstCell(section)
                + (offset / GRID_SIZE * ROW_COL_SEC_SIZE)
                + (offset % GRID_SIZE);
        }
    }
}
