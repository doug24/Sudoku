using System.Windows;
using System.Windows.Controls.Primitives;

namespace Sudoku
{
    public static class MultiSelect
    {
        static MultiSelect()
        {
            Selector.ItemsSourceProperty.OverrideMetadata(typeof(Selector),
                new FrameworkPropertyMetadata(ItemsSourceChanged));
        }

        public static bool GetIsEnabled(Selector target)
        {
            return (bool)target.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(Selector target, bool value)
        {
            target.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(MultiSelect),
                new UIPropertyMetadata(false, IsEnabledChanged));

        static void IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Selector selector = sender as Selector;
            IMultiSelectCollectionView collectionView = selector.ItemsSource as IMultiSelectCollectionView;

            if (selector != null && collectionView != null)
            {
                if ((bool)e.NewValue)
                {
                    collectionView.AddControl(selector);
                }
                else
                {
                    collectionView.RemoveControl(selector);
                }
            }
        }

        static void ItemsSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Selector selector = sender as Selector;

            if (GetIsEnabled(selector))
            {
                if (e.OldValue is IMultiSelectCollectionView oldCollectionView)
                {
                    oldCollectionView.RemoveControl(selector);
                }

                if (e.NewValue is IMultiSelectCollectionView newCollectionView)
                {
                    newCollectionView.AddControl(selector);
                }
            }
        }
    }
}
