# CenterEdge.Async

When you gotta, you gotta.

Don't use this library unless you gotta. Running async code from sync code tends
to result in deadlocks, thread pool depletion, poor performance, and unexpected
behaviors. However, when gradually converting code from sync to async sometimes
you must to keep the scope of work under control.

## Usage

```cs
// Basic usage for an action running Task or ValueTask
AsyncHelper.RunSync(() => SomeFunctionAsync(param1, param2));

// Basic usage for an action running Task<T> or ValueTask<T>
var result = AsyncHelper.RunSync(() => SomeFunctionAsync(param1, param2));

// Pass in state as a parameter to reduce heap allocations due to closures
AsyncHelper.RunSync(state => SomeFunctionAsync(state, true), param1);
// or you can use a method group if parameters align precisely
AsyncHelper.RunSync(SomeFunctionAsyncWithOneParameter, param1);
```

## Background

This project is based off [this post](https://social.msdn.microsoft.com/Forums/en-US/163ef755-ff7b-4ea5-b226-bbe8ef5f4796/is-there-a-pattern-for-calling-an-async-method-synchronously?forum=async).
The original flavor had the following behaviors:

- The calling thread is blocked until the asynchronous work is completed
- Basic `await` operations will run the continuation on the calling thread
  - Awaiting with `.ContinueAwait(false)` may run the continuations on the thread pool. However,
    this is not guaranteed, the continuation may be executed synchronously if the awaited task
    is returned already complete.
- The result of the asynchronous task is returned, if applicable
- Exceptions returned by the asynchronous task will be thrown

## Improvements

This implementation offers the following improvements over the original implementation:

- Support for direct usage of `ValueTask` and `ValueTask<T>`. This avoids the need to call `.AsTask()`
  and offers better performance in the case where the `ValueTask` is returned already completed.
- Exceptions are thrown with a more meaningful stack trace by using `.GetResult()` on the awaiter.
- `SynchronizationContext.Current` is now correctly reset if the asynchronous work returns an exception.
  - This can prevent some esoteric deadlock scenarios, especially in cases like WinForms or WPF which use
    a single long-lived synchronization context.
- Asynchronous work left running after the main task completes might never execute their continuations
  unless they were awaited with `.ContinueAwait(false)`. These continuations will now be run on the
  `SynchronizationContext` captured at the time `RunSync` is called.
- `Dispose` is used to perform cleanup which can reduce Gen1 and Gen2 garbage collections on objects with finalizers.
- Significant overall performance improvements.
  - For example, benchmarks are showing a 78% improvement on .NET 4.8 running in x86 (75% in x64).

## Limitations

This implementation still has several limitations. As always, writing truly asynchronous code is the
best approach.

- Work will still be single-threaded in most cases, potentially reducing performance.
- The calling thread is blocked, which can lead to thread pool depletion issues.
- In scenarios where many calls to `RunSync` are running at once in different threads,
  the ThreadPool may need to scale up to a larger size which can reduce performance due
  to context switching.
- RunSync itself adds some additional overhead, though this implementation has less impact than
  the original.

## Running Benchmarks

```sh
cd src/CenterEdge.Async.Benchmarks

# x64
dotnet run -c Release -f net8.0 -- job default --runtimes net48 net6.0 net8.0

# x86, .NET 4.8 only
dotnet run -c Release -f net8.0 -- job default --runtimes net48 --platform x86
```
