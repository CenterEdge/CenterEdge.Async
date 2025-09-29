using System.Threading;

namespace CenterEdge.Async;

internal readonly struct WorkItem(SendOrPostCallback callback, object? state)
{
    public readonly SendOrPostCallback Callback = callback;
    public readonly object? State = state;

    public void Deconstruct(out SendOrPostCallback callback, out object? state)
    {
        callback = Callback;
        state = State;
    }
}
