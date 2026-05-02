/*
 * qqwing - Sudoku solver and generator
 * Copyright (C) 2026 Doug Persons: add Killer Sudoku solver and generator
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
    /// The symmetry to apply to the first <see cref="SymmetricCageCount"/> cage pairs.
    /// Supported values: NONE, ROTATE180, MIRROR, FLIP. RANDOM picks one of
    /// the three at random. ROTATE90 falls back to ROTATE180.
    /// </summary>
    public Symmetry Symmetry { get; set; } = Symmetry.NONE;

    /// <summary>
    /// Number of cage pairs to create symmetrically before falling back to
    /// random flood-fill for the remaining cells.
    /// </summary>
    public int SymmetricCageCount { get; set; } = 4;

    /// <summary>
    /// Number of cages to bias toward extreme (very low or very high) sums.
    /// Half target low sums and half target high sums. These cages are seeded
    /// from cells with extreme digit values and grown by preferring neighbors
    /// whose digits reinforce the bias.
    /// </summary>
    public int ExtremeCageCount { get; set; } = 4;

    /// <summary>
    /// Maximum number of single-cell cages allowed in a valid puzzle.
    /// Partitions that exceed this limit are discarded. Default is 5.
    /// </summary>
    public int MaxSingleCellCages { get; set; } = 5;

    /// <summary>
    /// Generate a Killer Sudoku puzzle. Returns the cage list and the solution,
    /// or null if generation could not produce a unique puzzle before cancellation.
    /// </summary>
    /// <param name="targetDifficulty">
    /// The desired difficulty level. Pass <see cref="Difficulty.UNKNOWN"/> to accept any difficulty.
    /// </param>
    public KillerPuzzle Generate(Difficulty targetDifficulty, CancellationToken token)
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

                // Discard if too many single-cell cages
                int singleCellCount = 0;
                foreach (Cage cage in cages)
                    if (cage.Size == 1) singleCellCount++;
                if (singleCellCount > MaxSingleCellCages)
                    continue;

                // Step 3: verify the cage layout produces a unique solution
                KillerSolver solver = new(cages);
                if (solver.HasUniqueSolution())
                {
                    Difficulty difficulty = solver.GetDifficulty();
                    List<string> strategies = solver.GetStrategiesUsed();

                    // Step 4: check difficulty matches the target
                    if (targetDifficulty != Difficulty.UNKNOWN && difficulty != targetDifficulty)
                        continue;

                    return new KillerPuzzle(cages, solution, difficulty, strategies);
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
    /// sum from the solution. When <see cref="Symmetry"/> is set, the first
    /// <see cref="SymmetricCageCount"/> cage pairs are placed symmetrically,
    /// then remaining cells are filled with random flood-fill.
    /// </summary>
    private List<Cage> PartitionIntoCages(int[] solution)
    {
        int[] cageId = new int[BOARD_SIZE];
        Array.Fill(cageId, -1);

        List<Cage> cages = [];

        Symmetry effectiveSymmetry = ResolveSymmetry();
        if (effectiveSymmetry != Symmetry.NONE && SymmetricCageCount > 0)
        {
            PlaceSymmetricCages(solution, cageId, cages, effectiveSymmetry);
        }

        // Place a few cages biased toward extreme (low/high) sums
        if (ExtremeCageCount > 0)
        {
            PlaceExtremeCages(solution, cageId, cages);
        }

        // Fill remaining unassigned cells with random flood-fill
        int[] order = new int[BOARD_SIZE];
        for (int i = 0; i < BOARD_SIZE; i++) order[i] = i;
        Shuffle(order);

        foreach (int seed in order)
        {
            if (cageId[seed] != -1)
                continue;

            GrowCage(seed, solution, cageId, cages);
        }

        return cages;
    }

    /// <summary>
    /// Place up to <see cref="SymmetricCageCount"/> symmetric cage pairs.
    /// For each pair, a cage is grown from a random seed in the primary half,
    /// then its mirror is created at the symmetric positions.
    /// </summary>
    private void PlaceSymmetricCages(int[] solution, int[] cageId, List<Cage> cages, Symmetry symmetry)
    {
        // Build a shuffled list of cells in the primary half
        List<int> seeds = [];
        for (int cell = 0; cell < BOARD_SIZE; cell++)
        {
            if (IsInPrimaryHalf(cell, symmetry))
                seeds.Add(cell);
        }
        ShuffleList(seeds);

        int placed = 0;
        foreach (int seed in seeds)
        {
            if (placed >= SymmetricCageCount)
                break;

            if (cageId[seed] != -1)
                continue;

            int mirrorSeed = MirrorCell(seed, symmetry);
            if (mirrorSeed == seed || cageId[mirrorSeed] != -1)
                continue;

            // Try to grow a symmetric cage pair
            if (TryGrowSymmetricCage(seed, mirrorSeed, solution, cageId, cages, symmetry))
                placed++;
        }
    }

    /// <summary>
    /// Grow a cage from <paramref name="seed"/> and simultaneously build its
    /// mirror cage. Each cell added to the primary cage must have its mirror
    /// cell unassigned, and both cages must satisfy the no-duplicate-digit
    /// constraint. Returns false if only the seed pair could be placed and the
    /// cage would be too small (single cell).
    /// </summary>
    private bool TryGrowSymmetricCage(int seed, int mirrorSeed, int[] solution,
        int[] cageId, List<Cage> cages, Symmetry symmetry)
    {
        int targetSize = random.Next(MinCageSize, MaxCageSize + 1);

        List<int> cells = [seed];
        int primaryId = cages.Count;
        int mirrorId = cages.Count + 1;

        // Temporarily mark both seeds
        cageId[seed] = primaryId;
        cageId[mirrorSeed] = mirrorId;

        int usedDigitsMask = 1 << (solution[seed] - 1);
        int mirrorDigitsMask = 1 << (solution[mirrorSeed] - 1);

        List<int> mirrorCells = [mirrorSeed];

        List<int> frontier = [];
        AddNeighbours(seed, cageId, frontier);

        while (cells.Count < targetSize && frontier.Count > 0)
        {
            int idx = random.Next(frontier.Count);
            int next = frontier[idx];
            frontier.RemoveAt(idx);

            if (cageId[next] != -1)
                continue;

            int mirror = MirrorCell(next, symmetry);
            if (mirror == next || cageId[mirror] != -1)
                continue;

            // Check digit constraints for both cages
            int digitBit = 1 << (solution[next] - 1);
            if ((usedDigitsMask & digitBit) != 0)
                continue;

            int mirrorDigitBit = 1 << (solution[mirror] - 1);
            if ((mirrorDigitsMask & mirrorDigitBit) != 0)
                continue;

            cells.Add(next);
            cageId[next] = primaryId;
            usedDigitsMask |= digitBit;
            AddNeighbours(next, cageId, frontier);

            mirrorCells.Add(mirror);
            cageId[mirror] = mirrorId;
            mirrorDigitsMask |= mirrorDigitBit;
        }

        if (cells.Count < MinCageSize)
        {
            // Roll back: cage is too small
            foreach (int c in cells) cageId[c] = -1;
            foreach (int c in mirrorCells) cageId[c] = -1;
            return false;
        }

        int sum = 0;
        foreach (int c in cells) sum += solution[c];
        cages.Add(new Cage([.. cells], sum));

        int mirrorSum = 0;
        foreach (int c in mirrorCells) mirrorSum += solution[c];
        cages.Add(new Cage([.. mirrorCells], mirrorSum));

        return true;
    }

    /// <summary>
    /// Place up to <see cref="ExtremeCageCount"/> cages biased toward extreme sums.
    /// Half target low sums (seeded from cells with digits 1-2, grown toward low
    /// digits) and half target high sums (seeded from digits 8-9, grown toward
    /// high digits).
    /// </summary>
    private void PlaceExtremeCages(int[] solution, int[] cageId, List<Cage> cages)
    {
        int lowTarget = ExtremeCageCount / 2;
        int highTarget = ExtremeCageCount - lowTarget;

        List<int> lowSeeds = [];
        List<int> highSeeds = [];
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            if (solution[i] <= 2) lowSeeds.Add(i);
            else if (solution[i] >= 8) highSeeds.Add(i);
        }
        ShuffleList(lowSeeds);
        ShuffleList(highSeeds);

        int placed = 0;
        foreach (int seed in lowSeeds)
        {
            if (placed >= lowTarget) break;
            if (cageId[seed] != -1) continue;
            GrowCage(seed, solution, cageId, cages, SumBias.Low);
            placed++;
        }

        placed = 0;
        foreach (int seed in highSeeds)
        {
            if (placed >= highTarget) break;
            if (cageId[seed] != -1) continue;
            GrowCage(seed, solution, cageId, cages, SumBias.High);
            placed++;
        }
    }

    /// <summary>
    /// Grow a single cage from the seed cell using flood-fill. When
    /// <paramref name="bias"/> is not <see cref="SumBias.None"/>, frontier
    /// cells with the lowest or highest digit are preferred, producing cages
    /// with extreme sums.
    /// </summary>
    private void GrowCage(int seed, int[] solution, int[] cageId, List<Cage> cages,
        SumBias bias = SumBias.None)
    {
        int targetSize = random.Next(MinCageSize, MaxCageSize + 1);

        // Grow the cage from the seed cell
        List<int> cells = [seed];
        cageId[seed] = cages.Count;
        int usedDigitsMask = 1 << (solution[seed] - 1);

        // Frontier: unassigned orthogonal neighbors of cells already in this cage
        List<int> frontier = [];
        AddNeighbours(seed, cageId, frontier);

        while (cells.Count < targetSize && frontier.Count > 0)
        {
            int idx = bias != SumBias.None
                ? SelectBiasedIndex(frontier, solution, bias)
                : random.Next(frontier.Count);

            int next = frontier[idx];
            frontier.RemoveAt(idx);

            if (cageId[next] != -1)
                continue;

            // Killer convention: a cage cannot contain the same digit twice
            int digitBit = 1 << (solution[next] - 1);
            if ((usedDigitsMask & digitBit) != 0)
                continue;

            cells.Add(next);
            cageId[next] = cages.Count;
            usedDigitsMask |= digitBit;
            AddNeighbours(next, cageId, frontier);
        }

        int sum = 0;
        foreach (int cell in cells)
            sum += solution[cell];

        cages.Add(new Cage([.. cells], sum));
    }

    /// <summary>
    /// Return the index in <paramref name="frontier"/> of the cell with the
    /// lowest (for <see cref="SumBias.Low"/>) or highest (for
    /// <see cref="SumBias.High"/>) digit value.
    /// </summary>
    private static int SelectBiasedIndex(List<int> frontier, int[] solution, SumBias bias)
    {
        int bestIdx = 0;
        int bestDigit = solution[frontier[0]];

        for (int i = 1; i < frontier.Count; i++)
        {
            int digit = solution[frontier[i]];
            if ((bias == SumBias.Low && digit < bestDigit) ||
                (bias == SumBias.High && digit > bestDigit))
            {
                bestDigit = digit;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    /// <summary>
    /// Resolve RANDOM and ROTATE90 to a concrete supported symmetry.
    /// </summary>
    private Symmetry ResolveSymmetry()
    {
        Symmetry[] supported = [Symmetry.ROTATE180, Symmetry.MIRROR, Symmetry.FLIP];

        if (Symmetry == Symmetry.RANDOM || Symmetry == Symmetry.ROTATE90)
            return supported[random.Next(supported.Length)];

        return Symmetry;
    }

    /// <summary>
    /// Returns true if <paramref name="cell"/> is in the primary half of the
    /// board for the given symmetry. The primary half excludes the axis of
    /// symmetry so that seed and mirror are always distinct.
    /// </summary>
    private static bool IsInPrimaryHalf(int cell, Symmetry symmetry)
    {
        int row = cell / SIZE;
        int col = cell % SIZE;

        return symmetry switch
        {
            Symmetry.ROTATE180 => cell < BOARD_SIZE / 2, // cells 0-39 (excludes center cell 40)
            Symmetry.MIRROR => col < SIZE / 2,           // columns 0-3 (excludes center column 4)
            Symmetry.FLIP => row < SIZE / 2,             // rows 0-3 (excludes center row 4)
            _ => false,
        };
    }

    /// <summary>
    /// Return the symmetric counterpart of <paramref name="cell"/>.
    /// </summary>
    private static int MirrorCell(int cell, Symmetry symmetry)
    {
        int row = cell / SIZE;
        int col = cell % SIZE;

        return symmetry switch
        {
            Symmetry.ROTATE180 => (SIZE - 1 - row) * SIZE + (SIZE - 1 - col),
            Symmetry.MIRROR => row * SIZE + (SIZE - 1 - col),
            Symmetry.FLIP => (SIZE - 1 - row) * SIZE + col,
            _ => cell,
        };
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

    private static void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private enum SumBias { None, Low, High }
}

/// <summary>
/// The result of a successful Killer Sudoku generation: the cage definitions
/// and the underlying solution.
/// </summary>
public class KillerPuzzle(List<Cage> cages, int[] solution, Difficulty difficulty = Difficulty.UNKNOWN, List<string> strategies = null)
{
    /// <summary>
    /// The cages that define the puzzle. Every cell 0-80 belongs to exactly one cage.
    /// </summary>
    public List<Cage> Cages { get; } = cages;

    /// <summary>
    /// The unique solution (values 1-9 for all 81 cells).
    /// </summary>
    public int[] Solution { get; } = solution;

    /// <summary>
    /// The assessed difficulty of the puzzle.
    /// </summary>
    public Difficulty Difficulty { get; } = difficulty;

    /// <summary>
    /// The distinct strategy names used to solve the puzzle, in the order they were first used.
    /// </summary>
    public List<string> Strategies { get; } = strategies ?? [];
}
