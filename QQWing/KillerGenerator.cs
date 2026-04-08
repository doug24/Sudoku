using System;
using System.Collections.Generic;
using System.Threading;

namespace QQWingLib;

/// <summary>
/// Generates Killer Sudoku puzzles. Produces a random complete Sudoku grid,
/// partitions it into cages with target sums, and verifies the cage layout
/// yields a unique solution.
/// </summary>
public class KillerGenerator
{
    private const int BOARD_SIZE = 81;
    private const int SIZE = 9;

    private static readonly Random random = new();

    /// <summary>
    /// Minimum number of cells per cage (inclusive).
    /// </summary>
    public int MinCageSize { get; set; } = 2;

    /// <summary>
    /// Maximum number of cells per cage (inclusive).
    /// </summary>
    public int MaxCageSize { get; set; } = 5;

    /// <summary>
    /// Maximum number of cage-partition attempts per generated grid before
    /// generating a new grid. Each attempt produces a different random
    /// partition and checks uniqueness.
    /// </summary>
    public int MaxPartitionAttempts { get; set; } = 20;

    /// <summary>
    /// Generate a Killer Sudoku puzzle. Returns the cage list and the solution,
    /// or null if generation could not produce a unique puzzle before cancellation.
    /// </summary>
    public KillerPuzzle Generate(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            token.ThrowIfCancellationRequested();

            // Step 1: generate a random complete Sudoku grid using QQWing
            int[] solution = GenerateFullGrid(token);
            if (solution == null)
                continue;

            // Step 2: try random cage partitions on this grid
            for (int attempt = 0; attempt < MaxPartitionAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();

                List<Cage> cages = PartitionIntoCages(solution);

                // Step 3: verify the cage layout produces a unique solution
                KillerSolver solver = new(cages);
                if (solver.HasUniqueSolution())
                {
                    return new KillerPuzzle(cages, solution);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Use QQWing to solve an empty grid with randomized algorithms, producing
    /// a random valid Sudoku solution.
    /// </summary>
    private static int[] GenerateFullGrid(CancellationToken token)
    {
        QQWing qq = new();
        qq.SetRecordHistory(false);
        qq.SetLogHistory(false);
        qq.SetPuzzle(new int[BOARD_SIZE]);
        if (qq.Solve(token) && qq.IsSolved())
        {
            return qq.GetSolution();
        }
        return null;
    }

    /// <summary>
    /// Partition all 81 cells into contiguous cages and compute each cage's
    /// sum from the solution. Uses a randomized flood-fill: pick a random
    /// unassigned cell as a seed, then grow the cage by repeatedly adding a
    /// random orthogonally adjacent unassigned neighbor until the target
    /// size is reached or no neighbors remain.
    /// </summary>
    private List<Cage> PartitionIntoCages(int[] solution)
    {
        int[] cageId = new int[BOARD_SIZE];
        Array.Fill(cageId, -1);

        List<Cage> cages = [];

        // Build a shuffled list of cells to seed cages
        int[] order = new int[BOARD_SIZE];
        for (int i = 0; i < BOARD_SIZE; i++) order[i] = i;
        Shuffle(order);

        foreach (int seed in order)
        {
            if (cageId[seed] != -1)
                continue;

            int targetSize = random.Next(MinCageSize, MaxCageSize + 1);

            // Grow the cage from the seed cell
            List<int> cells = [seed];
            cageId[seed] = cages.Count;

            // Frontier: unassigned orthogonal neighbors of cells already in this cage
            List<int> frontier = [];
            AddNeighbours(seed, cageId, frontier);

            while (cells.Count < targetSize && frontier.Count > 0)
            {
                int idx = random.Next(frontier.Count);
                int next = frontier[idx];
                frontier.RemoveAt(idx);

                if (cageId[next] != -1)
                    continue;

                cells.Add(next);
                cageId[next] = cages.Count;
                AddNeighbours(next, cageId, frontier);
            }

            int sum = 0;
            foreach (int cell in cells)
                sum += solution[cell];

            cages.Add(new Cage([.. cells], sum));
        }

        return cages;
    }

    /// <summary>
    /// Add orthogonal neighbors of <paramref name="cell"/> that are not yet
    /// assigned to a cage to the frontier list.
    /// </summary>
    private static void AddNeighbours(int cell, int[] cageId, List<int> frontier)
    {
        int row = cell / SIZE;
        int col = cell % SIZE;

        if (row > 0) TryAdd(cell - SIZE, cageId, frontier);
        if (row < SIZE - 1) TryAdd(cell + SIZE, cageId, frontier);
        if (col > 0) TryAdd(cell - 1, cageId, frontier);
        if (col < SIZE - 1) TryAdd(cell + 1, cageId, frontier);
    }

    private static void TryAdd(int cell, int[] cageId, List<int> frontier)
    {
        if (cageId[cell] == -1 && !frontier.Contains(cell))
            frontier.Add(cell);
    }

    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}

/// <summary>
/// The result of a successful Killer Sudoku generation: the cage definitions
/// and the underlying solution.
/// </summary>
public class KillerPuzzle(List<Cage> cages, int[] solution)
{
    /// <summary>
    /// The cages that define the puzzle. Every cell 0-80 belongs to exactly one cage.
    /// </summary>
    public List<Cage> Cages { get; } = cages;

    /// <summary>
    /// The unique solution (values 1-9 for all 81 cells).
    /// </summary>
    public int[] Solution { get; } = solution;
}
