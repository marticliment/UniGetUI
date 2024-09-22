using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace UniGetUI.Core.Classes
{
    public class SortableObservableCollection<T> : ObservableCollection<T> where T : IIndexableListItem
    {
        public Func<T, object>? SortingSelector { get; set; }
        public bool Descending { get; set; }
        public bool BlockSorting { get; set; }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!BlockSorting)
            {
                base.OnCollectionChanged(e);
                if (SortingSelector is null
                    || e.Action == NotifyCollectionChangedAction.Remove
                    || e.Action == NotifyCollectionChangedAction.Reset)
                {
                    return;
                }

                Sort();
            }
        }

        public void Sort()
        {
            BlockSorting = true;

            if (SortingSelector is null)
            {
                throw new InvalidOperationException("SortableObservableCollection<T>.SortingSelector must not be null when sorting");
            }

            List<T> sorted = Descending ? this.OrderByDescending(SortingSelector).ToList() : this.OrderBy(SortingSelector).ToList();
            foreach (T item in sorted)
            {
                Move(IndexOf(item), sorted.IndexOf(item));
            }

            for (int i = 0; i < Count; i++)
            {
                this[i].Index = i;
            }

            BlockSorting = false;
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
