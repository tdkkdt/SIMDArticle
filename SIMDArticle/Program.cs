using System;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace SIMDArticle {
    class Program {
        static void Main(string[] args) {
//            BenchmarkDotNet.Running.BenchmarkRunner.Run<ArraySumBenchmark>();
//            BenchmarkDotNet.Running.BenchmarkRunner.Run<ArrayEqualsBenchmark>();
            BenchmarkDotNet.Running.BenchmarkRunner.Run<CountBenchmark>();
        }
    }
}