// @formatter:off
/*
 * qqwing - Sudoku solver and generator
 * Copyright (C) 2014 Stephen Ostermiller
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
// @formatter:on
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QQWingLib
{
    class QQWingMain
    {

        private static readonly String NL = Environment.NewLine;

        private QQWingMain()
        {
        }

        /**
         * Main method -- the entry point into the program. Run with --help as an
         * argument for usage and documentation
         */
        public static async void main(String[] argv)
        {
            // Start time for the application for timing
            Stopwatch sw = new Stopwatch();
            sw.Start();

            QQWingOptions opts = new QQWingOptions();

            // Read the arguments and set the options
            for (int i = 0; i < argv.Length; i++)
            {
                if (argv[i].Equals("--puzzle"))
                {
                    opts.printPuzzle = true;
                }
                else if (argv[i].Equals("--nopuzzle"))
                {
                    opts.printPuzzle = false;
                }
                else if (argv[i].Equals("--solution"))
                {
                    opts.printSolution = true;
                }
                else if (argv[i].Equals("--nosolution"))
                {
                    opts.printSolution = false;
                }
                else if (argv[i].Equals("--history"))
                {
                    opts.printHistory = true;
                }
                else if (argv[i].Equals("--nohistory"))
                {
                    opts.printHistory = false;
                }
                else if (argv[i].Equals("--instructions"))
                {
                    opts.printInstructions = true;
                }
                else if (argv[i].Equals("--noinstructions"))
                {
                    opts.printInstructions = false;
                }
                else if (argv[i].Equals("--stats"))
                {
                    opts.printStats = true;
                }
                else if (argv[i].Equals("--nostats"))
                {
                    opts.printStats = false;
                }
                else if (argv[i].Equals("--timer"))
                {
                    opts.timer = true;
                }
                else if (argv[i].Equals("--notimer"))
                {
                    opts.timer = false;
                }
                else if (argv[i].Equals("--count-solutions"))
                {
                    opts.countSolutions = true;
                }
                else if (argv[i].Equals("--nocount-solutions"))
                {
                    opts.countSolutions = false;
                }
                else if (argv[i].Equals("--threads"))
                {
                    i++;
                    if (i >= argv.Length)
                    {
                        Console.Error.WriteLine("Please specify a number of threads.");
                        Environment.Exit(1);
                    }

                    if (int.TryParse(argv[i], out int result))
                    {
                        opts.threads = result;
                    }
                    else
                    {
                        Console.Error.WriteLine("Invalid number of threads: " + argv[i]);
                        Environment.Exit(1);
                    }
                }
                else if (argv[i].Equals("--generate"))
                {
                    opts.action = Action.GENERATE;
                    opts.printPuzzle = true;
                    if (i + 1 < argv.Length && !argv[i + 1].StartsWith("-"))
                    {
                        if (int.TryParse(argv[i + 1], out int result))
                        {
                            opts.numberToGenerate = result;
                        }
                        else
                        {
                            opts.numberToGenerate = 0;
                        }

                        if (opts.numberToGenerate <= 0)
                        {
                            Console.Error.WriteLine("Bad number of puzzles to generate: " + argv[i + 1]);
                            Environment.Exit(1);
                        }
                        i++;
                    }
                }
                else if (argv[i].Equals("--difficulty"))
                {
                    if (argv.Length <= i + 1)
                    {
                        Console.Error.WriteLine("Please specify a difficulty.");
                        Environment.Exit(1);
                    }
                    if (Enum.TryParse(argv[i + 1], out Difficulty result))
                    {
                        opts.difficulty = result;
                    }
                    else
                    {
                        Console.Error.WriteLine("Difficulty expected to be simple, easy, intermediate, expert, or any, not " + argv[i + 1]);
                        Environment.Exit(1);
                    }
                    i++;
                }
                else if (argv[i].Equals("--symmetry"))
                {
                    if (argv.Length <= i + 1)
                    {
                        Console.Error.WriteLine("Please specify a symmetry.");
                        Environment.Exit(1);
                    }
                    if (Enum.TryParse(argv[i + 1], out Symmetry result))
                    {
                        opts.symmetry = result;
                    }
                    else
                    {
                        Console.Error.WriteLine("Symmetry expected to be none, rotate90, rotate180, mirror, flip, or random, not " + argv[i + 1]);
                        Environment.Exit(1);
                    }
                    i++;
                }
                else if (argv[i].Equals("--solve"))
                {
                    opts.action = Action.SOLVE;
                    opts.printSolution = true;
                }
                else if (argv[i].Equals("--log-history"))
                {
                    opts.logHistory = true;
                }
                else if (argv[i].Equals("--nolog-history"))
                {
                    opts.logHistory = false;
                }
                else if (argv[i].Equals("--one-line"))
                {
                    opts.printStyle = PrintStyle.ONE_LINE;
                }
                else if (argv[i].Equals("--compact"))
                {
                    opts.printStyle = PrintStyle.COMPACT;
                }
                else if (argv[i].Equals("--readable"))
                {
                    opts.printStyle = PrintStyle.READABLE;
                }
                else if (argv[i].Equals("--csv"))
                {
                    opts.printStyle = PrintStyle.CSV;
                }
                else if (argv[i].Equals("-n") || argv[i].Equals("--number"))
                {
                    if (i + 1 < argv.Length)
                    {
                        if (int.TryParse(argv[i + 1], out int result))
                        {
                            opts.numberToGenerate = result;
                        }
                        i++;
                    }
                    else
                    {
                        Console.Error.WriteLine("Please specify a number.");
                        Environment.Exit(1);
                    }
                }
                else if (argv[i].Equals("-h") || argv[i].Equals("--help") || argv[i].Equals("help") || argv[i].Equals("?"))
                {
                    printHelp();
                    Environment.Exit(0);
                }
                else if (argv[i].Equals("--version"))
                {
                    printVersion();
                    Environment.Exit(0);
                }
                else if (argv[i].Equals("--about"))
                {
                    printAbout();
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("Unknown argument: '" + argv[i] + "'");
                    printHelp();
                    Environment.Exit(0);
                }
            }

            if (opts.action == Action.NONE)
            {
                Console.WriteLine("Either --solve or --generate must be specified.");
                printHelp();
                Environment.Exit(1);
            }

            // If printing out CSV, print a header
            if (opts.printStyle == PrintStyle.CSV)
            {
                if (opts.printPuzzle) Console.Write("Puzzle,");
                if (opts.printSolution) Console.Write("Solution,");
                if (opts.printHistory) Console.Write("Solve History,");
                if (opts.printInstructions) Console.Write("Solve Instructions,");
                if (opts.countSolutions) Console.Write("Solution Count,");
                if (opts.timer) Console.Write("Time (milliseconds),");
                if (opts.printStats) Console.Write("Givens,Singles,Hidden Singles,Naked Pairs,Hidden Pairs,Pointing Pairs/Triples,Box/Line Intersections,Guesses,Backtracks,Difficulty");
                Console.WriteLine("");
            }

            // The number of puzzles solved or generated.
            puzzleCount = 0;

            // WARNING this code is not tested!!!!!
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var token = cancellationTokenSource.Token;
                try
                {
                    List<Task> tasks = new List<Task>();
                    for (int idx = 0; idx < opts.threads; idx++)
                    {
                        tasks.Add(Task.Run(() => threadProc(opts, token), token));
                    }

                    await Task.WhenAny(tasks);
                    cancellationTokenSource.Cancel();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error generating puzzle: " + e.Message);
                }
            }

            // Print out the time it took to do everything
            if (opts.timer)
            {
                double t = sw.ElapsedMilliseconds / 1000;
                int count = opts.action == Action.GENERATE ? opts.numberToGenerate : puzzleCount;
                Console.WriteLine(count + " puzzle" + ((count == 1) ? "" : "s") + " " + (opts.action == Action.GENERATE ? "generated" : "solved") + " in " + t + " seconds.");
            }
            Environment.Exit(0);
        }

        private static int puzzleCount;

        private static void threadProc(QQWingOptions opts, CancellationToken token)
        {
            QQWing ss = new QQWing();
            ss.setRecordHistory(opts.printHistory || opts.printInstructions || opts.printStats || opts.difficulty != Difficulty.UNKNOWN);
            ss.setLogHistory(opts.logHistory);
            ss.setPrintStyle(opts.printStyle);

            bool done = false;
            try
            {
                // Solve puzzle or generate puzzles
                // until end of input for solving, or
                // until we have generated the specified number.
                while (!done && !token.IsCancellationRequested)
                {
                    // record the start time for the timer.
                    long puzzleStartTime = DateTime.Now.Ticks;

                    // if something has been printed for this
                    // particular puzzle
                    StringBuilder output = new StringBuilder();

                    // Record whether the puzzle was possible or
                    // not,
                    // so that we don't try to solve impossible
                    // givens.
                    bool havePuzzle = false;

                    if (opts.action == Action.GENERATE)
                    {
                        // Generate a puzzle
                        havePuzzle = ss.generatePuzzleSymmetry(opts.symmetry);

                        if (!havePuzzle && opts.printPuzzle)
                        {
                            output.Append("Could not generate puzzle.");
                            if (opts.printStyle == PrintStyle.CSV)
                            {
                                output.Append(",").Append(NL);
                            }
                            else
                            {
                                output.Append(NL);
                            }
                        }
                    }
                    else
                    {
                        // Read the next puzzle on STDIN
                        int[] puzzle = new int[QQWing.BOARD_SIZE];
                        if (readPuzzleFromStdIn(puzzle))
                        {
                            havePuzzle = ss.setPuzzle(puzzle);
                            if (havePuzzle)
                            {
                                Interlocked.Decrement(ref puzzleCount);
                            }
                            else
                            {
                                if (opts.printPuzzle)
                                {
                                    output.Append(ss.getPuzzleString());
                                }
                                if (opts.printSolution)
                                {
                                    output.Append("Puzzle is not possible.");
                                    if (opts.printStyle == PrintStyle.CSV)
                                    {
                                        output.Append(",");
                                    }
                                    else
                                    {
                                        output.Append(NL);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Set loop to terminate when nothing is
                            // left on STDIN
                            havePuzzle = false;
                            done = true;
                        }
                        puzzle = null;
                    }

                    int solutions = 0;

                    if (token.IsCancellationRequested) break;

                    if (havePuzzle)
                    {
                        // Count the solutions if requested.
                        // (Must be done before solving, as it would
                        // mess up the stats.)
                        if (opts.countSolutions)
                        {
                            solutions = ss.countSolutions();
                        }

                        // Solve the puzzle
                        if (opts.printSolution || opts.printHistory || opts.printStats || opts.printInstructions || opts.difficulty != Difficulty.UNKNOWN)
                        {
                            ss.solve();
                        }

                        // Bail out if it didn't meet the difficulty
                        // standards for generation
                        if (opts.action == Action.GENERATE)
                        {
                            if (opts.difficulty != Difficulty.UNKNOWN && opts.difficulty != ss.getDifficulty())
                            {
                                havePuzzle = false;
                                // check if other threads have
                                // finished the job
                                if (puzzleCount >= opts.numberToGenerate)
                                    done = true;
                            }
                            else
                            {
                                int numDone = Interlocked.Increment(ref puzzleCount);
                                if (numDone >= opts.numberToGenerate) done = true;
                                if (numDone > opts.numberToGenerate) havePuzzle = false;
                            }
                        }
                    }

                    // Check havePuzzle again, it may have changed
                    // based on difficulty
                    if (havePuzzle)
                    {
                        // With a puzzle now in hand and possibly
                        // solved
                        // print out the solution, stats, etc.
                        // Record the end time for the timer.
                        long puzzleDoneTime = DateTime.Now.Ticks;

                        // Print the puzzle itself.
                        if (opts.printPuzzle) output.Append(ss.getPuzzleString());

                        // Print the solution if there is one
                        if (opts.printSolution)
                        {
                            if (ss.isSolved())
                            {
                                output.Append(ss.getSolutionString());
                            }
                            else
                            {
                                output.Append("Puzzle has no solution.");
                                if (opts.printStyle == PrintStyle.CSV)
                                {
                                    output.Append(",");
                                }
                                else
                                {
                                    output.Append(NL);
                                }
                            }
                        }

                        // Print the steps taken to solve or attempt
                        // to solve the puzzle.
                        if (opts.printHistory) output.Append(ss.getSolveHistoryString());
                        // Print the instructions for solving the
                        // puzzle
                        if (opts.printInstructions) output.Append(ss.getSolveInstructionsString());

                        // Print the number of solutions to the
                        // puzzle.
                        if (opts.countSolutions)
                        {
                            if (opts.printStyle == PrintStyle.CSV)
                            {
                                output.Append(solutions + ",");
                            }
                            else
                            {
                                if (solutions == 0)
                                {
                                    output.Append("There are no solutions to the puzzle.").Append(NL);
                                }
                                else if (solutions == 1)
                                {
                                    output.Append("The solution to the puzzle is unique.").Append(NL);
                                }
                                else
                                {
                                    output.Append("There are " + solutions + " solutions to the puzzle.").Append(NL);
                                }
                            }
                        }

                        // Print out the time it took to solve the
                        // puzzle.
                        if (opts.timer)
                        {
                            double t = ((double)(puzzleDoneTime - puzzleStartTime)) / 1000.0;
                            if (opts.printStyle == PrintStyle.CSV)
                            {
                                output.Append(t + ",");
                            }
                            else
                            {
                                output.Append("Time: " + t + " milliseconds").Append(NL);
                            }
                        }

                        // Print any stats we were able to gather
                        // while solving the puzzle.
                        if (opts.printStats)
                        {
                            int givenCount = ss.getGivenCount();
                            int singleCount = ss.getSingleCount();
                            int hiddenSingleCount = ss.getHiddenSingleCount();
                            int nakedPairCount = ss.getNakedPairCount();
                            int hiddenPairCount = ss.getHiddenPairCount();
                            int pointingPairTripleCount = ss.getPointingPairTripleCount();
                            int boxReductionCount = ss.getBoxLineReductionCount();
                            int guessCount = ss.getGuessCount();
                            int backtrackCount = ss.getBacktrackCount();
                            String difficultyString = ss.getDifficultyAsString();
                            if (opts.printStyle == PrintStyle.CSV)
                            {
                                output.Append(givenCount).Append(",").Append(singleCount).Append(",")
                                    .Append(hiddenSingleCount).Append(",").Append(nakedPairCount)
                                    .Append(",").Append(hiddenPairCount).Append(",")
                                    .Append(pointingPairTripleCount).Append(",").Append(boxReductionCount)
                                    .Append(",").Append(guessCount).Append(",").Append(backtrackCount)
                                    .Append(",").Append(difficultyString).Append(",");
                            }
                            else
                            {
                                output.Append("Number of Givens: ").Append(givenCount).Append(NL);
                                output.Append("Number of Singles: ").Append(singleCount).Append(NL);
                                output.Append("Number of Hidden Singles: ").Append(hiddenSingleCount).Append(NL);
                                output.Append("Number of Naked Pairs: ").Append(nakedPairCount).Append(NL);
                                output.Append("Number of Hidden Pairs: ").Append(hiddenPairCount).Append(NL);
                                output.Append("Number of Pointing Pairs/Triples: ").Append(pointingPairTripleCount).Append(NL);
                                output.Append("Number of Box/Line Intersections: ").Append(boxReductionCount).Append(NL);
                                output.Append("Number of Guesses: ").Append(guessCount).Append(NL);
                                output.Append("Number of Backtracks: ").Append(backtrackCount).Append(NL);
                                output.Append("Difficulty: ").Append(difficultyString).Append(NL);
                            }
                        }
                    }
                    if (output.Length > 0)
                    {
                        if (opts.printStyle == PrintStyle.CSV) output.Append(NL);
                        Console.Write(output);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
                Environment.Exit(1);
            }
        }

        private static void printVersion()
        {
            Console.WriteLine("qqwing " + QQWing.QQWING_VERSION);
        }


        private static void printAbout()
        {
            Console.WriteLine("qqwing - Sudoku solver and generator");
            Console.WriteLine("Copyright (C) 2006-2014 Stephen Ostermiller http://ostermiller.org/");
            Console.WriteLine("Copyright (C) 2007 Jacques Bensimon (jacques@ipm.com)");
            Console.WriteLine("Copyright (C) 2007 Joel Yarde (joel.yarde - gmail.com)");
            Console.WriteLine("");
            Console.WriteLine("This program is free software; you can redistribute it and/or modify");
            Console.WriteLine("it under the terms of the GNU General Public License as published by");
            Console.WriteLine("the Free Software Foundation; either version 2 of the License, or");
            Console.WriteLine("(at your option) any later version.");
            Console.WriteLine("");
            Console.WriteLine("This program is distributed in the hope that it will be useful,");
            Console.WriteLine("but WITHOUT ANY WARRANTY; without even the implied warranty of");
            Console.WriteLine("MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the");
            Console.WriteLine("GNU General Public License for more details.");
            Console.WriteLine("");
            Console.WriteLine("You should have received a copy of the GNU General Public License along");
            Console.WriteLine("with this program; if not, write to the Free Software Foundation, Inc.,");
            Console.WriteLine("51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.");
        }

        private static void printHelp()
        {
            Console.WriteLine("qqwing <options>");
            Console.WriteLine("Sudoku solver and generator.");
            Console.WriteLine("  --generate <num>     Generate new puzzles");
            Console.WriteLine("  --solve              Solve all the puzzles from standard input");
            Console.WriteLine("  --difficulty <diff>  Generate only simple, easy, intermediate, expert, or any");
            Console.WriteLine("  --symmetry <sym>     Symmetry: none, rotate90, rotate180, mirror, flip, or random");
            Console.WriteLine("  --puzzle             Print the puzzle (default when generating)");
            Console.WriteLine("  --nopuzzle           Do not print the puzzle (default when solving)");
            Console.WriteLine("  --solution           Print the solution (default when solving)");
            Console.WriteLine("  --nosolution         Do not print the solution (default when generating)");
            Console.WriteLine("  --stats              Print statistics about moves used to solve the puzzle");
            Console.WriteLine("  --nostats            Do not print statistics (default)");
            Console.WriteLine("  --timer              Print time to generate or solve each puzzle");
            Console.WriteLine("  --notimer            Do not print solve or generation times (default)");
            Console.WriteLine("  --threads            Number of processes (default available processors)");
            Console.WriteLine("  --count-solutions    Count the number of solutions to puzzles");
            Console.WriteLine("  --nocount-solutions  Do not count the number of solutions (default)");
            Console.WriteLine("  --history            Print trial and error used when solving");
            Console.WriteLine("  --nohistory          Do not print trial and error to solve (default)");
            Console.WriteLine("  --instructions       Print the steps (at least 81) needed to solve the puzzle");
            Console.WriteLine("  --noinstructions     Do not print steps to solve (default)");
            Console.WriteLine("  --log-history        Print trial and error to solve as it happens");
            Console.WriteLine("  --nolog-history      Do not print trial and error  to solve as it happens");
            Console.WriteLine("  --one-line           Print puzzles on one line of 81 characters");
            Console.WriteLine("  --compact            Print puzzles on 9 lines of 9 characters");
            Console.WriteLine("  --readable           Print puzzles in human readable form (default)");
            Console.WriteLine("  --csv                Output CSV format with one line puzzles");
            Console.WriteLine("  --help               Print this message");
            Console.WriteLine("  --about              Author and license information");
            Console.WriteLine("  --version            Display current version number");
        }

        /**
         * Read a sudoku puzzle from standard input. STDIN is processed one
         * character at a time until the sudoku is filled in. Any digit or period is
         * used to fill the sudoku, any other character is ignored.
         */
        private static bool readPuzzleFromStdIn(int[] puzzle)
        {
            //synchronized (System.in) {
            //int read = 0;
            //while (read<QQWing.BOARD_SIZE) {
            //    int c = System.in.read();
            //    if (c< 0) return false;
            //    if (c >= '1' && c <= '9') {
            //        puzzle[read] = c - '0';
            //        read++;
            //    }
            //    if (c == '.' || c == '0') {
            //        puzzle[read] = 0;
            //        read++;
            //    }
            //}
            return true;
        }
    }

    public class QQWingOptions
    {
        // defaults for options
        public bool printPuzzle = false;

        public bool printSolution = false;

        public bool printHistory = false;

        public bool printInstructions = false;

        public bool timer = false;

        public bool countSolutions = false;

        public Action action = Action.NONE;

        public bool logHistory = false;

        public PrintStyle printStyle = PrintStyle.READABLE;

        public int numberToGenerate = 1;

        public bool printStats = false;

        public Difficulty difficulty = Difficulty.UNKNOWN;

        public Symmetry symmetry = Symmetry.NONE;

        public int threads = Environment.ProcessorCount;
    }

}
