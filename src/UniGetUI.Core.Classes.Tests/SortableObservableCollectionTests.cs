namespace UniGetUI.Core.Classes.Tests
{
    public class SortableObservableCollectionTests
    {
        [Fact]
        public void TestSortableCollection()
        {
            int EventTriggeredCount = 0;

            SortableObservableCollection<int> SortableCollection = new();
            SortableCollection.CollectionChanged += (s, e) => { EventTriggeredCount++; };
            SortableCollection.SortingSelector = (s) => { return s; };
            SortableCollection.Add(1);
            SortableCollection.Add(3);
            SortableCollection.Add(4);

            SortableCollection.BlockSorting = true;
            SortableCollection.Add(5);
            SortableCollection.Add(2);
            SortableCollection.BlockSorting = false;


            SortableCollection.Sort();

            Assert.Equal(7, EventTriggeredCount);
            Assert.Equal(1, SortableCollection[0]);
            Assert.Equal(2, SortableCollection[1]);
            Assert.Equal(3, SortableCollection[2]);
            Assert.Equal(4, SortableCollection[3]);
            Assert.Equal(5, SortableCollection[4]);
        }
    }
}