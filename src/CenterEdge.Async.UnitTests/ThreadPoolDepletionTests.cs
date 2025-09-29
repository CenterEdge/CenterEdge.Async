using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CenterEdge.Async.UnitTests;

public class ThreadPoolDepletionTests
{
    [Fact]
    public void ReproThreadPoolDepletion()
    {
        // This test demonstrates running a single-threaded synchronization context,
        // Using AsyncHelper.RunSync from this context, using ConfigureAwait(false) to
        // get off the context and onto the thread pool, and then using AsyncHelper.RunSync
        // again from the thread pool thread. This is a contrived example that could happen
        // in multi-layer architectures where some layers are async and some are sync.
        // In older .NET, this test will run slowly due to thread pool depletion and the
        // hill climbing algorithm for adding threads to the pool. But in modern .NET (6.0+)
        // this test should run quickly because it recognizes we're waiting on tasks in a
        // thread pool thread.

        static async Task DoAnotherAsyncThing()
        {
            await Task.Delay(1).ConfigureAwait(false);
        }

        static void DoSyncThing()
        {
            AsyncHelper.RunSync(DoAnotherAsyncThing);
        }

        static async Task DoAsyncThing()
        {
            await Task.Delay(1).ConfigureAwait(false);

            // We're on the thread pool here due to ConfigureAwait(false)
            DoSyncThing();
        }

        var sw = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (var i = 0; i < Environment.ProcessorCount * 4; i++)
        {
            var tcs = new TaskCompletionSource<object>();
            tasks.Add(tcs.Task);

            new Thread(() =>
            {
                try
                {
                    SingleThreadedSynchronizationContext.Run(() =>
                    {
                        AsyncHelper.RunSync(DoAsyncThing);
                    });
                }
                finally
                {
                    tcs.TrySetResult(null);
                }
            })
            { IsBackground = true }.Start();
        }

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Task.WaitAll([.. tasks], TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        sw.Stop();

        TestContext.Current.TestOutputHelper?.WriteLine($"Elapsed: {sw.Elapsed.TotalMilliseconds}ms");
    }

    internal sealed class SingleThreadedSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new BlockingCollection<(SendOrPostCallback Callback, object? State)>();

        public override void Send(SendOrPostCallback d, object? state) // Sync operations
        {
            throw new NotSupportedException($"{nameof(SingleThreadedSynchronizationContext)} does not support synchronous operations.");
        }

        public override void Post(SendOrPostCallback d, object? state) // Async operations
        {
            _queue.Add((d, state));
        }

        public static void Run(Action action)
        {
            var previous = Current;
            var context = new SingleThreadedSynchronizationContext();
            SetSynchronizationContext(context);
            try
            {
                action();

                while (context._queue.TryTake(out var item))
                {
                    item.Callback(item.State);
                }
            }
            finally
            {
                context._queue.CompleteAdding();
                SetSynchronizationContext(previous);
            }
        }
    }
}
