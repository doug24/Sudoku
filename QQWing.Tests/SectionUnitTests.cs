using QQWingLib;

namespace QQWingTest
{
    [TestClass]
    public class SectionUnitTests
    {
        [TestMethod]
        public void TestCellToSection()
        {
            int[] expected = new int[]
            {
                0, 0, 0, 1, 1, 1, 2, 2, 2,
                0, 0, 0, 1, 1, 1, 2, 2, 2,
                0, 0, 0, 1, 1, 1, 2, 2, 2,
                3, 3, 3, 4, 4, 4, 5, 5, 5,
                3, 3, 3, 4, 4, 4, 5, 5, 5,
                3, 3, 3, 4, 4, 4, 5, 5, 5,
                6, 6, 6, 7, 7, 7, 8, 8, 8,
                6, 6, 6, 7, 7, 7, 8, 8, 8,
                6, 6, 6, 7, 7, 7, 8, 8, 8,
            };

            ISectionLayout layout = new RegularLayout();
            for (int cell = 0; cell < QQWing.BOARD_SIZE; cell++)
            {
                Assert.AreEqual(expected[cell], layout.CellToSection(cell));
            }
        }

        [TestMethod]
        public void TestCellToSectionStartCell()
        {
            int[] expected = new int[]
            {
                0, 0, 0, 3, 3, 3, 6, 6, 6,
                0, 0, 0, 3, 3, 3, 6, 6, 6,
                0, 0, 0, 3, 3, 3, 6, 6, 6,
                27, 27, 27, 30, 30, 30, 33, 33, 33,
                27, 27, 27, 30, 30, 30, 33, 33, 33,
                27, 27, 27, 30, 30, 30, 33, 33, 33,
                54, 54, 54, 57, 57, 57, 60, 60, 60,
                54, 54, 54, 57, 57, 57, 60, 60, 60,
                54, 54, 54, 57, 57, 57, 60, 60, 60,
            };

            ISectionLayout layout = new RegularLayout();
            for (int cell = 0; cell < QQWing.BOARD_SIZE; cell++)
            {
                Assert.AreEqual(expected[cell], layout.CellToSectionStartCell(cell));
            }
        }

        [TestMethod]
        public void TestSectionToSectionRows()
        {
            Dictionary<int, List<int>> expected = new()
            {
                { 0, new() {0,1,2} },
                { 1, new() {0,1,2} },
                { 2, new() {0,1,2} },
                { 3, new() {3,4,5} },
                { 4, new() {3,4,5} },
                { 5, new() {3,4,5} },
                { 6, new() {6,7,8} },
                { 7, new() {6,7,8} },
                { 8, new() {6,7,8} },
            };

            ISectionLayout layout = new RegularLayout();
            for (int sec = 0; sec < QQWing.ROW_COL_SEC_SIZE; sec++)
            {
                var rows = layout.SectionToSectionRows(sec).ToList();

                Assert.AreEqual(3, rows.Count);
                var expectedRows = expected[sec];
                for (int idx = 0; idx < 3; idx++)
                {
                    Assert.AreEqual(expectedRows[idx], rows[idx]);
                }
            }
        }

        [TestMethod]
        public void TestRowToSections()
        {
            Dictionary<int, List<int>> expected = new()
            {
                { 0, new() {0,1,2} },
                { 1, new() {0,1,2} },
                { 2, new() {0,1,2} },
                { 3, new() {3,4,5} },
                { 4, new() {3,4,5} },
                { 5, new() {3,4,5} },
                { 6, new() {6,7,8} },
                { 7, new() {6,7,8} },
                { 8, new() {6,7,8} },
            };

            ISectionLayout layout = new RegularLayout();
            for (int row = 0; row < QQWing.ROW_COL_SEC_SIZE; row++)
            {
                var sections = layout.RowToSections(row).ToList();

                Assert.AreEqual(3, sections.Count);
                var expectedSections = expected[row];
                for (int idx = 0; idx < 3; idx++)
                {
                    Assert.AreEqual(expectedSections[idx], sections[idx]);
                }
            }
        }

        [TestMethod]
        public void TestSectionToSectionCols()
        {
            Dictionary<int, List<int>> expected = new()
            {
                { 0, new() {0,1,2} },
                { 1, new() {3,4,5} },
                { 2, new() {6,7,8} },
                { 3, new() {0,1,2} },
                { 4, new() {3,4,5} },
                { 5, new() {6,7,8} },
                { 6, new() {0,1,2} },
                { 7, new() {3,4,5} },
                { 8, new() {6,7,8} },
            };

            ISectionLayout layout = new RegularLayout();
            for (int sec = 0; sec < QQWing.ROW_COL_SEC_SIZE; sec++)
            {
                var cols = layout.SectionToSectionCols(sec).ToList();

                Assert.AreEqual(3, cols.Count);
                var expectedCols = expected[sec];
                for (int idx = 0; idx < 3; idx++)
                {
                    Assert.AreEqual(expectedCols[idx], cols[idx]);
                }
            }
        }

        [TestMethod]
        public void TestColumnToSections()
        {
            Dictionary<int, List<int>> expected = new()
            {
                { 0, new() {0,3,6} },
                { 1, new() {0,3,6} },
                { 2, new() {0,3,6} },
                { 3, new() {1,4,7} },
                { 4, new() {1,4,7} },
                { 5, new() {1,4,7} },
                { 6, new() {2,5,8} },
                { 7, new() {2,5,8} },
                { 8, new() {2,5,8} },
            };

            ISectionLayout layout = new RegularLayout();
            for (int col = 0; col < QQWing.ROW_COL_SEC_SIZE; col++)
            {
                var sections = layout.ColumnToSections(col).ToList();

                Assert.AreEqual(3, sections.Count);
                var expectedSections = expected[col];
                for (int idx = 0; idx < 3; idx++)
                {
                    Assert.AreEqual(expectedSections[idx], sections[idx]);
                }
            }
        }

        [TestMethod]
        public void TestSectionToSectionRowsByCol()
        {
            Dictionary<int, List<int>> expected = new()
            {
                { 0, new() {0,1,2} },
                { 1, new() {0,1,2} },
                { 2, new() {0,1,2} },
                { 3, new() {3,4,5} },
                { 4, new() {3,4,5} },
                { 5, new() {3,4,5} },
                { 6, new() {6,7,8} },
                { 7, new() {6,7,8} },
                { 8, new() {6,7,8} },
            };

            ISectionLayout layout = new RegularLayout();
            for (int sec = 0; sec < QQWing.ROW_COL_SEC_SIZE; sec++)
            {
                var cols = layout.SectionToSectionCols(sec);
                foreach (var col in cols)
                {
                    var rows = layout.SectionToSectionRowsByCol(sec, col).ToList();

                    Assert.AreEqual(3, rows.Count);
                    var expectedRows = expected[sec];
                    for (int idx = 0; idx < 3; idx++)
                    {
                        Assert.AreEqual(expectedRows[idx], rows[idx]);
                    }
                }
            }
        }
    }
}
