using System;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace SIMDArticle {
    class Program {
        static void Main(string[] args) {
            var arr = new int[] {0, 1, 2};
            var q = arr.Cast<long>().Sum();
            //BenchmarkDotNet.Running.BenchmarkRunner.Run<ArraySumBenchmark>();
            BenchmarkDotNet.Running.BenchmarkRunner.Run<ArrayEqualsBenchmark>();
            //BenchmarkDotNet.Running.BenchmarkRunner.Run<CountBenchmark>();
        }
    }
}