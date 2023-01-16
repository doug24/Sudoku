using System.Text;
using QQWingLib;

namespace QQWingTest
{
    [TestClass]
    public class SolverUnitTests
    {
        [Ignore]
        public void GenerateTestData()
        {
            QQWing ss = new();
            ss.SetRecordHistory(true);

            var symmetry = Symmetry.RANDOM;
            var difficulty = Difficulty.INTERMEDIATE;

            List<LogType> logTypes = new()
            { 
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
            };

            using FileStream file = File.OpenWrite("GeneratedPuzzles.cs");
            using StreamWriter writer = new(file);

            for (int count = 0; count < 50; count++)
            {
                bool done = false;
                while (!done)
                {
                    // Record whether the puzzle was possible or not,
                    // so that we don't try to solve impossible givens.
                    bool havePuzzle = ss.GeneratePuzzleSymmetry(symmetry);

                    if (havePuzzle)
                    {
                        // Solve the puzzle
                        ss.Solve();

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
            QQWing ss = new();
            ss.SetRecordHistory(true);
            ss.SetPuzzle(data.Puzzle);

            Assert.IsNotNull(name);
            Assert.IsTrue(ss.Solve());

            var solution = ss.GetSolution();
            Assert.AreEqual(data.Solution.Length, solution.Length);
            for (int idx = 0; idx < data.Solution.Length; idx++)
            {
                Assert.AreEqual(data.Solution[idx], solution[idx]);
            }

            var solveSteps = ss.GetCompactSolveInstructions();
            Assert.AreEqual(data.SolveSteps.Length, solveSteps.Length);
            for (int jdx = 0; jdx < solveSteps.Length; jdx++)
            {
                Assert.AreEqual(data.SolveSteps[jdx], solveSteps[jdx]);
            }
        }
    }
}