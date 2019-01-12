using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
#if NETCOREAPP3_0
using System.Runtime.Intrinsics.X86;
#endif
using BenchmarkDotNet.Attributes;
using NUnit.Framework;

namespace SIMDArticle {
    [RyuJitX64Job, DisassemblyDiagnoser]
    public class ArrayEqualsBenchmark {
        [Params(10000, 100000, 1000000)]
        public int ItemsCount { get; set; }

        public byte[] ArrayA { get; set; }
        public byte[] ArrayB { get; set; }

        [IterationSetup]
        public void IterationSetup() {
            Random rnd = new Random(31337);
            ArrayA = new byte[ItemsCount];
            ArrayB = new byte[ItemsCount];
        }

        [Benchmark(Baseline = true)]
        public bool Naive() {
            for (int i = 0; i < ArrayA.Length; i++) {
                if (ArrayA[i] != ArrayB[i]) return false;
            }
            return true;
        }

        [Benchmark]
        public bool LINQ() => ArrayA.SequenceEqual(ArrayB);

        [Benchmark]
        public bool Vectors() {
            int vectorSize = Vector<byte>.Count;
            int i = 0;
            for (; i < ArrayA.Length - vectorSize; i += vectorSize) {
                var va = new Vector<byte>(ArrayA, i);
                var vb = new Vector<byte>(ArrayB, i);
                if (!Vector.EqualsAll(va, vb)) {
                    return false;
                }
            }
            for (; i < ArrayA.Length; i++) {
                if (ArrayA[i] != ArrayB[i])
                    return false;
            }
            return true;
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);

        [Benchmark]
        public bool MemCmp() => memcmp(ArrayA, ArrayB, ArrayA.Length) == 0;

#if NETCOREAPP3_0
        [Benchmark]
        public unsafe bool Intrinsics() {
            int vectorSize = 256 / 8;
            int i = 0;
            const int equalsMask = unchecked((int) (0b1111_1111_1111_1111_1111_1111_1111_1111));
            fixed (byte* ptrA = ArrayA)
            fixed (byte* ptrB = ArrayB) {
                for (; i < ArrayA.Length - vectorSize; i += vectorSize) {
                    var va = Avx2.LoadVector256(ptrA + i);
                    var vb = Avx2.LoadVector256(ptrB + i);
                    var areEqual = Avx2.CompareEqual(va, vb);
                    if (Avx2.MoveMask(areEqual) != equalsMask) {
                        return false;
                    }
                }
                for (; i < ArrayA.Length; i++) {
                    if (ArrayA[i] != ArrayB[i])
                        return false;
                }
                return true;
            }
        }
#endif
    }

    [TestFixture]
    public class ArrayEqualsTests {
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
            var arrayEquals = new ArrayEqualsBenchmark();
            arrayEquals.ItemsCount = itemsCount;
            arrayEquals.IterationSetup();
            CheckEqualsTrue(arrayEquals);
            arrayEquals.ArrayA[0] = 1;
            CheckEqualsFalse(arrayEquals);
            arrayEquals.ArrayA[0] = 0;
            arrayEquals.ArrayB[itemsCount - 1] = 1;
            CheckEqualsFalse(arrayEquals);
        }

        static void CheckEqualsFalse(ArrayEqualsBenchmark arrayEquals) {
            Assert.IsFalse(arrayEquals.Naive());
            Assert.IsFalse(arrayEquals.LINQ());
            Assert.IsFalse(arrayEquals.Vectors());
            Assert.IsFalse(arrayEquals.MemCmp());
#if NETCOREAPP3_0
            Assert.IsFalse(arrayEquals.Intrinsics());
#endif
        }

        static void CheckEqualsTrue(ArrayEqualsBenchmark arrayEquals) {
            Assert.IsTrue(arrayEquals.Naive());
            Assert.IsTrue(arrayEquals.LINQ());
            Assert.IsTrue(arrayEquals.Vectors());
            Assert.IsTrue(arrayEquals.MemCmp());
#if NETCOREAPP3_0
            Assert.IsTrue(arrayEquals.Intrinsics());
#endif
        }
    }
}