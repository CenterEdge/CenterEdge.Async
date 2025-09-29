using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CenterEdge.Async;

/// <summary>
/// Executes asynchronous tasks synchronously.
/// </summary>
/// <remarks>
/// DO NOT use this class unless absolutely necessary. Calling async code from sync code is an anti-pattern
/// in most cases. This class is provided to assist in gradual conversion from sync to async code.
/// </remarks>
public static class AsyncHelper
{
    // ValueTask-based overloads include OverloadResolutionPriority(-1) so that C# 13 and later will prefer the Task-based
    // overloads by default when both are applicable. This occurs when passing an async lambda directly to the RunSync method
    // without an explicit return type, for example:
    //
    //     AsyncHelper.RunSync(async () =>
    //     {
    //         await DoSomething();
    //         await DoSomethingElse();
    //     });

    // If there is no SynchronizationContext or if the current SynchronizationContext is the default one, and if there
    // is no custom TaskScheduler, then it is safe to run the task directly without risk of deadlock. This reduces
    // overhead and improves the speed of continuations because we don't need to use the ExclusiveSynchronizationContext.
    // Also, this is particularly valuable in cases where the thread being blocked is a thread pool thread. Modern .NET
    // includes optimizations which reduce the risk of thread pool depletion and otherwise improves performance when
    // waiting on a Task from a thread pool thread, something the ExclusiveSynchronizationContext cannot replicate.
    private static bool IsDeadlockSafe(SynchronizationContext? currentSynchronizationContext) =>
        (currentSynchronizationContext is null || currentSynchronizationContext.GetType() == typeof(SynchronizationContext))
            && ReferenceEquals(TaskScheduler.Current, TaskScheduler.Default);

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
        RunSync(static state => state.Invoke(), task);
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

        if (IsDeadlockSafe(oldContext))
        {
            task(state).GetAwaiter().GetResult();
            return;
        }

        using var synch = new ExclusiveSynchronizationContext(oldContext);
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
    [OverloadResolutionPriority(-1)]
    public static void RunSync(Func<ValueTask> task)
    {
        RunSync(static state => state.Invoke(), task);
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
    [OverloadResolutionPriority(-1)]
    public static void RunSync<TState>(Func<TState, ValueTask> task, TState state)
    {
        var oldContext = SynchronizationContext.Current;

        if (IsDeadlockSafe(oldContext))
        {
            task(state).AsTask().GetAwaiter().GetResult();
            return;
        }

        using var synch = new ExclusiveSynchronizationContext(oldContext);
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
        return RunSync(static state => state.Invoke(), task);
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

        if (IsDeadlockSafe(oldContext))
        {
            return task(state).GetAwaiter().GetResult();
        }

        using var synch = new ExclusiveSynchronizationContext(oldContext);
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
    [OverloadResolutionPriority(-1)]
    public static T RunSync<T>(Func<ValueTask<T>> task)
    {
        return RunSync(static state => state.Invoke(), task);
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
    [OverloadResolutionPriority(-1)]
    public static T RunSync<T, TState>(Func<TState, ValueTask<T>> task, TState state)
    {
        var oldContext = SynchronizationContext.Current;

        if (IsDeadlockSafe(oldContext))
        {
            return task(state).AsTask().GetAwaiter().GetResult();
        }

        using var synch = new ExclusiveSynchronizationContext(oldContext);
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
}
