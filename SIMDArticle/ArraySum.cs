using System;
using System.Linq;
using System.Numerics;
#if NETCOREAPP3_0
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif
using BenchmarkDotNet.Attributes;
using NUnit.Framework;

namespace SIMDArticle {
    [RyuJitX64Job, DisassemblyDiagnoser]
    public class ArraySumBenchmark {
        [Params(10, 100, 1000, 10000, 100000)]
        public int ItemsCount { get; set; }

        public int[] Array { get; set; }

        [IterationSetup]
        public void IterationSetup() {
            Random rnd = new Random(31337);
            Array = new int[ItemsCount];
            for (int i = 0; i < ItemsCount; i++) {
                Array[i] = rnd.Next(-10000, 10000);
            }
        }

        [Benchmark(Baseline = true)]
        public int Naive() {
            int result = 0;
            foreach (int i in Array) {
                result += i;
            }
            return result;
        }

        [Benchmark]
        public long LINQ() => Array.Aggregate<int, long>(0, (current, i) => current + i);

        [Benchmark]
        public int Vectors() {
            int vectorSize = Vector<int>.Count;
            var accVector = Vector<int>.Zero;
            int i;
            var array = Array;
            for (i = 0; i < array.Length - vectorSize; i += vectorSize) {
                var v = new Vector<int>(array, i);
                accVector = Vector.Add(accVector, v);
            }
            int result = Vector.Dot(accVector, Vector<int>.One);
            for (; i < array.Length; i++) {
                result += array[i];
            }
            return result;
        }

#if NETCOREAPP3_0
        [Benchmark]
        public unsafe int Intrinsics() {
            int vectorSize = 256 / 8 / 4;
            var accVector = Vector256<int>.Zero;
            int i;
            var array = Array;
            fixed (int* ptr = array) {
                for (i = 0; i < array.Length - vectorSize; i += vectorSize) {
                    var v = Avx2.LoadVector256(ptr + i);
                    accVector = Avx2.Add(accVector, v);
                }
            }
            int result = 0;
            var temp = stackalloc int[vectorSize];
            Avx2.Store(temp, accVector);
            for(int j = 0; j < vectorSize; j++) {
                result += temp[j];
            }
            for (; i < array.Length; i++) {
                result += array[i];
            }
            return result;
        }
#endif

    }

    [TestFixture]
    public class ArraySumTests {
        [Test]
        public void Sum10() {
            TestHelper(10);
        }

        [Test]
        public void Sum100() {
            TestHelper(100);
        }

        [Test]
        public void Sum1000() {
            TestHelper(1000);
        }

        [Test]
        public void Sum10000() {
            TestHelper(10000);
        }

        [Test]
        public void Sum100000() {
            TestHelper(100000);
        }

        static void TestHelper(int itemsCount) {
            var arraySumBenchmark = new ArraySumBenchmark();
            arraySumBenchmark.ItemsCount = itemsCount;
            arraySumBenchmark.IterationSetup();
            long naive = arraySumBenchmark.Naive();
            Assert.AreEqual(naive, arraySumBenchmark.LINQ());
            Assert.AreEqual(naive, arraySumBenchmark.Vectors());
#if NETCOREAPP3_0
            Assert.AreEqual(naive, arraySumBenchmark.Intrinsics());
#endif
        }
    }
}