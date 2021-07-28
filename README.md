# CenterEdge.Async

When you gotta, you gotta.

Don't use this library unless you gotta. Running async code from sync code tends
to result in deadlocks, thread pool depletion, poor performance, and unexpected
behaviors. However, when gradually converting code from sync to async sometimes
you must to keep the scope of work under control.

## Usage

```cs

// Basic usage, function may return Task, Task<T>, ValueTask, or ValueTask<T>
var result = AsyncHelper.RunSync(() => SomeFunctionAsync(param1, param2));

```
