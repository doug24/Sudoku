using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace QQWingLib
{
    public static class Util
    {
        public static string SerializeCandidates(HashSet<int>[] candidates)
        {
            if (candidates == null) return string.Empty;

            int[] masks = new int[candidates.Length];
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null)
                {
                    foreach (int val in candidates[i])
                        masks[i] |= (1 << val);
                }
            }
            return string.Join(",", masks);
        }

        public static HashSet<int>[] DeserializeCandidates(string candidateMasksCsv)
        {
            if (string.IsNullOrEmpty(candidateMasksCsv)) return null;

            string[] masks = candidateMasksCsv.Split(','); 

            HashSet<int>[] candidates = new HashSet<int>[masks.Length];
            for (int i = 0; i < masks.Length; i++)
            {
                if (int.TryParse(masks[i], out int  mask))
                if (mask != 0)
                {
                    candidates[i] = [];
                    for (int val = 1; val <= 9; val++)
                    {
                        if ((mask & (1 << val)) != 0)
                            candidates[i].Add(val);
                    }
                }
            }
            return candidates;
        }

        public static (int[] Puzzle, int[] Solution, string Difficulty, string[] SolveSteps) ParsePuzzleData(string puzzleData)
        {
            // PuzzleData must contain 81 numbers. Zeroes can be dots or other punctuation.
            // This importer can handle noisy formats like grids with lines and dots.
            // As long as there are 81 separated numbers.
            // Any number greater than 9 will be treated as a set of candidates.

            List<string> tokens = ParseTokens(puzzleData);

            if (tokens.Count != 81)
            {
                throw new ArgumentException(
                     $"Expected 81 numbers but found {tokens.Count}. " +
                     "Please check the input and try again.", nameof(puzzleData));
            }

            int[] initialBoard = new int[81];

            for (int i = 0; i < 81; i++)
            {
                int value = int.Parse(tokens[i]);
                if (value >= 1 && value <= 9)
                {
                    initialBoard[i] = value;
                }
                // else value == 0: empty cell
            }

            int[] solution = new int[81];
            string difficulty = string.Empty;
            string[] solveSteps = null;

            QQWing.SectionLayout = new RegularLayout();
            QQWing ss = new();
            ss.SetRecordHistory(true);
            ss.SetPuzzle(initialBoard);
            ss.Solve(CancellationToken.None);
            if (ss.IsSolved())
            {
                // If the puzzle was solved with a Guess, it is not be suitable for unit tests:
                // The guess can occur at different locations in each solve. And that makes all 
                // subsequent solve steps different, which makes it impossible to have a stable
                // set of solve steps for unit tests.
                if (ss.GetDifficulty() == Difficulty.EXPERT)
                {
                    throw new Exception("This puzzle was solved with a Guess, which can't be used in unit tests.");
                }

                solution = ss.GetSolution();

                // Get difficulty and solve steps before HasMultipleSolutions,
                // which calls Reset and clears the solve instructions
                difficulty = ss.GetDifficultyAsString();
                solveSteps = ss.GetCompactSolveInstructions();

                if (ss.HasMultipleSolutions())
                {
                    throw new InvalidOperationException(
                        "The provided puzzle has multiple solutions. " +
                        "Please provide a puzzle with a unique solution.");
                }
            }
            else
            {
                throw new Exception("The puzzle did not solve.");
            }

            return (initialBoard, solution, difficulty, solveSteps);
        }

        /// <summary>
        /// Parse puzzle data into tokens. PuzzleData must contain 81 numbers. 
        /// Zeroes can be dots or other punctuation.
        /// This parser can handle noisy formats like grids with lines and dots.
        /// As long as there are 81 separated numbers.
        /// Any number greater than 9 will be treated as a set of candidates.
        /// </summary>
        /// <param name="puzzleData"></param>
        /// <returns></returns>
        public static List<string> ParseTokens(string puzzleData)
        {
            // Try separated format first: treat '.' as '0', extract groups of digits,
            // and ignore grid decoration characters like '-', '+', '|'
            List<string> tokens = [];
            StringBuilder sb = new();

            foreach (char c in puzzleData)
            {
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
                else if (c == '.')
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                    tokens.Add("0");
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                }
            }

            if (sb.Length > 0)
                tokens.Add(sb.ToString());

            if (tokens.Count == 81)
                return tokens;

            // Fall back to compact format: each digit or dot is one cell
            tokens.Clear();
            foreach (char c in puzzleData)
            {
                if (char.IsDigit(c))
                    tokens.Add(c.ToString());
                else if (c == '.')
                    tokens.Add("0");
            }

            return tokens;
        }
    }
}
