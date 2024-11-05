using System.Collections.Concurrent;
using System.Diagnostics;
using UniGetUI.Core.Logging;
using WinRT.Interop;

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
    private static ConcurrentDictionary<int, Task<ReturnT>> _tasks = new();

    /*
     * Method definitions
     */
    public static async Task<ReturnT> RunOrAttachAsync(Func<ReturnT> method)
    {
        int hash = method.GetHashCode();

        var existingTask = _getExistingTaskIfAny(hash);
        Logger.Debug($"[TaskRecycler] Deduplicated one call to {method.Method.Name}");
        return await (existingTask ?? _runTaskAndWait(Task.Run(method), hash));
    }

    public static async Task<ReturnT> RunOrAttachOrCacheAsync(Func<ReturnT> method, int cacheTimeSecs)
    {
        int hash = method.GetHashCode();
        return await (_getExistingTaskIfAny(hash) ?? _runTaskAndWait(Task.Run(method), hash, cacheTimeSecs));
    }

    public static async Task<ReturnT> RunOrAttachAsync<ParamT>(Func<ParamT, ReturnT> method, ParamT arg1)
    {
        int hash = method.GetHashCode() + (arg1?.GetHashCode() ?? 0);
        return await (_getExistingTaskIfAny(hash) ?? _runTaskAndWait(Task.Run(() => method(arg1)), hash));
    }

    public static async Task<ReturnT> RunOrAttachAsync<Param1T, Param2T>(Func<Param1T, Param2T, ReturnT> method,
        Param1T arg1, Param2T arg2)
    {
        int hash = method.GetHashCode() + (arg1?.GetHashCode() ?? 0) + +(arg2?.GetHashCode() ?? 0);
        return await (_getExistingTaskIfAny(hash) ?? _runTaskAndWait(Task.Run(() => method(arg1, arg2)), hash));
    }


    /*
     * Synchronous wrappers for methods defined above
     */
    public static ReturnT RunOrAttach(Func<ReturnT> method)
        => RunOrAttachAsync(method).GetAwaiter().GetResult();

    public static ReturnT RunOrAttach<ParamT>(Func<ParamT, ReturnT> method, ParamT arg1)
        => RunOrAttachAsync(method, arg1).GetAwaiter().GetResult();

    public static ReturnT RunOrAttach<Param1T, Param2T>(Func<Param1T, Param2T, ReturnT> method, Param1T arg1,
        Param2T arg2)
        => RunOrAttachAsync(method, arg1, arg2).GetAwaiter().GetResult();


    /*
     * No-argment entries for cached calls
     */
    public static ReturnT RunOrAttachOrCache(Func<ReturnT> method, int cacheTimeSecs)
        => RunOrAttachOrCacheAsync(method, cacheTimeSecs).GetAwaiter().GetResult();

    public static void RemoveFromCache(Func<ReturnT> method)
        => _removeFromCache(method.GetHashCode(), 0);


    /*
     * Internal methods used to cache and retrieve tasks
     */
    private static Task<ReturnT>? _getExistingTaskIfAny(int hash)
    {
        _tasks.TryGetValue(hash, out Task<ReturnT>? currentTask);
        return currentTask;
    }

    private static async Task<ReturnT> _runTaskAndWait(Task<ReturnT> task, int hash, int cacheTimeSecs = 0)
    {
        _tasks[hash] = task;
        /* BEGIN WAIT */
        ReturnT result = await task;

        /* END WAIT, AND REMOVE FROM CACHE WHEN ASKED  */
        if (cacheTimeSecs is 0) _tasks.Remove(hash, out _);
        else _removeFromCache(hash, cacheTimeSecs);

        return result;
    }

    private static async void _removeFromCache(int hash, int cacheTimeSecs)
    {
        await Task.Delay(cacheTimeSecs * 1000);
        _tasks.Remove(hash, out _);
    }
}


public static class TaskRecyclerTelemetry
{
    public static int DeduplicatedCalls = 0;
}
