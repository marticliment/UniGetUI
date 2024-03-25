// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace DevHome.SetupFlow.Common.Elevation;

public class RemoteObject<T> : IDisposable
{
    private readonly Semaphore _completionSemaphore;
    private bool _disposedValue;

    public T Value { get; }

    public RemoteObject(T value, Semaphore completionSemaphore)
    {
        Value = value;
        _completionSemaphore = completionSemaphore;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _completionSemaphore.Release();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
