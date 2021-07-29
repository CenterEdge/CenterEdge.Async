using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace CenterEdge.Async.Benchmarks
{
    public class TaskVsValueTask
    {
        [Benchmark(Baseline = true)]
        public int Task()
        {
            return AsyncHelper.RunSync(AsyncTaskMethod);
        }

        [Benchmark]
        public int ValueTask()
        {
            return AsyncHelper.RunSync(AsyncValueTaskMethod);
        }

        private async Task<int> AsyncTaskMethod()
        {
            var i = 1;
            await System.Threading.Tasks.Task.Yield();

            i++;

            return i;
        }

        private async ValueTask<int> AsyncValueTaskMethod()
        {
            var i = 1;
            await System.Threading.Tasks.Task.Yield();

            i++;

            return i;
        }
    }
}
