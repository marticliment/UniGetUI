using System.Collections.Concurrent;

namespace UniGetUI.Core.Classes;

/*
 * This static class can help reduce the CPU
 * impact of calling a CPU-intensive method
 * that is expected to return the same result when
 * called twice concurrently.
 *
 * This can apply to getting the locally installed
 * packages, for example.
 */
public static class TaskRecycler<T>
{
    private static ConcurrentDictionary<int, Task<T>> _tasks = new();

    public static async Task<T> RunOrAttachAsync(Func<T> method)
    {
        int hash = method.GetHashCode();
        if (_tasks.TryGetValue(hash, out Task<T>? currentTask))
        {
            return await currentTask;
        }

        Task<T> task = Task.Run(method);
        _tasks[hash] = task;
        /* BEGIN WAIT */
        T result = await task;
        /* END WAIT */
        _tasks.Remove(hash, out _);
        return result;
    }
}
