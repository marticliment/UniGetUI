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

            Assert.AreEqual(7, EventTriggeredCount, "The CollectionChanged event was not invoked the expected amount of times");
            Assert.AreEqual(1, SortableCollection[0], "Collection is not sorted");
            Assert.AreEqual(2, SortableCollection[1], "Collection is not sorted");
            Assert.AreEqual(3, SortableCollection[2], "Collection is not sorted");
            Assert.AreEqual(4, SortableCollection[3], "Collection is not sorted");
            Assert.AreEqual(5, SortableCollection[4], "Collection is not sorted");
        }
    }
}