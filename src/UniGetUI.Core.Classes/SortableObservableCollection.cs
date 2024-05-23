using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace UniGetUI.Core.Classes
{
    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        public Func<T, object>? SortingSelector { get; set; }
        public bool Descending { get; set; }
        public bool BlockSorting { get; set; } = false;
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!BlockSorting)
            {
                base.OnCollectionChanged(e);
                if (SortingSelector == null
                    || e.Action == NotifyCollectionChangedAction.Remove
                    || e.Action == NotifyCollectionChangedAction.Reset)
                    return;
                Sort();
            }
        }

        public void Sort()
        {
            BlockSorting = true;

            if (SortingSelector == null)
                throw new Exception("SortableObservableCollection<T>.SortingSelector must not be null when sorting a function");

            List<T> sorted = Descending ? this.OrderByDescending(SortingSelector).ToList() : this.OrderBy(SortingSelector).ToList();
            foreach (T item in sorted)
            {
                Move(IndexOf(item), sorted.IndexOf(item));
            }
            BlockSorting = false;
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
