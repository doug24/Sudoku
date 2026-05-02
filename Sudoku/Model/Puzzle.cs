using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public List<Cage> Cages { get; private set; } = [];

    //public string Initial  { get => ".....9.5.1.....2...2.7..8.6.45.1....89..4.51............8..2..9.5.4.8.....1..7..."; }
    //public string Solution { get => "784629351136584297529731846645913728897246513312875964478152639953468172261397485"; }


    public async Task Generate(Difficulty difficulty, Symmetry symmetry)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        using CancellationTokenSource cancellationTokenSource = new();
        var token = cancellationTokenSource.Token;
        List<Task<PuzzleData>> tasks = [];
        for (int idx = 0; idx < 8; idx++)
        {
            tasks.Add(Task.Run(() => GenerateInternal(symmetry, difficulty, token), token));
        }

        Task<PuzzleData> completedTask = await WaitWithCancelDialog(
            Task.WhenAny(tasks), cancellationTokenSource,
            "Generating puzzle\u2026 this is taking longer than expected.");

        cancellationTokenSource.Cancel();

        Initial = completedTask.Result.Initial;
        Solution = completedTask.Result.Solution;
        Difficulty = completedTask.Result.Difficulty;
        Strategies = completedTask.Result.Strategies;

        await Task.WhenAll(tasks);

        Mouse.OverrideCursor = Cursors.Arrow;

        if (Initial.Length == 0 && !token.IsCancellationRequested)
        {
            CustomMessageBox.Show("Could not generate a new puzzle." +
                Environment.NewLine + "Try again with different settings.", "Sudoku", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private static readonly TimeSpan DialogDelay = TimeSpan.FromSeconds(10);

    private static async Task<Task<T>> WaitWithCancelDialog<T>(
        Task<Task<T>> whenAny,
        CancellationTokenSource cts,
        string message)
    {
        var delayTask = Task.Delay(DialogDelay);
        var innerTask = await Task.WhenAny(whenAny, delayTask);

        if (innerTask == whenAny)
        {
            // Completed before dialog delay — no dialog needed
            return await whenAny;
        }

        // Still running after delay — show cancel dialog on UI thread
        GenerationProgressDialog? dialog = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            dialog = new GenerationProgressDialog(message);
            var activeWindow = Application.Current.Windows.OfType<Window>()
                .SingleOrDefault(x => x.IsActive);
            if (activeWindow != null)
                dialog.Owner = activeWindow;
        });

        // Monitor both the generation task and the dialog closing
        var tcs = new TaskCompletionSource<Task<T>>();

        _ = whenAny.ContinueWith(t =>
        {
            Application.Current.Dispatcher.Invoke(() => dialog?.Close());
            tcs.TrySetResult(t.Result);
        }, TaskScheduler.Default);

        Application.Current.Dispatcher.Invoke(() =>
        {
            dialog!.Closed += (s, e) =>
            {
                if (dialog.Cancelled)
                {
                    cts.Cancel();
                    // Return the first task regardless (it will hold an empty result after cancellation)
                    _ = whenAny.ContinueWith(t => tcs.TrySetResult(t.Result), TaskScheduler.Default);
                }
            };
            dialog.Show();
        });

        return await tcs.Task;
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

    public async Task GenerateKiller(Difficulty difficulty, Symmetry symmetry = Symmetry.NONE)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        using CancellationTokenSource cancellationTokenSource = new();
        var token = cancellationTokenSource.Token;
        List<Task<KillerPuzzle?>> tasks = [];
        for (int idx = 0; idx < 8; idx++)
        {
            tasks.Add(Task.Run(() => GenerateKillerInternal(difficulty, symmetry, token), token));
        }

        Task<KillerPuzzle?> completedTask = await WaitWithCancelDialog(
            Task.WhenAny(tasks), cancellationTokenSource,
            "Generating Killer puzzle\u2026 this is taking longer than expected.");

        cancellationTokenSource.Cancel();

        KillerPuzzle? result = completedTask.Result;
        if (result != null)
        {
            int[] initial = new int[81];
            foreach (Cage cage in result.Cages)
            {
                if (cage.Size == 1)
                {
                    initial[cage.Cells[0]] = cage.Sum;
                }
            }
            Initial = initial;
            Solution = result.Solution;
            Cages = result.Cages;
            Strategies = result.Strategies;
            Difficulty = result.Difficulty != QQWingLib.Difficulty.UNKNOWN
                ? $"Killer {result.Difficulty}" : "Killer";
        }

        await Task.WhenAll(tasks);

        Mouse.OverrideCursor = Cursors.Arrow;

        if (result == null && !token.IsCancellationRequested)
        {
            CustomMessageBox.Show("Could not generate a Killer puzzle." +
                Environment.NewLine + "Please try again.", "Sudoku", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static KillerPuzzle? GenerateKillerInternal(Difficulty difficulty, Symmetry symmetry, CancellationToken token)
    {
        try
        {
            KillerGenerator generator = new() { Symmetry = symmetry };
            return generator.Generate(difficulty, token);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Killer GenerateInternal was canceled");
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Debug.WriteLine("Error generating Killer puzzle: " + e.Message);
        }

        return null;
    }

    public async Task GenerateEvenOdd(Difficulty difficulty, Symmetry symmetry, bool? isEven = null)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        using CancellationTokenSource cancellationTokenSource = new();
        var token = cancellationTokenSource.Token;
        List<Task<EvenOddPuzzle?>> tasks = [];
        for (int idx = 0; idx < 8; idx++)
        {
            tasks.Add(Task.Run(() => GenerateEvenOddInternal(difficulty, symmetry, isEven, token), token));
        }

        Task<EvenOddPuzzle?> completedTask = await WaitWithCancelDialog(
            Task.WhenAny(tasks), cancellationTokenSource,
            "Generating Even/Odd puzzle\u2026 this is taking longer than expected.");

        cancellationTokenSource.Cancel();

        EvenOddPuzzle? result = completedTask.Result;
        if (result != null)
        {
            Initial = result.Initial;
            Solution = result.Solution;
            Difficulty = result.Difficulty;
            Strategies = result.Strategies;
            EvenOddConstraint = result.Constraint;
        }

        await Task.WhenAll(tasks);

        Mouse.OverrideCursor = Cursors.Arrow;

        if (result == null && !token.IsCancellationRequested)
        {
            CustomMessageBox.Show("Could not generate an Even/Odd puzzle." +
                Environment.NewLine + "Please try again.", "Sudoku", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static EvenOddPuzzle? GenerateEvenOddInternal(Difficulty difficulty, Symmetry symmetry, bool? isEven, CancellationToken token)
    {
        try
        {
            QQWing ss = new();
            ss.SetPrintStyle(PrintStyle.ONE_LINE);

            var result = ss.GenerateEvenOddPuzzle(difficulty, symmetry, isEven, token);
            if (result == null) return null;

            var (puzzleArr, solutionArr, constraint) = result.Value;

            // Re-solve with history to get difficulty/strategies
            QQWing solver = new();
            solver.SetRecordHistory(true);
            solver.SetEvenOddConstraint(constraint.ShadedCells, constraint.IsEven);
            solver.SetPuzzle(puzzleArr);
            solver.Solve(token);

            string diff = solver.GetDifficultyAsString();
            List<string> strategies = solver.GetStrategiesUsed();

            return new EvenOddPuzzle(puzzleArr, solutionArr, diff, strategies, constraint);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("EvenOdd GenerateInternal was canceled");
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Debug.WriteLine("Error generating Even/Odd puzzle: " + e.Message);
        }

        return null;
    }

    public EvenOddConstraint? EvenOddConstraint { get; private set; }
}

public class EvenOddPuzzle(int[] initial, int[] solution, string difficulty, List<string> strategies, EvenOddConstraint constraint)
{
    public int[] Initial { get; } = initial;
    public int[] Solution { get; } = solution;
    public string Difficulty { get; } = difficulty;
    public List<string> Strategies { get; } = strategies;
    public EvenOddConstraint Constraint { get; } = constraint;
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