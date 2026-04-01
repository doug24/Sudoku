using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Sudoku
{
    /// <summary>
    /// Interaction logic for ImportWindow.xaml
    /// </summary>
    public partial class ImportWindow : Window
    {
        public int[] InitialBoard { get; private set; } = new int[81];
        public string[] Candidates { get; private set; } = new string[81];

        public ImportWindow()
        {
            InitializeComponent();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            bool skipCandidates = CandidatesCheckBox.IsChecked == true;

            string puzzleData = InputTextBox.Text;

            // PuzzleData must contain 81 numbers. Zeroes can be dots or other punctuation.
            // This importer can handle noisy formats like grids with lines and dots.
            // As long as there are 81 separated numbers.
            // Any number greater than 9 will be treated as a set of candidates.

            List<string> tokens = ParseTokens(puzzleData);

            if (tokens.Count != 81)
            {
                MessageBox.Show(
                    $"Expected 81 numbers but found {tokens.Count}. " +
                    "Please check the input and try again.",
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int[] initialBoard = new int[81];
            string[] candidates = new string[81];

            for (int i = 0; i < 81; i++)
            {
                int value = int.Parse(tokens[i]);
                if (value >= 1 && value <= 9)
                {
                    initialBoard[i] = value;
                    candidates[i] = tokens[i];
                }
                else if (value > 9 && !skipCandidates)
                {
                    // Each digit is a candidate, format as "c135" for FromSnapshotString
                    initialBoard[i] = 0;
                    var digits = tokens[i]
                        .Select(d => d - '0')
                        .Where(d => d >= 1 && d <= 9)
                        .Distinct()
                        .OrderBy(d => d);
                    candidates[i] = "c" + string.Join(string.Empty, digits);
                }
                // else value == 0: empty cell
            }

            InitialBoard = initialBoard;
            Candidates = candidates;
            DialogResult = true;
            Close();
        }

        private static List<string> ParseTokens(string puzzleData)
        {
            // Try separated format first: treat '.' as '0', extract groups of digits,
            // and ignore grid decoration characters like '-', '+', '|'
            List<string> tokens = [];
            StringBuilder sb = new();

            foreach (char c in puzzleData)
            {
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
                else if (c == '.')
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                    tokens.Add("0");
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                }
            }

            if (sb.Length > 0)
                tokens.Add(sb.ToString());

            if (tokens.Count == 81)
                return tokens;

            // Fall back to compact format: each digit or dot is one cell
            tokens.Clear();
            foreach (char c in puzzleData)
            {
                if (char.IsDigit(c))
                    tokens.Add(c.ToString());
                else if (c == '.')
                    tokens.Add("0");
            }

            return tokens;
        }
    }
}
