namespace UniGetUI.Core.Classes.Tests
{
    [TestClass]
    public class SortableObservableCollectionTests
    {
        [TestMethod]
        public void TestSortableCollection()
        {
            int EventTriggeredCount = 0;
            
            var SortableCollection = new SortableObservableCollection<int>();
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

            Assert.AreEqual(EventTriggeredCount, 7, "The CollectionChanged event was not invoked the expected amount of times");
            Assert.AreEqual(SortableCollection[0], 1, "Collection is not sorted");
            Assert.AreEqual(SortableCollection[1], 2, "Collection is not sorted");
            Assert.AreEqual(SortableCollection[2], 3, "Collection is not sorted");
            Assert.AreEqual(SortableCollection[3], 4, "Collection is not sorted");
            Assert.AreEqual(SortableCollection[4], 5, "Collection is not sorted");
        }
    }
}