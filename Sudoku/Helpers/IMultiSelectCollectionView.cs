using System.Windows.Controls.Primitives;

namespace Sudoku
{
    public interface IMultiSelectCollectionView
    {
        void AddControl(Selector selector);
        void RemoveControl(Selector selector);
    }
}
