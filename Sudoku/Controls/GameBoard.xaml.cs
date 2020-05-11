using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Sudoku
{
    /// <summary>
    /// Interaction logic for GameBoard.xaml
    /// </summary>
    public partial class GameBoard : UserControl
    {
        public GameBoard()
        {
            InitializeComponent();
        }

        private void ListBox_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (e.OriginalSource is DependencyObject control)
            {
                var listBoxItem = FindParent<ListBoxItem>(control);

                if (listBoxItem != null)
                {
                    gameListBox.SelectedItem = listBoxItem;
                    e.Handled = true;
                }
            }
        }
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            if (parentObject is T parent)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }
}
