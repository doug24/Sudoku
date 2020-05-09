using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Sudoku
{
    public static class MultiSelect
    {
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
                new UIPropertyMetadata(IsEnabledChanged));

        static void IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Selector selector = sender as Selector;
            bool enabled = (bool)e.NewValue;

            if (selector != null)
            {
                DependencyPropertyDescriptor itemsSourceProperty =
                    DependencyPropertyDescriptor.FromProperty(Selector.ItemsSourceProperty, typeof(Selector));
                IMultiSelectCollectionView collectionView = selector.ItemsSource as IMultiSelectCollectionView;

                if (enabled)
                {
                    if (collectionView != null) collectionView.AddControl(selector);
                    itemsSourceProperty.AddValueChanged(selector, ItemsSourceChanged);
                }
                else
                {
                    if (collectionView != null) collectionView.RemoveControl(selector);
                    itemsSourceProperty.RemoveValueChanged(selector, ItemsSourceChanged);
                }
            }
        }

        static void ItemsSourceChanged(object sender, EventArgs e)
        {
            Selector selector = sender as Selector;

            if (GetIsEnabled(selector))
            {
                IMultiSelectCollectionView oldCollectionView;
                IMultiSelectCollectionView newCollectionView = selector.ItemsSource as IMultiSelectCollectionView;
                collectionViews.TryGetValue(selector, out oldCollectionView);

                if (oldCollectionView != null)
                {
                    oldCollectionView.RemoveControl(selector);
                    collectionViews.Remove(selector);
                }

                if (newCollectionView != null)
                {
                    newCollectionView.AddControl(selector);
                    collectionViews.Add(selector, newCollectionView);
                }
            }
        }

        static Dictionary<Selector, IMultiSelectCollectionView> collectionViews =
            new Dictionary<Selector, IMultiSelectCollectionView>();
    }
}
