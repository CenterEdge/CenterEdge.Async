using System;

namespace CenterEdge.Async;

internal class ThrowHelper
{
    public static void ThrowArgumentNullException(string paramName) =>
        throw new ArgumentNullException(paramName);
}
