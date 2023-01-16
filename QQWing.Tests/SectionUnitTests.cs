using QQWingLib;

namespace QQWingTest
{
    [TestClass]
    public class SectionUnitTests
    {
        [Ignore]
        public void TestSectionToSectionRows()
        {
            ISectionLayout section = new RegularLayout();

            for (int sec = 0; sec < QQWing.ROW_COL_SEC_SIZE; sec++)
            {
                var rows = section.SectionToSectionRows(sec).ToList();
            }
        }
    }
}
