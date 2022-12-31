using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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
            bool enabled = (bool)e.NewValue;

            if (sender is Selector selector)
            {
                DependencyPropertyDescriptor itemsSourceProperty =
                    DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(Selector));
                IMultiSelectCollectionView? collectionView = selector.ItemsSource as IMultiSelectCollectionView;

                if (enabled)
                {
                    collectionView?.AddControl(selector);
                    itemsSourceProperty.AddValueChanged(selector, ItemsSourceChanged);
                }
                else
                {
                    collectionView?.RemoveControl(selector);
                    itemsSourceProperty.RemoveValueChanged(selector, ItemsSourceChanged);
                }
            }
        }

        static void ItemsSourceChanged(object? sender, EventArgs e)
        {
            if (sender is Selector selector && GetIsEnabled(selector))
            {
                collectionViews.TryGetValue(selector, out IMultiSelectCollectionView? oldCollectionView);
                if (oldCollectionView != null)
                {
                    oldCollectionView.RemoveControl(selector);
                    collectionViews.Remove(selector);
                }

                if (selector.ItemsSource is IMultiSelectCollectionView newCollectionView)
                {
                    newCollectionView.AddControl(selector);
                    collectionViews.Add(selector, newCollectionView);
                }
            }
        }

        static readonly Dictionary<Selector, IMultiSelectCollectionView> collectionViews = new();
    }
}
