﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using QQWingLib;

namespace Sudoku
{
    public class Puzzle
    {
        public Puzzle()
        {
        }

        public string Initial { get; private set; }
        public string Solution { get; private set; }

        //public string Initial  { get => ".....9.5.1.....2...2.7..8.6.45.1....89..4.51............8..2..9.5.4.8.....1..7..."; }
        //public string Solution { get => "784629351136584297529731846645913728897246513312875964478152639953468172261397485"; }


        public async Task Generate()
        {
            Symmetry symmetry = Symmetry.ROTATE90;
            Difficulty difficulty = Difficulty.INTERMEDIATE;

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var token = cancellationTokenSource.Token;
                try
                {
                    List<Task<PuzzleData>> tasks = new List<Task<PuzzleData>>();
                    for (int idx = 0; idx < 4; idx++)
                    {
                        tasks.Add(Task.Run(() => GenerateInternal(symmetry, difficulty, token), token));
                    }

                    Task<PuzzleData> completedTask = await Task.WhenAny(tasks);
                    cancellationTokenSource.Cancel();

                    Initial = completedTask.Result.Initial;
                    Solution = completedTask.Result.Solution;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error generating puzzle: " + e.Message);
                }
            }
        }
        private static PuzzleData GenerateInternal(Symmetry symmetry, Difficulty difficulty, CancellationToken token)
        {
            PuzzleData result = null;
            bool done = false;

            QQWing ss = new QQWing();
            ss.setRecordHistory(true);
            //ss.setLogHistory(true);
            ss.setPrintStyle(PrintStyle.ONE_LINE);

            // Solve puzzle or generate puzzles
            // until end of input for solving, or
            // until we have generated the specified number.
            while (!done && !token.IsCancellationRequested)
            {
                // Record whether the puzzle was possible or not,
                // so that we don't try to solve impossible givens.
                bool havePuzzle = ss.generatePuzzleSymmetry(symmetry);

                if (token.IsCancellationRequested) break;

                if (havePuzzle)
                {
                    // Solve the puzzle
                    ss.solve();

                    // Bail out if it didn't meet the difficulty standards for generation
                    if (difficulty != Difficulty.UNKNOWN && difficulty != ss.getDifficulty())
                    {
                        havePuzzle = false;
                    }
                    else
                    {
                        done = true;
                    }
                }

                // Check havePuzzle again, it may have changed based on difficulty
                if (havePuzzle)
                {
                    if (ss.isSolved())
                    {
                        result = new PuzzleData(
                            ss.getPuzzleString().TrimEnd(),
                            ss.getSolutionString().TrimEnd());
                    }
                    else
                    {
                        done = false;
                    }
                }
            }

            return result;
        }
    }

    public class PuzzleData
    {
        public PuzzleData(string initial, string solution)
        {
            Initial = initial;
            Solution = solution;
        }

        public string Initial { get; private set; }
        public string Solution { get; private set; }

        public override string ToString()
        {
            return Initial;
        }
    }
}
