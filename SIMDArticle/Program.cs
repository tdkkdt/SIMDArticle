using System;
using System.Runtime.Intrinsics.X86;

namespace SIMDArticle {
    class Program {
        static void Main(string[] args) {
            //BenchmarkDotNet.Running.BenchmarkRunner.Run<ArraySumBenchmark>();
            //BenchmarkDotNet.Running.BenchmarkRunner.Run<ArrayEqualsBenchmark>();
            BenchmarkDotNet.Running.BenchmarkRunner.Run<CountBenchmark>();
        }
    }
}