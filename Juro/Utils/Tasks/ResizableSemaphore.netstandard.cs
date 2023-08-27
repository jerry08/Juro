using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Juro.Utils.Tasks;

internal partial class ResizableSemaphore : IDisposable
{
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();

    private void Refresh()
    {
        lock (_lock)
        {
            while (_count < MaxCount)
            {
                try
                {
                    var waiter = _waiters.Dequeue();

                    // Don't increment if the waiter has ben canceled
                    if (waiter.TrySetResult(true))
                        _count++;
                }
                catch
                {
                    break;
                }
            }
        }
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name);

        var waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        using (_cts.Token.Register(() => waiter.TrySetCanceled(_cts.Token)))
        using (cancellationToken.Register(() => waiter.TrySetCanceled(cancellationToken)))
        {
            lock (_lock)
            {
                _waiters.Enqueue(waiter);
                Refresh();
            }

            await waiter.Task;

            return new AcquiredAccess(this);
        }
    }
}