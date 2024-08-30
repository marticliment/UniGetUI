namespace UniGetUI.Core.Classes;

public class ObservableQueue<T> : Queue<T>
{
    public class EventArgs(T item)
    {
        public readonly T Item = item;
    }

    public event EventHandler<EventArgs> ItemEnqueued;
    public event EventHandler<EventArgs> ItemDequeued;

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        ItemEnqueued?.Invoke(this, new EventArgs(item));
    }

    public new T Dequeue()
    {
        T item =  base.Dequeue();
        ItemDequeued?.Invoke(this, new EventArgs(item));
        return item;
    }
}
