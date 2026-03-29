using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QQWingLib;

namespace Sudoku;

public class Puzzle
{
    public Puzzle()
    {
    }

    public int[] Initial { get; private set; } = [];
    public int[] Solution { get; private set; } = [];
    public string Difficulty { get; private set; } = string.Empty;
    public List<string> Strategies { get; private set; } = [];

    //public string Initial  { get => ".....9.5.1.....2...2.7..8.6.45.1....89..4.51............8..2..9.5.4.8.....1..7..."; }
    //public string Solution { get => "784629351136584297529731846645913728897246513312875964478152639953468172261397485"; }


    public async Task Generate(Difficulty difficulty, Symmetry symmetry)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        var timeout = TimeSpan.FromSeconds(20);
        using CancellationTokenSource cancellationTokenSource = new(timeout);
        var token = cancellationTokenSource.Token;
        List<Task<PuzzleData>> tasks = [];
        for (int idx = 0; idx < 4; idx++)
        {
            tasks.Add(Task.Run(() => GenerateInternal(symmetry, difficulty, token), token));
        }

        Task<PuzzleData> completedTask = await Task.WhenAny(tasks);
        cancellationTokenSource.Cancel();

        Initial = completedTask.Result.Initial;
        Solution = completedTask.Result.Solution;
        Difficulty = completedTask.Result.Difficulty;
        Strategies = completedTask.Result.Strategies;

        await Task.WhenAll(tasks);

        if (Initial.Length == 0)
        {
            CustomMessageBox.Show($"Could not generate a new puzzle in {timeout.TotalSeconds} seconds:" +
                Environment.NewLine + "Try again with different settings.", "Sudoku", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        Mouse.OverrideCursor = Cursors.Arrow;
    }
    private static PuzzleData GenerateInternal(Symmetry symmetry, Difficulty difficulty, CancellationToken token)
    {
        try
        {
            bool done = false;

            QQWing ss = new();
            ss.SetPrintStyle(PrintStyle.ONE_LINE);

            // Solve puzzle or generate puzzles
            // until end of input for solving, or
            // until we have generated the specified number.
            while (!done && !token.IsCancellationRequested)
            {
                // Record whether the puzzle was possible or not,
                // so that we don't try to solve impossible givens.
                bool havePuzzle = ss.GeneratePuzzleSymmetry(symmetry, token);

                token.ThrowIfCancellationRequested();

                if (havePuzzle)
                {
                    // Solve with history recording so we can check difficulty
                    // and get strategies used in a single pass
                    ss.SetRecordHistory(true);
                    ss.Solve(token);

                    // Bail out if it didn't meet the difficulty standards for generation
                    if (difficulty != QQWingLib.Difficulty.UNKNOWN && difficulty != ss.GetDifficulty())
                    {
                        Debug.WriteLine($"Discard solution, difficulty is {ss.GetDifficulty()}");
                        havePuzzle = false;
                    }
                    else
                    {
                        done = true;
                    }
                }

                token.ThrowIfCancellationRequested();

                // Check havePuzzle again, it may have changed based on difficulty
                if (havePuzzle)
                {
                    if (ss.IsSolved())
                    {
                        Debug.WriteLine($"Puzzle GenerateInternal returning {ss.GetDifficulty()} puzzle");
                        return new PuzzleData(
                            ss.GetPuzzle(),
                            ss.GetSolution(),
                            ss.GetDifficultyAsString(),
                            ss.GetStrategiesUsed());
                    }
                    else
                    {
                        done = false;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Puzzle GenerateInternal was canceled");
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            CustomMessageBox.Show("Error generating puzzle: " + e.Message, "Sudoku", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return PuzzleData.Empty;
    }
}

public class PuzzleData(int[] initial, int[] solution, string difficulty, List<string> strategies)
{
    public int[] Initial { get; private set; } = initial;
    public int[] Solution { get; private set; } = solution;
    public string Difficulty { get; private set; } = difficulty;
    public List<string> Strategies { get; private set; } = strategies;

    public static PuzzleData Empty => new([], [], string.Empty, []);

    public override string ToString()
    {
        return string.Join("", Initial);
    }
}