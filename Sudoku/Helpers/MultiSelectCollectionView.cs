using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Sudoku
{
    /// <summary>
    /// http://grokys.blogspot.com/2010/07/mvvm-and-multiple-selection-part-iii.html
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MultiSelectCollectionView<T> : ListCollectionView, IMultiSelectCollectionView
    {
        private bool ignoreSelectionChanged;
        private readonly List<Selector> controls = new();

        public event EventHandler? SelectionChanged;

        public MultiSelectCollectionView(IList list)
            : base(list)
        {
        }

        void IMultiSelectCollectionView.AddControl(Selector selector)
        {
            this.controls.Add(selector);
            SetSelection(selector);
            selector.SelectionChanged += OnSelectionChanged;
        }

        void IMultiSelectCollectionView.RemoveControl(Selector selector)
        {
            if (this.controls.Remove(selector))
            {
                selector.SelectionChanged -= OnSelectionChanged;
            }
        }

        public ObservableCollection<T> SelectedItems { get; private set; } = new();

        private void SetSelection(Selector selector)
        {
            if (selector is MultiSelector multiSelector)
            {
                multiSelector.SelectedItems.Clear();

                foreach (T item in SelectedItems)
                {
                    multiSelector.SelectedItems.Add(item);
                }
            }
            else if (selector is ListBox listBox)
            {
                listBox.SelectedItems.Clear();

                foreach (T item in SelectedItems)
                {
                    listBox.SelectedItems.Add(item);
                }
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.ignoreSelectionChanged)
            {
                bool changed = false;

                ignoreSelectionChanged = true;

                try
                {
                    foreach (T item in e.AddedItems)
                    {
                        if (!SelectedItems.Contains(item))
                        {
                            SelectedItems.Add(item);
                            changed = true;
                        }
                    }

                    foreach (T item in e.RemovedItems)
                    {
                        if (SelectedItems.Remove(item))
                        {
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        foreach (Selector control in this.controls)
                        {
                            if (control != sender)
                            {
                                SetSelection(control);
                            }
                        }
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                finally
                {
                    ignoreSelectionChanged = false;
                }
            }
        }
    }
}
