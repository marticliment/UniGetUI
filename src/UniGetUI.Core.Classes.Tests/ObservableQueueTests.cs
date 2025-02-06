namespace UniGetUI.Core.Classes.Tests;

public class ObservableQueueTests
{
    [Fact]
    public void TestObservableQueue()
    {
        var queue = new ObservableQueue<int>();

        List<int> enqueuedElements = [];
        List<int> dequeuedElements = [];

        queue.ItemEnqueued += (_, e) => enqueuedElements.Add(e.Item);
        queue.ItemDequeued += (_, e) => dequeuedElements.Add(e.Item);

        queue.Enqueue(1);
        queue.Enqueue(2);
        Assert.Equal(1, queue.Dequeue());
        queue.Enqueue(4);
        queue.Enqueue(3);

        Assert.Equal(2, queue.Dequeue());
        Assert.Equal(4, queue.Dequeue());

        Assert.Equal(4, enqueuedElements.Count);
        Assert.Equal(1, enqueuedElements[0]);
        Assert.Equal(2, enqueuedElements[1]);
        Assert.Equal(4, enqueuedElements[2]);
        Assert.Equal(3, enqueuedElements[3]);

        Assert.Equal(3, dequeuedElements.Count);
        Assert.Equal(1, dequeuedElements[0]);
        Assert.Equal(2, dequeuedElements[1]);
        Assert.Equal(4, dequeuedElements[2]);
    }
}
