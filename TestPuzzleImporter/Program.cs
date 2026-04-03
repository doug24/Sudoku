using System.Text;
using QQWingLib;

class Program
{
    [STAThread] // Required for clipboard access
    static void Main()
    {
        Console.WriteLine("Create a puzzle for the QQWing unit tests.");
        Console.WriteLine("Enter 81 numbers. Zeroes can be dots or other punctuation.\r\nThis importer can handle noisy formats like grids with lines and dots.\r\nAs long as there are 81 separated numbers.\r\nAny number greater than 9 will be treated as a set of candidates.");
        Console.WriteLine();
        Console.WriteLine("This app reads data from the clipboard, and writes the output back to the clipboard.");
        Console.WriteLine("Press any key when the data is on the clipboard...");

        Console.ReadKey(intercept: true);

        Console.WriteLine();
        Console.WriteLine();

        if (Clipboard.ContainsText())
        {
            string clipboardText = Clipboard.GetText();
            try
            {
                var (puzzle, solution, difficulty, solveSteps) = Util.ParsePuzzleData(clipboardText);

                // Output the parsed puzzle, candidates, difficulty, and solve steps
                // in the formate of a TestPuzzleData instance for use in unit tests.

                string puzzleData = string.Join(", ", puzzle.Select(v => v.ToString()));
                string solutionData = string.Join(", ", solution.Select(v => v.ToString()));

                StringBuilder sb = new();
                sb.AppendLine($"// Parsed Puzzle difficulty: {difficulty}");
                sb.AppendLine("public static readonly TestPuzzle Puzzle0 = new(\"Puzzle0\",");
                sb.AppendLine($"    [{puzzleData}],");
                sb.AppendLine($"    [{solutionData}],");
                sb.AppendLine("    [");
                for (int i = 0; i < solveSteps.Length; i++)
                {
                    sb.AppendLine($"        \"{solveSteps[i]}\",");
                }
                sb.AppendLine("        ]);");
                sb.AppendLine("");

                Clipboard.SetText(sb.ToString());
                Console.WriteLine("Parsed puzzle data has been copied to the clipboard.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing puzzle data: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("No text found in the clipboard. Please copy the puzzle data and try again.");
        }
    }
}