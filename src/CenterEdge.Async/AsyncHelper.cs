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
        /// <summary>
        /// Executes an async <see cref="Task"/> method with no return value synchronously.
        /// </summary>
        /// <param name="task"><see cref="Task"/> method to execute.</param>
        /// <remarks>
        /// DO NOT use this method unless absolutely necessary. Calling async code from sync code is an anti-pattern
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
                else
                {
                    synch.RunAlreadyComplete();
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
        /// Executes an async <see cref="Task"/> method with no return value synchronously.
        /// </summary>
        /// <param name="task"><see cref="Task"/> method to execute.</param>
        /// <param name="state">State to pass to the method.</param>
        /// <remarks>
        /// DO NOT use this method unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static void RunSync<TState>(Func<TState, Task> task, TState state)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<TaskAwaiter>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                var awaiter = task(state).GetAwaiter();

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }
                else
                {
                    synch.RunAlreadyComplete();
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
        /// <param name="task"><see cref="ValueTask"/> method to execute.</param>
        /// <remarks>
        /// DO NOT use this method unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static void RunSync(Func<ValueTask> task)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<ValueTaskAwaiter>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
#pragma warning disable CA2012
                var awaiter = task().GetAwaiter();
#pragma warning restore CA2012

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }
                else
                {
                    synch.RunAlreadyComplete();
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
        /// <param name="task"><see cref="ValueTask"/> method to execute.</param>
        /// <param name="state">State to pass to the method.</param>
        /// <remarks>
        /// DO NOT use this method unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static void RunSync<TState>(Func<TState, ValueTask> task, TState state)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<ValueTaskAwaiter>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
#pragma warning disable CA2012
                var awaiter = task(state).GetAwaiter();
#pragma warning restore CA2012

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }
                else
                {
                    synch.RunAlreadyComplete();
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
        /// <param name="task"><see cref="Task{T}"/> method to execute.</param>
        /// <returns>The asynchronous result.</returns>
        /// <remarks>
        /// DO NOT use this method unless absolutely necessary. Calling async code from sync code is an anti-pattern
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
                else
                {
                    synch.RunAlreadyComplete();
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
        /// Executes an async <see cref="Task{T}"/> method which has a void return value synchronously.
        /// </summary>
        /// <param name="task"><see cref="Task{T}"/> method to execute.</param>
        /// <param name="state">State to pass to the method.</param>
        /// <returns>The asynchronous result.</returns>
        /// <remarks>
        /// DO NOT use this method unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static T RunSync<T, TState>(Func<TState, Task<T>> task, TState state)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<TaskAwaiter<T>>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                var awaiter = task(state).GetAwaiter();

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }
                else
                {
                    synch.RunAlreadyComplete();
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
        /// <param name="task"><see cref="ValueTask{T}"/> method to execute.</param>
        /// <returns>The asynchronous result.</returns>
        /// <remarks>
        /// DO NOT use this method unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static T RunSync<T>(Func<ValueTask<T>> task)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<ValueTaskAwaiter<T>>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
#pragma warning disable CA2012
                var awaiter = task().GetAwaiter();
#pragma warning restore CA2012

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }
                else
                {
                    synch.RunAlreadyComplete();
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
        /// <param name="task"><see cref="ValueTask{T}"/> method to execute.</param>
        /// <param name="state">State to pass to the method.</param>
        /// <returns>The asynchronous result.</returns>
        /// <remarks>
        /// DO NOT use this method unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static T RunSync<T, TState>(Func<TState, ValueTask<T>> task, TState state)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<ValueTaskAwaiter<T>>(oldContext);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
#pragma warning disable CA2012
                var awaiter = task(state).GetAwaiter();
#pragma warning restore CA2012

                if (!awaiter.IsCompleted)
                {
                    synch.Run(awaiter);
                }
                else
                {
                    synch.RunAlreadyComplete();
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
        private sealed class ExclusiveSynchronizationContext<TAwaiter>(
            SynchronizationContext? parentSynchronizationContext)
            : SynchronizationContext, IDisposable
            where TAwaiter : struct, ICriticalNotifyCompletion
        {
            private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _items = [];

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

                ExecuteOnParent(d, state);
            }

            private void EndMessageLoop()
            {
                // This method is only called when _items can still accept messages
                Debug.Assert(!_items.IsAddingCompleted);

                // We could post this onto the queue to be processed, but that's unnecessary because
                // we're registered via OnCompleted on the awaiter. This will already marshal us onto
                // this synchronization context.
                //
                // If we're already on this synchronization context when the main task completes,
                // this continuation will be executed inline rather than queued in most cases.

                _items.CompleteAdding();
            }

            public void Run(TAwaiter awaiter)
            {
                // Register a callback to run when the original awaiter is completed
                // We use UnsafeOnCompleted so it doesn't flow ExecutionContext
                awaiter.UnsafeOnCompleted(EndMessageLoop);

                while (!_items.IsCompleted)
                {
                    var (callback, state) = _items.Take();

                    callback(state);
                }
            }

            // Processes any remaining continuations in the queue
            public void RunAlreadyComplete()
            {
                EndMessageLoop();

                while (!_items.IsCompleted)
                {
                    var (callback, state) = _items.Take();

                    ExecuteOnParent(callback, state);
                }
            }

            // Executes a work item on the parent SynchronizationContext or on the thread pool if there is not one
            private void ExecuteOnParent(SendOrPostCallback callback, object? state)
            {
                if (parentSynchronizationContext != null)
                {
                    parentSynchronizationContext.Post(callback, state);
                }
                else
                {
                    // There is no parent sync context, so use the default behavior from the default
                    // SynchronizationContext and post to the thread pool.

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                    ThreadPool.QueueUserWorkItem(static s => s.callback(s.state), (callback, state), preferLocal: false);
#else
                    ThreadPool.QueueUserWorkItem(static s =>
                    {
                        var state = ((SendOrPostCallback callback, object? state))s;
                        state.callback(state.state);
                    }, (callback, state));
#endif
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
