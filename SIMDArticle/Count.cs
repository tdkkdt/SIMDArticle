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
    public class CountBenchmark {
        [Params(10, 100, 1000, 10000, 100000, 1000000)]
        public int ItemsCount { get; set; }

        public int Item { get; set; }

        public int[] Array { get; set; }

        [IterationSetup]
        public void IterationSetup() {
            Random rnd = new Random(31337);
            Array = new int[ItemsCount];
            for (int i = 0; i < ItemsCount; i++) {
                Array[i] = rnd.Next(-10000, 10000);
            }
            Item = rnd.Next(-10000, 10000);
        }

        [Benchmark(Baseline = true)]
        public int Naive() {
            int result = 0;
            foreach (int i in Array) {
                if (i == Item) {
                    result++;
                }
            }
            return result;
        }

        [Benchmark]
        public int LINQ() => Array.Count(i => i == Item);

        [Benchmark]
        public int Vectors() {
            var mask = new Vector<int>(Item);
            int vectorSize = Vector<int>.Count;
            var accResult = new Vector<int>();
            int i;
            var array = Array;
            for (i = 0; i < array.Length - vectorSize; i += vectorSize) {
                var v = new Vector<int>(array, i);
                var areEqual = Vector.Equals(v, mask);
                accResult = Vector.Subtract(accResult, areEqual);
            }
            int result = 0;
            for (; i < array.Length; i++) {
                if (array[i] == Item) {
                    result++;
                }
            }
            result += Vector.Dot(accResult, Vector<int>.One);
            return result;
        }

#if NETCOREAPP3_0
        [Benchmark]
        public unsafe int Intrinsics() {
            int vectorSize = 256 / 8 / 4;
            //var mask = Avx2.SetAllVector256(Item);
            //var mask = Avx2.SetVector256(Item, Item, Item, Item, Item, Item, Item, Item);
            var temp = stackalloc int[vectorSize];
            for (int j = 0; j < vectorSize; j++) {
                temp[j] = Item;
            }
            var mask = Avx2.LoadVector256(temp);
            var accVector = Vector256<int>.Zero;
            int i;
            var array = Array;
            fixed (int* ptr = array) {
                for (i = 0; i < array.Length - vectorSize; i += vectorSize) {
                    var v = Avx2.LoadVector256(ptr + i);
                    var areEqual = Avx2.CompareEqual(v, mask);
                    accVector = Avx2.Subtract(accVector, areEqual);
                }
            }
            int result = 0;
            Avx2.Store(temp, accVector);
            for(int j = 0; j < vectorSize; j++) {
                result += temp[j];
            }
            for(; i < array.Length; i++) {
                if (array[i] == Item) {
                    result++;
                }
            }
            return result;
        }
#endif

    }

    [TestFixture]
    public class CountTests {
        [Test]
        public void Test10() {
            TestHelper(10);
        }

        [Test]
        public void Test100() {
            TestHelper(100);
        }

        [Test]
        public void Test1000() {
            TestHelper(1000);
        }

        [Test]
        public void Test10000() {
            TestHelper(10000);
        }

        [Test]
        public void Test100000() {
            TestHelper(100000);
        }

        static void TestHelper(int itemsCount) {
            var countBenchmark = new CountBenchmark();
            countBenchmark.ItemsCount = itemsCount;
            countBenchmark.IterationSetup();
            int naive = countBenchmark.Naive();
            Assert.AreEqual(naive, countBenchmark.LINQ());
            Assert.AreEqual(naive, countBenchmark.Vectors());
#if NETCOREAPP3_0
            Assert.AreEqual(naive, countBenchmark.Intrinsics());
#endif
        }

        [Test]
        public void HardTest() {
            var countBenchmark = new CountBenchmark();
            const int count = 1000000;
            countBenchmark.ItemsCount = count;
            countBenchmark.IterationSetup();
            for (int i = 0; i < countBenchmark.ItemsCount; i += 8) {
                for (int j = 0; j < 8; j++) {
                    countBenchmark.Array[i + j] = 0;
                }
                countBenchmark.Array[i + 5] = 31337;
                countBenchmark.Array[i + 3] = 31337;
            }
            countBenchmark.Item = 31337;
            Assert.AreEqual(250000, countBenchmark.Naive());
            Assert.AreEqual(250000, countBenchmark.LINQ());
            Assert.AreEqual(250000, countBenchmark.Vectors());
#if NETCOREAPP3_0
            Assert.AreEqual(250000, countBenchmark.Intrinsics());
#endif
        }
    }
}