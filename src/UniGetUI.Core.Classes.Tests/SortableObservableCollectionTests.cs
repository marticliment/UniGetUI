#pragma warning disable CA1852
namespace UniGetUI.Core.Classes.Tests
{
    public class SortableObservableCollectionTests
    {
        private class SortableInt : IIndexableListItem
        {
            public int Value { get; set; }
            public int Index { get; set; }
            public SortableInt(int value) { Value = value; }
        }

        [Fact]
        public void TestSortableCollection()
        {
            int EventTriggeredCount = 0;

            SortableObservableCollection<SortableInt> SortableCollection = [];
            SortableCollection.CollectionChanged += (_, _) => { EventTriggeredCount++; };
            SortableCollection.SortingSelector = (s) => { return s.Value; };
            SortableCollection.Add(new(1));
            SortableCollection.Add(new(2));
            SortableCollection.Add(new(4));

            SortableCollection.BlockSorting = true;
            SortableCollection.Add(new(5));
            SortableCollection.Add(new(2));
            SortableCollection.BlockSorting = false;

            SortableCollection.Sort();

            Assert.Equal(7, EventTriggeredCount);
            Assert.Equal(5, SortableCollection.Count);
            Assert.Equal(1, SortableCollection[0].Value);
            Assert.Equal(2, SortableCollection[1].Value);
            Assert.Equal(2, SortableCollection[2].Value);
            Assert.Equal(4, SortableCollection[3].Value);
            Assert.Equal(5, SortableCollection[4].Value);

            for (int i = 0; i < SortableCollection.Count; i++)
            {
                Assert.Equal(i, SortableCollection[i].Index);
            }
        }
    }
}
