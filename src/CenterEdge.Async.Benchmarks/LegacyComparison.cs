using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace CenterEdge.Async.Benchmarks
{
    public class LegacyComparison
    {
        [Benchmark]
        public int New()
        {
            return AsyncHelper.RunSync(AsyncMethod);
        }

        [Benchmark(Baseline = true)]
        public int Legacy()
        {
            return LegacyAsyncHelper.RunSync(AsyncMethod);
        }

        private async Task<int> AsyncMethod()
        {
            var i = 1;
            await Task.Yield();

            i++;

            return i;
        }
    }
}
