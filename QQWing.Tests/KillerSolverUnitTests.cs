using QQWingLib;

namespace QQWingTest;

[TestClass]
public class KillerSolverUnitTests
{
    private static readonly int[] TestSolution =
    [
        2,1,5, 6,4,7, 3,9,8,
        3,6,8, 9,5,2, 1,7,4,
        7,9,4, 3,8,1, 6,5,2,
        5,8,6, 2,7,4, 9,3,1,
        1,4,2, 5,9,3, 8,6,7,
        9,7,3, 8,1,6, 4,2,5,
        8,2,1, 7,3,9, 5,4,6,
        6,5,9, 4,2,8, 7,1,3,
        4,3,7, 1,6,5, 2,8,9,
    ];

    private static readonly string Test28CagesData =
        """
        17 0,0 0,1 1,1
        15 0,2 1,2 1,3
        14 0,3 0,4
        16 0,5 0,6 1,6
        7 0,7 0,8
        10 1,0 2,0 3,0
        11 1,4 1,5 2,5
        13 1,7 2,7
        9 1,8 2,8
        14 2,1 2,2 3,1
        8 2,3 2,4
        27 2,6 3,6 4,5 4,6 5,6 6,6
        32 3,2 3,3 4,2 4,3 4,4 5,2 5,3
        8 3,4 3,5
        31 3,7 3,8 4,7 4,8 5,7 5,8
        12 4,0 4,1
        22 5,0 6,0 7,0
        16 5,1 6,1 6,2
        16 5,4 5,5
        9 6,3 6,4
        21 6,5 7,4 7,5
        5 6,7 7,7
        14 6,8 7,8
        11 7,1 8,0 8,1
        9 7,2 7,3 8,2
        15 7,6 8,5 8,6
        12 8,3 8,4
        11 8,7 8,8
        """;

    private static readonly int[] Test28CagesSolution =
    [
        7,1,4,6,8,9,3,5,2,
        6,9,8,3,2,5,4,7,1,
        3,2,5,7,1,4,9,6,8,
        1,7,9,4,6,2,5,8,3,
        4,8,2,1,5,3,7,9,6,
        5,6,3,8,9,7,2,1,4,
        8,3,7,5,4,6,1,2,9,
        9,4,1,2,7,8,6,3,5,
        2,5,6,9,3,1,8,4,7,
    ];

    private static readonly string Test23CagesData =
        """
        17 0,0 1,0
        8 0,1 1,1
        30 0,2 0,3 0,4 0,5 0,6
        13 0,7 0,8 1,7 1,8
        19 1,2 1,3 2,2
        4 1,4 1,5
        41 1,6 2,4 2,5 2,6 2,7 3,6 4,6
        9 2,0 3,0 2,1
        12 2,3 3,1 3,2 3,3
        26 2,8 3,8 4,8 5,8 6,8
        26 3,4 3,5 4,4 4,5
        11 3,7 4,7
        19 4,0 4,1 5,0 6,0
        19 4,2 5,1 5,2
        14 4,3 5,3 5,4
        18 5,5 5,6 6,5 7,5
        13 5,7 6,7 6,6
        32 6,1 6,2 7,0 7,1 7,2 8,0 8,1
        23 6,3 6,4 7,3
        18 7,4 8,2 8,3 8,4
        11 7,6 8,6 8,5
        11 7,7 7,8
        11 8,7 8,8
        """;

    private static readonly int[] Test23CagesSolution =
    [
        9,2,4,7,8,5,6,3,1,
        8,6,7,9,3,1,2,5,4,
        1,5,3,2,4,6,7,9,8,
        3,4,5,1,2,9,8,7,6,
        6,1,9,3,7,8,5,4,2,
        7,8,2,5,6,4,9,1,3,
        5,3,1,6,9,2,4,8,7,
        4,7,6,8,5,3,1,2,9,
        2,9,8,4,1,7,3,6,5,
    ];


    /// <summary>
    /// Build cages from a known solution and verify the solver finds a valid solution
    /// that satisfies all Sudoku rules and all cage constraints.
    /// </summary>
    [TestMethod]
    public void Solve_WithCages_FindsValidSolution()
    {
        // Create cages: 2-cell horizontal pairs + single-cell for last column
        List<Cage> cages = BuildHorizontalPairCages(TestSolution);

        KillerSolver solver = new(cages);
        int[] result = solver.Solve();

        Assert.IsNotNull(result, "Solver should find a solution.");
        AssertValidSudoku(result);
        AssertCagesSatisfied(result, cages);
    }

    /// <summary>
    /// Single-cell cages are equivalent to givens: the puzzle is fully determined
    /// and must have exactly one solution.
    /// </summary>
    [TestMethod]
    public void HasUniqueSolution_AllSingleCellCages_ReturnsTrue()
    {
        List<Cage> cages = [];
        for (int cell = 0; cell < 81; cell++)
        {
            cages.Add(new Cage([cell], TestSolution[cell]));
        }

        KillerSolver solver = new(cages);
        Assert.IsTrue(solver.HasUniqueSolution());
    }

    /// <summary>
    /// Cages that cross row boundaries (vertical 2-cell pairs) exercise the
    /// inter-row constraint propagation in the solver.
    /// </summary>
    [TestMethod]
    public void Solve_WithVerticalCages_FindsValidSolution()
    {
        // Vertical 2-cell cages down each column (rows 0-1, 2-3, 4-5, 6-7)
        // plus single-cell cages for row 8.
        List<Cage> cages = [];
        for (int col = 0; col < 9; col++)
        {
            for (int row = 0; row < 8; row += 2)
            {
                int c1 = row * 9 + col;
                int c2 = (row + 1) * 9 + col;
                cages.Add(new Cage([c1, c2], TestSolution[c1] + TestSolution[c2]));
            }
            // Last row: single-cell cage
            int last = 8 * 9 + col;
            cages.Add(new Cage([last], TestSolution[last]));
        }

        KillerSolver solver = new(cages);
        int[] result = solver.Solve();

        Assert.IsNotNull(result, "Solver should find a solution with vertical cages.");
        AssertValidSudoku(result);
        AssertCagesSatisfied(result, cages);
    }

    [TestMethod]
    public void CountSolutions_ImpossibleCage_ReturnsZero()
    {
        // A cage with sum 1 spanning two cells is impossible (min sum of 2 distinct digits = 1+2=3)
        List<Cage> cages =
        [
            new Cage([0, 1], 1),
        ];

        KillerSolver solver = new(cages);
        Assert.AreEqual(0, solver.CountSolutions(1));
    }

    [TestMethod]
    public void Constructor_DuplicateCell_ThrowsArgumentException()
    {
        List<Cage> cages =
        [
            new Cage([0, 1], 5),
            new Cage([1, 2], 8),  // cell 1 overlaps
        ];

        Assert.ThrowsExactly<ArgumentException>(() => new KillerSolver(cages));
    }

    [TestMethod]
    public void Constructor_CellOutOfRange_ThrowsArgumentException()
    {
        List<Cage> cages =
        [
            new Cage([81], 5),
        ];

        Assert.ThrowsExactly<ArgumentException>(() => new KillerSolver(cages));
    }

    [TestMethod]
    public void Cage_InvalidSize_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Cage([], 5));
    }

    [TestMethod]
    public void Cage_InvalidSum_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Cage([0], 0));
        Assert.ThrowsExactly<ArgumentException>(() => new Cage([0], 46));
    }

    [TestMethod]
    public void Cage_Properties_ReturnCorrectValues()
    {
        Cage cage = new([0, 1, 2], 10);
        Assert.AreEqual(10, cage.Sum);
        Assert.AreEqual(3, cage.Size);
        CollectionAssert.AreEqual(new int[] { 0, 1, 2 }, cage.Cells);
    }

    [TestMethod]
    public void ParseKillerData_ValidInput_ParsesCages()
    {
        string data = """
            3 0,0 0,1
            15 0,2 1,2
            9 8,8
            """;

        List<Cage> cages = Util.ParseKillerData(data);

        Assert.HasCount(3, cages);

        // Cage 0: sum=3, cells R0C0 and R0C1 => indices 0, 1
        Assert.AreEqual(3, cages[0].Sum);
        CollectionAssert.AreEqual(new int[] { 0, 1 }, cages[0].Cells);

        // Cage 1: sum=15, cells R0C2 and R1C2 => indices 2, 11
        Assert.AreEqual(15, cages[1].Sum);
        CollectionAssert.AreEqual(new int[] { 2, 11 }, cages[1].Cells);

        // Cage 2: sum=9, cell R8C8 => index 80
        Assert.AreEqual(9, cages[2].Sum);
        CollectionAssert.AreEqual(new int[] { 80 }, cages[2].Cells);
    }

    [TestMethod]
    public void ParseKillerData_IgnoresBlankLinesAndComments()
    {
        string data = """
            # This is a comment
            3 0,0 0,1

            15 0,2 1,2
            # Another comment
            """;

        List<Cage> cages = Util.ParseKillerData(data);
        Assert.HasCount(2, cages);
    }

    [TestMethod]
    public void ParseKillerData_InvalidSum_ThrowsArgumentException()
    {
        string data = "abc 1,1 1,2";
        Assert.ThrowsExactly<ArgumentException>(() => Util.ParseKillerData(data));
    }

    [TestMethod]
    public void ParseKillerData_InvalidRowCol_ThrowsArgumentException()
    {
        string data = "3 0,0 x,1";
        Assert.ThrowsExactly<ArgumentException>(() => Util.ParseKillerData(data));
    }

    [TestMethod]
    public void ParseKillerData_RowColOutOfRange_ThrowsArgumentException()
    {
        string data = "3 -1,0 0,1";
        Assert.ThrowsExactly<ArgumentException>(() => Util.ParseKillerData(data));
    }

    [TestMethod]
    public void ParseKillerData_MissingCells_ThrowsArgumentException()
    {
        string data = "3";
        Assert.ThrowsExactly<ArgumentException>(() => Util.ParseKillerData(data));
    }

    [TestMethod]
    public void ParseKillerData_EmptyInput_ReturnsEmptyList()
    {
        List<Cage> cages = Util.ParseKillerData("");
        Assert.IsEmpty(cages);
    }

    [TestMethod]
    public void ParseKillerData_Full9x9()
    {
        List<Cage> cages = Util.ParseKillerData(Test28CagesData);

        Assert.HasCount(28, cages);

        // Cage 0: sum=17, cells R0C0, R0C1, R1,1 => indices 0, 1, 10
        Assert.AreEqual(17, cages[0].Sum);
        CollectionAssert.AreEqual(new int[] { 0, 1, 10 }, cages[0].Cells);

        // Cage 1: sum=15, cells R0C2, R1C2, and R1C3 => indices 2, 11, 12
        Assert.AreEqual(15, cages[1].Sum);
        CollectionAssert.AreEqual(new int[] { 2, 11, 12 }, cages[1].Cells);

        // Cage 28: sum=11, cells R8C7 and R8C8 => index 79, 80
        Assert.AreEqual(11, cages[27].Sum);
        CollectionAssert.AreEqual(new int[] { 79, 80 }, cages[27].Cells);
    }

    [TestMethod]
    public void Solve_With28Cages_FindsValidSolution()
    {
        List<Cage> cages = Util.ParseKillerData(Test28CagesData);

        KillerSolver solver = new(cages);
        int[] result = solver.Solve();

        Assert.IsNotNull(result, "Solver should find a solution for the 28-cage puzzle.");
        AssertValidSudoku(result);
        AssertCagesSatisfied(result, cages);
        CollectionAssert.AreEqual(Test28CagesSolution, result, "Solver should find the known solution for the 28-cage puzzle.");
    }

    [TestMethod]
    public void Solve_With23Cages_FindsValidSolution()
    {
        List<Cage> cages = Util.ParseKillerData(Test23CagesData);

        KillerSolver solver = new(cages);
        int[] result = solver.Solve();

        Assert.IsNotNull(result, "Solver should find a solution for the 23-cage puzzle.");
        AssertValidSudoku(result);
        AssertCagesSatisfied(result, cages);
        CollectionAssert.AreEqual(Test23CagesSolution, result, "Solver should find the known solution for the 23-cage puzzle.");
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public void Generate_ProducesValidUniquePuzzle()
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(25));

        KillerGenerator generator = new();
        KillerPuzzle puzzle = generator.Generate(Difficulty.UNKNOWN, cts.Token);

        Assert.IsNotNull(puzzle, "Generator should produce a puzzle.");
        Assert.IsNotEmpty(puzzle.Cages, "Puzzle should have at least one cage.");
        AssertValidSudoku(puzzle.Solution);
        AssertCagesSatisfied(puzzle.Solution, puzzle.Cages);
        AssertFullCoverage(puzzle.Cages);

        // Verify uniqueness independently
        KillerSolver solver = new(puzzle.Cages);
        Assert.IsTrue(solver.HasUniqueSolution(), "Generated puzzle should have a unique solution.");
    }

    [TestMethod]
    public void GetDifficulty_AllSingleCellCages_ReturnsSimple()
    {
        // Single-cell cages are like givens: solved by naked singles alone
        List<Cage> cages = [];
        for (int cell = 0; cell < 81; cell++)
        {
            cages.Add(new Cage([cell], TestSolution[cell]));
        }

        KillerSolver solver = new(cages);
        Assert.AreEqual(Difficulty.SIMPLE, solver.GetDifficulty());
    }

    [TestMethod]
    public void GetDifficulty_28CagePuzzle_ReturnsKnownDifficulty()
    {
        List<Cage> cages = Util.ParseKillerData(Test28CagesData);
        KillerSolver solver = new(cages);

        Difficulty difficulty = solver.GetDifficulty();

        Assert.AreNotEqual(Difficulty.UNKNOWN, difficulty, "A solvable puzzle should have a known difficulty.");
    }

    [TestMethod]
    public void GetDifficulty_23CagePuzzle_ReturnsKnownDifficulty()
    {
        List<Cage> cages = Util.ParseKillerData(Test23CagesData);
        KillerSolver solver = new(cages);

        Difficulty difficulty = solver.GetDifficulty();

        Assert.AreNotEqual(Difficulty.UNKNOWN, difficulty, "A solvable puzzle should have a known difficulty.");
    }

    [TestMethod]
    public void GetDifficulty_ImpossiblePuzzle_ReturnsUnknown()
    {
        // A cage with sum 1 spanning two cells is impossible
        List<Cage> cages = [new Cage([0, 1], 1)];

        KillerSolver solver = new(cages);
        Assert.AreEqual(Difficulty.UNKNOWN, solver.GetDifficulty());
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public void Generate_PuzzleHasDifficulty()
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(25));

        KillerGenerator generator = new();
        KillerPuzzle puzzle = generator.Generate(Difficulty.UNKNOWN, cts.Token);

        Assert.IsNotNull(puzzle, "Generator should produce a puzzle.");
        Assert.AreNotEqual(Difficulty.UNKNOWN, puzzle.Difficulty,
            "Generated puzzle should have an assessed difficulty.");
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public void Generate_CageSizesWithinBounds()
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(25));

        KillerGenerator generator = new() { MinCageSize = 2, MaxCageSize = 4 };
        KillerPuzzle puzzle = generator.Generate(Difficulty.UNKNOWN, cts.Token);

        Assert.IsNotNull(puzzle);
        foreach (Cage cage in puzzle.Cages)
        {
            Assert.IsTrue(cage.Size >= 1 && cage.Size <= 4,
                $"Cage has {cage.Size} cells, expected 1-4.");
        }
    }

    /// <summary>
    /// Verify that greedy graph-coloring assigns different colors to
    /// adjacent cages, including single-cell cages next to multi-cell cages.
    /// </summary>
    [TestMethod]
    public void GraphColoring_SingleCellAdjacentToMultiCell_DifferentColors()
    {
        // Layout: a single-cell cage at cell 0, a 4-cell cage at cells 1,2,9,10
        // Cell 0 (row 0, col 0) is orthogonally adjacent to cell 1 (row 0, col 1)
        // and cell 9 (row 1, col 0), so the cages share edges.
        List<Cage> cages =
        [
            new Cage([0], TestSolution[0]),                              // single cell
            new Cage([1, 2, 9, 10],                                     // 4-cell block
                TestSolution[1] + TestSolution[2] + TestSolution[9] + TestSolution[10]),
        ];

        // Fill remaining cells as single-cell cages to make a complete partition
        HashSet<int> used = [0, 1, 2, 9, 10];
        for (int cell = 0; cell < 81; cell++)
        {
            if (!used.Contains(cell))
                cages.Add(new Cage([cell], TestSolution[cell]));
        }

        int[] colors = ComputeCageColors(cages);

        // Cage 0 (single cell at 0) and cage 1 (4-cell block containing cell 1)
        // are orthogonally adjacent — they must have different colors
        Assert.AreNotEqual(colors[0], colors[1],
            "Single-cell cage and adjacent 4-cell cage must have different colors.");
    }

    /// <summary>
    /// Generate a puzzle and verify graph-coloring produces no adjacent
    /// same-color cages (orthogonal or diagonal).
    /// </summary>
    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public void GraphColoring_GeneratedPuzzle_NoAdjacentSameColor()
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(25));

        KillerGenerator generator = new();
        KillerPuzzle puzzle = generator.Generate(Difficulty.UNKNOWN, cts.Token);

        Assert.IsNotNull(puzzle);

        int[] colors = ComputeCageColors(puzzle.Cages);
        AssertNoAdjacentSameColor(puzzle.Cages, colors);
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    [DataRow(Symmetry.ROTATE180)]
    [DataRow(Symmetry.MIRROR)]
    [DataRow(Symmetry.FLIP)]
    public void Generate_WithSymmetry_ProducesSymmetricCages(Symmetry symmetry)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(25));

        KillerGenerator generator = new() { Symmetry = symmetry, SymmetricCageCount = 5 };
        KillerPuzzle puzzle = generator.Generate(Difficulty.UNKNOWN, cts.Token);

        Assert.IsNotNull(puzzle, "Generator should produce a puzzle.");
        AssertValidSudoku(puzzle.Solution);
        AssertCagesSatisfied(puzzle.Solution, puzzle.Cages);
        AssertFullCoverage(puzzle.Cages);

        KillerSolver solver = new(puzzle.Cages);
        Assert.IsTrue(solver.HasUniqueSolution(), "Generated puzzle should have a unique solution.");

        // Verify that some cages have symmetric counterparts
        int symmetricPairs = CountSymmetricPairs(puzzle.Cages, symmetry);
        Assert.IsTrue(symmetricPairs >= 1,
            $"Expected at least 1 symmetric cage pair for {symmetry}, found {symmetricPairs}.");
    }

    #region Test Helpers

    /// <summary>
    /// Count cage pairs where one cage's cells are the symmetric mirror of another's.
    /// </summary>
    private static int CountSymmetricPairs(List<Cage> cages, Symmetry symmetry)
    {
        int pairs = 0;
        bool[] matched = new bool[cages.Count];

        for (int i = 0; i < cages.Count; i++)
        {
            if (matched[i]) continue;

            HashSet<int> mirrorCells = [];
            foreach (int cell in cages[i].Cells)
            {
                int row = cell / 9;
                int col = cell % 9;
                int mirror = symmetry switch
                {
                    Symmetry.ROTATE180 => (8 - row) * 9 + (8 - col),
                    Symmetry.MIRROR => row * 9 + (8 - col),
                    Symmetry.FLIP => (8 - row) * 9 + col,
                    _ => cell,
                };
                mirrorCells.Add(mirror);
            }

            for (int j = i + 1; j < cages.Count; j++)
            {
                if (matched[j]) continue;

                if (cages[j].Cells.Length == mirrorCells.Count &&
                    new HashSet<int>(cages[j].Cells).SetEquals(mirrorCells))
                {
                    matched[i] = true;
                    matched[j] = true;
                    pairs++;
                    break;
                }
            }
        }

        return pairs;
    }

    private static List<Cage> BuildHorizontalPairCages(int[] solution)
    {
        List<Cage> cages = [];
        for (int row = 0; row < 9; row++)
        {
            int b = row * 9;
            for (int pair = 0; pair < 4; pair++)
            {
                int c1 = b + pair * 2;
                int c2 = c1 + 1;
                cages.Add(new Cage([c1, c2], solution[c1] + solution[c2]));
            }
            int last = b + 8;
            cages.Add(new Cage([last], solution[last]));
        }
        return cages;
    }

    private static void AssertValidSudoku(int[] board)
    {
        Assert.HasCount(81, board);

        // Every cell must be 1-9
        for (int i = 0; i < 81; i++)
            Assert.IsTrue(board[i] >= 1 && board[i] <= 9, $"Cell {i} has invalid value {board[i]}.");

        // Rows
        for (int row = 0; row < 9; row++)
        {
            HashSet<int> seen = [];
            for (int col = 0; col < 9; col++)
                Assert.IsTrue(seen.Add(board[row * 9 + col]), $"Duplicate in row {row}.");
        }

        // Columns
        for (int col = 0; col < 9; col++)
        {
            HashSet<int> seen = [];
            for (int row = 0; row < 9; row++)
                Assert.IsTrue(seen.Add(board[row * 9 + col]), $"Duplicate in column {col}.");
        }

        // Sections
        for (int sec = 0; sec < 9; sec++)
        {
            HashSet<int> seen = [];
            foreach (int cell in QQWing.SectionLayout.SectionToSectionCells(sec))
                Assert.IsTrue(seen.Add(board[cell]), $"Duplicate in section {sec}.");
        }
    }

    private static void AssertCagesSatisfied(int[] board, List<Cage> cages)
    {
        foreach (Cage cage in cages)
        {
            int sum = 0;
            HashSet<int> seen = [];
            foreach (int cell in cage.Cells)
            {
                Assert.IsTrue(seen.Add(board[cell]),
                    $"Duplicate digit {board[cell]} in {cage}.");
                sum += board[cell];
            }
            Assert.AreEqual(cage.Sum, sum,
                $"Cage sum mismatch for {cage}: expected {cage.Sum}, got {sum}.");
        }
    }

    private static void AssertFullCoverage(List<Cage> cages)
    {
        HashSet<int> covered = [];
        foreach (Cage cage in cages)
        {
            foreach (int cell in cage.Cells)
                Assert.IsTrue(covered.Add(cell), $"Cell {cell} appears in multiple cages.");
        }
        Assert.HasCount(81, covered);
    }

    /// <summary>
    /// Greedy graph-coloring: same algorithm as GameBoardViewModel.ApplyCageData,
    /// extracted for testability. Returns an array of color indices per cage.
    /// </summary>
    private static int[] ComputeCageColors(List<Cage> cages)
    {
        int cageCount = cages.Count;
        int[] cellToCage = new int[81];
        Array.Fill(cellToCage, -1);
        for (int ci = 0; ci < cageCount; ci++)
        {
            foreach (int cell in cages[ci].Cells)
                cellToCage[cell] = ci;
        }

        HashSet<int>[] adjacent = new HashSet<int>[cageCount];
        for (int ci = 0; ci < cageCount; ci++)
            adjacent[ci] = [];

        for (int cell = 0; cell < 81; cell++)
        {
            int ci = cellToCage[cell];
            if (ci < 0) continue;

            int row = cell / 9;
            int col = cell % 9;

            int[] neighbours =
            [
                row > 0 ? cell - 9 : -1,
                row < 8 ? cell + 9 : -1,
                col > 0 ? cell - 1 : -1,
                col < 8 ? cell + 1 : -1,
            ];

            foreach (int nb in neighbours)
            {
                if (nb >= 0 && cellToCage[nb] >= 0 && cellToCage[nb] != ci)
                    adjacent[ci].Add(cellToCage[nb]);
            }
        }

        const int singleCellColor = 4;
        int[] cageColor = new int[cageCount];
        Array.Fill(cageColor, -1);
        for (int ci = 0; ci < cageCount; ci++)
        {
            if (cages[ci].Size == 1)
                cageColor[ci] = singleCellColor;
        }
        for (int ci = 0; ci < cageCount; ci++)
        {
            if (cageColor[ci] >= 0) continue;
            HashSet<int> usedColors = [];
            foreach (int adj in adjacent[ci])
            {
                if (cageColor[adj] >= 0)
                    usedColors.Add(cageColor[adj]);
            }
            int color = 0;
            while (usedColors.Contains(color))
                color++;
            cageColor[ci] = color;
        }

        return cageColor;
    }

    /// <summary>
    /// Assert no two orthogonally adjacent cages share the same color.
    /// </summary>
    private static void AssertNoAdjacentSameColor(List<Cage> cages, int[] colors)
    {
        int[] cellToCage = new int[81];
        Array.Fill(cellToCage, -1);
        for (int ci = 0; ci < cages.Count; ci++)
        {
            foreach (int cell in cages[ci].Cells)
                cellToCage[cell] = ci;
        }

        for (int cell = 0; cell < 81; cell++)
        {
            int ci = cellToCage[cell];
            if (ci < 0) continue;

            int row = cell / 9;
            int col = cell % 9;

            int[] neighbours =
            [
                row > 0 ? cell - 9 : -1,
                row < 8 ? cell + 9 : -1,
                col > 0 ? cell - 1 : -1,
                col < 8 ? cell + 1 : -1,
            ];

            foreach (int nb in neighbours)
            {
                if (nb >= 0 && cellToCage[nb] >= 0 && cellToCage[nb] != ci)
                {
                    int adj = cellToCage[nb];
                    Assert.AreNotEqual(colors[ci], colors[adj],
                        $"Cage {ci} (cell {cell}) and cage {adj} (cell {nb}) are adjacent but share color {colors[ci]}.");
                }
            }
        }
    }

    #endregion
}
