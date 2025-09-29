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
