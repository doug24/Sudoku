using System.Text;
using QQWingLib;

namespace QQWingTest;

[TestClass]
public class SolverUnitTests
{
    public static void GenerateTestData()
    {
        QQWing ss = new();
        ss.SetRecordHistory(true);

        var symmetry = Symmetry.RANDOM;
        var difficulty = Difficulty.INTERMEDIATE;

        List<LogType> logTypes =
        [
            LogType.GIVEN,
            LogType.SINGLE,
            LogType.HIDDEN_SINGLE_ROW,
            LogType.HIDDEN_SINGLE_COLUMN,
            LogType.HIDDEN_SINGLE_SECTION,
            LogType.NAKED_PAIR_ROW,
            LogType.NAKED_PAIR_COLUMN,
            LogType.NAKED_PAIR_SECTION,
            LogType.POINTING_PAIR_TRIPLE_ROW,
            LogType.POINTING_PAIR_TRIPLE_COLUMN,
            LogType.ROW_BOX,
            LogType.COLUMN_BOX,
            LogType.HIDDEN_PAIR_ROW,
            LogType.HIDDEN_PAIR_COLUMN,
            LogType.HIDDEN_PAIR_SECTION
        ];

        using FileStream file = File.OpenWrite("GeneratedPuzzles.cs");
        using StreamWriter writer = new(file);

        for (int count = 0; count < 50; count++)
        {
            bool done = false;
            while (!done)
            {
                // Record whether the puzzle was possible or not,
                // so that we don't try to solve impossible givens.
                bool havePuzzle = ss.GeneratePuzzleSymmetry(symmetry, CancellationToken.None);

                if (havePuzzle)
                {
                    // Solve the puzzle
                    ss.Solve(CancellationToken.None);

                    // Bail out if it didn't meet the difficulty standards for generation
                    if (difficulty != Difficulty.UNKNOWN && difficulty != ss.GetDifficulty())
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
                    if (ss.IsSolved())
                    {
                        foreach (var instr in ss.GetSolveInstructions())
                        {
                            if (logTypes.Contains(instr.GetLogType()))
                            {
                                logTypes.Remove(instr.GetLogType());
                            }
                        }

                        if (count == 49)
                        {
                            if (logTypes.Count > 0)
                            {
                                done = false;
                                continue;
                            }
                        }

                        string puzzle = ss.GetPuzzleArray();
                        string solution = ss.GetSolutionArray();
                        StringBuilder sb = new();
                        sb.Append('{');
                        foreach (var step in ss.GetCompactSolveInstructions())
                        {
                            sb.Append('\"').Append(step).Append('\"').AppendLine(",");
                        }
                        sb.Append('}');
                        string stepString = sb.ToString();

                        writer.WriteLine($"        public static TestPuzzle Puzzle{count} = new(\"Puzzle{count}\"");
                        writer.WriteLine($"            new[] {puzzle},");
                        writer.WriteLine($"            new[] {solution},");
                        writer.WriteLine($"            new[] {stepString});");
                        writer.WriteLine();
                    }
                    else
                    {
                        done = false;
                    }
                }
            }
        }
    }

    public static IEnumerable<object[]> TestData
    {
        get
        {
            foreach (TestPuzzle item in TestPuzzle.Puzzles)
            {
                yield return new object[] { item.Name, item };
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(TestData))]
    public void TestSolveAll(string name, TestPuzzle data)
    {
        QQWing.SectionLayout = new RegularLayout();
        QQWing ss = new();
        ss.SetRecordHistory(true);
        ss.SetPuzzle(data.Puzzle);

        Assert.IsNotNull(name);
        Assert.IsTrue(ss.Solve(CancellationToken.None));

        var solution = ss.GetSolution();
        Assert.HasCount(data.Solution.Length, solution);
        for (int idx = 0; idx < data.Solution.Length; idx++)
        {
            Assert.AreEqual(data.Solution[idx], solution[idx]);
        }

        var solveSteps = ss.GetCompactSolveInstructions();
        Assert.HasCount(data.SolveSteps.Length, solveSteps);
        for (int jdx = 0; jdx < solveSteps.Length; jdx++)
        {
            Assert.AreEqual(data.SolveSteps[jdx], solveSteps[jdx]);
        }
    }

    // this test is meant to be a canary for a specific bug that was found in the past,
    // where a Naked Pair hint was returned even though the player candidates already
    // eliminated everything possible from the Naked Pair. Fixed bug where GetHint called
    // Solve which Reset the players candidates
    [TestMethod]
    public void TestHintShouldReturnSimpleColoring()
    {
        int[] currentBoard = new int[] { 0, 2, 1, 9, 8, 0, 7, 4, 3, 7, 3, 0, 1, 4, 2, 0, 8, 9, 4, 9, 8, 0, 0, 0, 0, 1, 2, 0, 4, 0, 0, 0, 0, 8, 2, 1, 8, 1, 0, 2, 0, 0, 4, 3, 7, 3, 7, 2, 8, 1, 4, 9, 6, 5, 2, 6, 3, 4, 5, 9, 1, 7, 8, 1, 5, 7, 0, 0, 8, 2, 9, 4, 9, 8, 4, 7, 2, 1, 3, 5, 6 };
        HashSet<int>[] playerCandidates = Util.DeserializeCandidates("96,0,0,0,0,96,0,0,0,0,0,96,0,0,0,96,0,0,0,0,0,104,200,136,96,0,0,96,0,608,104,712,136,0,0,0,0,0,608,0,576,96,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,72,72,0,0,0,0,0,0,0,0,0,0,0,0,0");

        QQWing ss = new();
        LogItem hint = ss.GetHint(currentBoard, playerCandidates, 0);

        Assert.IsNotNull(hint);
        Assert.AreEqual(LogType.SIMPLE_COLORING, hint.GetLogType());
    }

    // this test is meant to be a canary for a specific bug that was found in the past,
    // where an X-Wing hint was returned even though the player candidates already
    // eliminated everything possible from the X-Wing. Fixed bug where GetHint called
    // Solve which Reset the players candidates
    [TestMethod]
    public void TestHintShouldNotReturnXWing()
    {
        int[] currentBoard = new int[] { 0, 9, 6, 0, 2, 1, 0, 0, 8, 7, 1, 0, 8, 3, 6, 2, 0, 9, 8, 2, 0, 9, 7, 0, 0, 6, 1, 4, 8, 2, 0, 6, 9, 0, 0, 3, 0, 0, 1, 2, 4, 8, 9, 0, 6, 9, 6, 0, 3, 1, 0, 8, 2, 4, 2, 4, 9, 1, 5, 3, 6, 8, 7, 6, 0, 8, 0, 9, 2, 0, 0, 5, 1, 0, 0, 6, 8, 0, 0, 9, 2 };
        HashSet<int>[] playerCandidates = Util.DeserializeCandidates("40,0,0,48,0,0,184,184,0,0,0,48,0,0,0,0,48,0,0,0,56,0,0,48,56,0,0,0,0,0,160,0,0,162,162,0,40,168,0,0,0,0,0,160,0,0,0,160,0,0,160,0,0,0,0,0,0,0,0,0,0,0,0,0,136,0,144,0,0,26,26,0,0,40,168,0,0,144,24,0,0");

        QQWing ss = new();
        LogItem hint = ss.GetHint(currentBoard, playerCandidates, 0);

        Assert.IsNotNull(hint);
        Assert.AreEqual(LogType.SIMPLE_COLORING, hint.GetLogType());
    }
}