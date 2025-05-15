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
 *
 *
 * WARNING: When using TaskRecycler with methods that return instances of classes
 * the return instance WILL BE THE SAME when the call attaches to an existing call.
 * Take this into account when handling received objects.
 */
public static class TaskRecycler<ReturnT>
{
    private static readonly ConcurrentDictionary<int, Task<ReturnT>> _tasks = new();
    private static readonly ConcurrentDictionary<int, Task> _tasks_VOID = new();

    // ---------------------------------------------------------------------------------------------------------------

    public static Task RunOrAttachAsync_VOID(Action method, int cacheTimeSecs = 0)
    {
        int hash = method.GetHashCode();
        return _runTaskAndWait_VOID(new Task(method), hash, cacheTimeSecs);
    }

    /// Asynchronous entry point for 0 parameters
    public static Task<ReturnT> RunOrAttachAsync(Func<ReturnT> method, int cacheTimeSecs = 0)
    {
        int hash = method.GetHashCode();
        return _runTaskAndWait(new Task<ReturnT>(method), hash, cacheTimeSecs);
    }

    /// Asynchronous entry point for 1 parameter
    public static Task<ReturnT> RunOrAttachAsync<ParamT>(Func<ParamT, ReturnT> method, ParamT arg1, int cacheTimeSecs = 0)
    {
        int hash = method.GetHashCode() + (arg1?.GetHashCode() ?? 0);
        return _runTaskAndWait(new Task<ReturnT>(() => method(arg1)), hash, cacheTimeSecs);
    }

    /// Asynchronous entry point for 2 parameters
    public static Task<ReturnT> RunOrAttachAsync<Param1T, Param2T>(Func<Param1T, Param2T, ReturnT> method,
        Param1T arg1, Param2T arg2, int cacheTimeSecs = 0)
    {
        int hash = method.GetHashCode() + (arg1?.GetHashCode() ?? 0) + (arg2?.GetHashCode() ?? 0);
        return _runTaskAndWait(new Task<ReturnT>(() => method(arg1, arg2)), hash, cacheTimeSecs);
    }

    /// Asynchronous entry point for 3 parameters
    public static Task<ReturnT> RunOrAttachAsync<Param1T, Param2T, Param3T>(Func<Param1T, Param2T, Param3T, ReturnT> method,
        Param1T arg1, Param2T arg2, Param3T arg3, int cacheTimeSecs = 0)
    {
        int hash = method.GetHashCode() + (arg1?.GetHashCode() ?? 0) + (arg2?.GetHashCode() ?? 0) + (arg3?.GetHashCode() ?? 0);
        return _runTaskAndWait(new Task<ReturnT>(() => method(arg1, arg2, arg3)), hash, cacheTimeSecs);
    }

    // ---------------------------------------------------------------------------------------------------------------

    /// Synchronous entry point for 0 parameters
    public static ReturnT RunOrAttach(Func<ReturnT> method, int cacheTimeSecs = 0)
        => RunOrAttachAsync(method, cacheTimeSecs).GetAwaiter().GetResult();

    /// Synchronous entry point for 1 parameter1
    public static ReturnT RunOrAttach<ParamT>(Func<ParamT, ReturnT> method, ParamT arg1, int cacheTimeSecs = 0)
        => RunOrAttachAsync(method, arg1, cacheTimeSecs).GetAwaiter().GetResult();

    /// Synchronous entry point for 2 parameters
    public static ReturnT RunOrAttach<Param1T, Param2T>(Func<Param1T, Param2T, ReturnT> method, Param1T arg1,
        Param2T arg2, int cacheTimeSecs = 0)
        => RunOrAttachAsync(method, arg1, arg2, cacheTimeSecs).GetAwaiter().GetResult();

    /// Synchronous entry point for 3 parameters
    public static ReturnT RunOrAttach<Param1T, Param2T, Param3T>(Func<Param1T, Param2T, Param3T, ReturnT> method, Param1T arg1,
        Param2T arg2, Param3T arg3, int cacheTimeSecs = 0)
        => RunOrAttachAsync(method, arg1, arg2, arg3, cacheTimeSecs).GetAwaiter().GetResult();

    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Instantly removes a function call from the cache, even if the associated task has not
    /// finished yet. Any previous call will finish as expected. New calls won't attach to any
    /// preexisting Tasks, and a new Task will be created instead.
    /// If the given function call is not present in the cache, nothing will be done.
    /// </summary>
    /// <param name="method"></param>
    public static void RemoveFromCache(Func<ReturnT> method)
        => _removeFromCache(method.GetHashCode(), 0);

    // ---------------------------------------------------------------------------------------------------------------


    /// <summary>
    /// Handles running the task if no such task was found on cache, and returning the cached task if it was found.
    /// </summary>
    private static async Task _runTaskAndWait_VOID(Task task, int hash, int cacheTimeSecsSecs)
    {
        if (_tasks_VOID.TryGetValue(hash, out Task? _task))
        {
            // Get the cached task, which is either running or finished
            task = _task;
        }
        else if (!_tasks_VOID.TryAdd(hash, task))
        {
            // Race condition, an equivalent task got added from another thread between the TryGetValue and TryAdd,
            // so we are going to restart the call to _runTaskAndWait in order for TryGetValue to return the new task again
            await _runTaskAndWait_VOID(task, hash, cacheTimeSecsSecs);
            return;
        }
        else
        {
            // Now that the new task is in the cache, run the task.
            task.Start();
        }

        // Wait for the task to finish
        await task;

        // Schedule the task for removal after the cache time expires
        _removeFromCache_VOID(hash, cacheTimeSecsSecs);
    }

    /// <summary>
    /// Handles running the task if no such task was found on cache, and returning the cached task if it was found.
    /// </summary>
    private static async Task<ReturnT> _runTaskAndWait(Task<ReturnT> task, int hash, int cacheTimeSecsSecs)
    {
        if (_tasks.TryGetValue(hash, out Task<ReturnT>? _task))
        {
            // Get the cached task, which is either running or finished
            task = _task;
        }
        else if (!_tasks.TryAdd(hash, task))
        {
            // Race condition, an equivalent task got added from another thread between the TryGetValue and TryAdd,
            // so we are going to restart the call to _runTaskAndWait in order for TryGetValue to return the new task again
            return await _runTaskAndWait(task, hash, cacheTimeSecsSecs);
        }
        else
        {
            // Now that the new task is in the cache, run the task.
            task.Start();
        }

        // Wait for the task to finish
        ReturnT result = await task;

        // Schedule the task for removal after the cache time expires
        _removeFromCache(hash, cacheTimeSecsSecs);

        return result;
    }

    /// <summary>
    /// Removes the task associated with the given hash from the cache after the given period of time
    /// If the given hash is not present, nothing will be done.
    /// </summary>
    private static async void _removeFromCache(int hash, int cacheTimeSecsSecs)
    {
        if (cacheTimeSecsSecs > 0)
            await Task.Delay(cacheTimeSecsSecs * 1000);

        _tasks.Remove(hash, out _);
    }

    private static async void _removeFromCache_VOID(int hash, int cacheTimeSecsSecs)
    {
        if (cacheTimeSecsSecs > 0)
            await Task.Delay(cacheTimeSecsSecs * 1000);

        _tasks_VOID.Remove(hash, out _);
    }
}

public static class TaskRecyclerTelemetry
{
    public static int DeduplicatedCalls;
}
