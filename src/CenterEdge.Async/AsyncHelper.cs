using System;
using System.Collections.Concurrent;
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
        /// <param name="task"><see cref="Task"/> method to execute</param>
        /// <remarks>
        /// DO NOT use this methods unless absolutely necessary. Calling async code from sync code is an anti-pattern
        /// in most cases. This method is provided to assist in gradual conversion from sync to async code.
        /// </remarks>
        public static void RunSync(Func<Task> task)
        {
            var oldContext = SynchronizationContext.Current;
            using var synch = new ExclusiveSynchronizationContext<Task, VoidTaskResult>(task);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                synch.Post(static async state =>
                {
                    var synch = (ExclusiveSynchronizationContext<Task, VoidTaskResult>)state!;

                    try
                    {
                        await synch.InitialTask();
                    }
                    catch (Exception e)
                    {
                        synch.TaskException = e;
                        throw;
                    }
                    finally
                    {
                        synch.EndMessageLoop();
                    }
                }, synch);
                synch.BeginMessageLoop();
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
            using var synch = new ExclusiveSynchronizationContext<ValueTask, VoidTaskResult>(task);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                synch.Post(static async state =>
                {
                    var synch = (ExclusiveSynchronizationContext<ValueTask, VoidTaskResult>)state!;

                    try
                    {
                        await synch.InitialTask();
                    }
                    catch (Exception e)
                    {
                        synch.TaskException = e;
                        throw;
                    }
                    finally
                    {
                        synch.EndMessageLoop();
                    }
                }, synch);
                synch.BeginMessageLoop();
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
            using var synch = new ExclusiveSynchronizationContext<Task<T>, T>(task);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                synch.Post(static async state =>
                {
                    var synch = (ExclusiveSynchronizationContext<Task<T>, T>)state!;

                    try
                    {
                        synch.Result = await synch.InitialTask();
                    }
                    catch (Exception e)
                    {
                        synch.TaskException = e;
                        throw;
                    }
                    finally
                    {
                        synch.EndMessageLoop();
                    }
                }, synch);

                synch.BeginMessageLoop();
                return synch.Result!;
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
            using var synch = new ExclusiveSynchronizationContext<ValueTask<T>, T>(task);
            SynchronizationContext.SetSynchronizationContext(synch);
            try
            {
                synch.Post(static async state =>
                {
                    var synch = (ExclusiveSynchronizationContext<ValueTask<T>, T>)state!;

                    try
                    {
                        synch.Result = await synch.InitialTask();
                    }
                    catch (Exception e)
                    {
                        synch.TaskException = e;
                        throw;
                    }
                    finally
                    {
                        synch.EndMessageLoop();
                    }
                }, synch);

                synch.BeginMessageLoop();
                return synch.Result!;
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }

        private sealed class ExclusiveSynchronizationContext<TTask, TResult> : SynchronizationContext, IDisposable
        {
            public Func<TTask> InitialTask { get; }
            public TResult? Result { get; set; }
            public Exception? TaskException { get; set; }

            private bool _done;
            private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _items = new();

            public ExclusiveSynchronizationContext(Func<TTask> initialTask)
            {
                InitialTask = initialTask;
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotSupportedException("We cannot send to our same thread");
            }

            public override void Post(SendOrPostCallback d, object? state)
            {
                _items.Add((d, state));
            }

            public void EndMessageLoop()
            {
                Post(static state => ((ExclusiveSynchronizationContext<TTask, TResult>) state!)._done = true, this);
            }

            public void BeginMessageLoop()
            {
                while (!_done)
                {
                    var task = _items.Take();

                    task.Callback(task.State);
                    if (TaskException != null)
                    {
                        throw TaskException;
                    }
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

        // Placeholder for a task that returns no result
        private struct VoidTaskResult
        {
        }
    }
}
