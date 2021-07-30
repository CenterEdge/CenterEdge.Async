using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CenterEdge.Async
{
    /// <summary>
    /// Executes asynchronous tasks synchronously.
    /// </summary>
    /// <remarks>
    /// DO NOT use this class unless absolutely necessary. Calling async code from sync code is an anti-pattern
    /// in most cases. This class is provided to assist in gradual conversion from sync to async code.
    /// </remarks>
    public static class AsyncHelper
    {
        // We allocate this delegate up-front rather than including it as an inline lambda for a small performance boost.
        // If placed as a lambda it's lazy initialized, requiring a null check each time. Since we know we're almost always
        // going to need it we can avoid that check. It's placed in AsyncHelper rather than ExclusiveSynchronizationContext
        // because it doesn't require the generic type parameter. It expects the BlockingCollection to be passed as the state.
        private static readonly SendOrPostCallback queueDoneMessage =
            static state =>
            {
                Debug.Assert(state is BlockingCollection<(SendOrPostCallback Callback, object? State)>);

                ((BlockingCollection<(SendOrPostCallback Callback, object? State)>)state!).CompleteAdding();
            };

        /// <summary>
        /// Executes an async <see cref="Task"/> method with no return value synchronously.
        /// </summary>
        /// <param name="task"><see cref="Task"/> method to execute</param>
        /// <remarks>
        /// DO NOT use this methods unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static void RunSync(Func<Task> task)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<TaskAwaiter>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                var awaiter = task().GetAwaiter();

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }

                // Throw any exception returned by the task
                awaiter.GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }

        /// <summary>
        /// Executes an async <see cref="ValueTask"/> method which has a void return value synchronously.
        /// </summary>
        /// <param name="task"><see cref="ValueTask"/> method to execute</param>
        /// <remarks>
        /// DO NOT use this methods unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static void RunSync(Func<ValueTask> task)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<ValueTaskAwaiter>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                var awaiter = task().GetAwaiter();

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }

                // Throw any exception returned by the task
                awaiter.GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }

        /// <summary>
        /// Executes an async <see cref="Task{T}"/> method which has a void return value synchronously.
        /// </summary>
        /// <param name="task"><see cref="Task{T}"/> method to execute</param>
        /// <returns>The asynchronous result.</returns>
        /// <remarks>
        /// DO NOT use this methods unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static T RunSync<T>(Func<Task<T>> task)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<TaskAwaiter<T>>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                var awaiter = task().GetAwaiter();

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }

                // Throw any exception returned by the task or return the result
                return awaiter.GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }

        /// <summary>
        /// Executes an async <see cref="ValueTask{T}"/> method which has a void return value synchronously.
        /// </summary>
        /// <param name="task"><see cref="ValueTask{T}"/> method to execute</param>
        /// <returns>The asynchronous result.</returns>
        /// <remarks>
        /// DO NOT use this methods unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static T RunSync<T>(Func<ValueTask<T>> task)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<ValueTaskAwaiter<T>>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                var awaiter = task().GetAwaiter();

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }

                // Throw any exception returned by the task or return the result
                return awaiter.GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }

        // Note: Sealing this class can help JIT make non-virtual method calls and inlined method calls for virtual methods
        private sealed class ExclusiveSynchronizationContext<TAwaiter> : SynchronizationContext, IDisposable
            where TAwaiter : struct, ICriticalNotifyCompletion
        {
            private readonly SynchronizationContext? _parentSynchronizationContext;
            private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _items = new();

            public ExclusiveSynchronizationContext(SynchronizationContext? parentSynchronizationContext)
            {
                _parentSynchronizationContext = parentSynchronizationContext;
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotSupportedException("We cannot send to our same thread");
            }

            public override void Post(SendOrPostCallback d, object? state)
            {
                try
                {
                    _items.Add((d, state));
                    return;
                }
                catch (InvalidOperationException)
                {
                    // This indicates the items cannot be added because the main loop
                    // is complete. We can't do a boolean check on IsAddingComplete because
                    // it will throw an ObjectDisposedException.
                }

                // The collection is closed to new items because we got done with the main task.
                // Instead, we post any remaining work to the parent SynchronizationContext.
                // This can occur if the main task starts additional work which isn't completed
                // before the main task completes.

                if (_parentSynchronizationContext != null)
                {
                    _parentSynchronizationContext.Post(d, state);
                }
                else
                {
                    // There is no parent sync context, so use the default behavior from the default
                    // SynchronizationContext and post to the thread pool.

                    #if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                    ThreadPool.QueueUserWorkItem(static s => s.d(s.state), (d, state), preferLocal: false);
                    #else
                    ThreadPool.QueueUserWorkItem(static s =>
                    {
                        var state = ((SendOrPostCallback d, object? state))s;
                        state.d(state.state);
                    }, (d, state));
                    #endif
                }
            }

            private void EndMessageLoop()
            {
                // This method is only called when _items can still accept messages
                // So we can get a small perf gain by avoiding the virtual method call
                // and exception handling in the Post method.

                Debug.Assert(!_items.IsAddingCompleted);

                _items.Add((queueDoneMessage, _items));
            }

            public void Run(TAwaiter awaiter)
            {
                // Register a callback to run when the original awaiter is completed
                // We use UnsafeOnCompleted so it doesn't flow ExecutionContext
                awaiter.UnsafeOnCompleted(EndMessageLoop);

                while (!_items.IsCompleted)
                {
                    var task = _items.Take();

                    task.Callback(task.State);
                }
            }

            public override SynchronizationContext CreateCopy()
            {
                return this;
            }

            public void Dispose()
            {
                _items.Dispose();
            }
        }
    }
}
