using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
#if NETCOREAPP3_0
using System.Runtime.Intrinsics.X86;
#endif
using BenchmarkDotNet.Attributes;
using NUnit.Framework;

namespace SIMDArticle {
    [RyuJitX64Job]
    public class ArrayEqualsBenchmark {
        [Params(10000, 100000, 1000000)]
        public int ItemsCount { get; set; }

        public byte[] ArrayA { get; set; }
        public byte[] ArrayB { get; set; }

        [GlobalSetup]
        public void GlobalSetup() {
            ArrayA = Utils.GetByteArray(ItemsCount);
            ArrayB = Utils.GetByteArray(ItemsCount);
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
            for (; i <= ArrayA.Length - vectorSize; i += vectorSize) {
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
//        [Benchmark]
//        public unsafe bool FastestIntrinsics() {
//            const int vectorSize = 256 / 8;
//            int i = 0;
//            var arrayA = ArrayA;
//            var arrayB = ArrayB;
//            int length = arrayA.Length;
//            fixed (byte* ptrA = arrayA)
//            fixed (byte* ptrB = arrayB) {
//                int l = length - vectorSize;
//                for (; i <= l; i += vectorSize) {
//                    if (Avx2.MoveMask(Avx2.CompareEqual(Avx2.LoadVector256(ptrA + i), Avx2.LoadVector256(ptrB + i))) != unchecked((int) 0b11111111111111111111111111111111)) {
//                        return false;
//                    }
//                }
//                int rem = length & (vectorSize - 1);
//                int d = rem >> 3;
//                switch (d) {
//                    case 0b11: {
//                        if (*(long*) (ptrA + i) != *(long*) (ptrB + i) ||
//                            *(long*) (ptrA + i + 8) != *(long*) (ptrB + i + 8) ||
//                            *(long*) (ptrA + i + 16) != *(long*) (ptrB + i + 16)) {
//                            return false;
//                        }
//                        i += 24;
//                        break;
//                    }
//                    case 0b10: {
//                        if (*(long*) (ptrA + i) != *(long*) (ptrB + i) ||
//                            *(long*) (ptrA + i + 8) != *(long*) (ptrB + i + 8)) {
//                            return false;
//                        }
//                        i += 16;
//                        break;
//                    }
//                    case 0b01: {
//                        if (*(long*) (ptrA + i) != *(long*) (ptrB + i)) {
//                            return false;
//                        }
//                        i += 8;
//                        break;
//                    }
//                }
//                rem &= 0b111;
//                switch (rem) {
//                    case 7: {
//                        return (*(int*) (ptrA + i) == *(int*) (ptrB + i) &&
//                                *(short*) (ptrA + i + 4) == *(short*) (ptrB + i + 4) &&
//                                *(ptrA + i + 6) == *(ptrB + i + 6));
//                    }
//                    case 6: {
//                        return (*(int*) (ptrA + i) == *(int*) (ptrB + i) &&
//                                *(short*) (ptrA + i + 4) == *(short*) (ptrB + i + 4));
//                    }
//                    case 5: {
//                        return (*(int*) (ptrA + i) == *(int*) (ptrB + i) &&
//                                *(ptrA + i + 4) == *(ptrB + i + 4));
//                    }
//                    case 4: {
//                        return (*(int*) (ptrA + i) == *(int*) (ptrB + i));
//                    }
//                    case 3: {
//                        return *(short*) (ptrA + i) == *(short*) (ptrB + i) &&
//                               *(ptrA + i + 2) == *(ptrB + i + 2);
//                    }
//                    case 2: {
//                        return *(short*) (ptrA + i) == *(short*) (ptrB + i);
//                    }
//                    case 1: {
//                        return *(ptrA + i) == *(ptrB + i);
//                    }
//                }
//                return true;
//            }
//        }
        
        
        [Benchmark]
        public unsafe bool Intrinsics() {
            int vectorSize = 256 / 8;
            int i = 0;
            const int equalsMask = unchecked((int) (0b1111_1111_1111_1111_1111_1111_1111_1111));
            fixed (byte* ptrA = ArrayA)
            fixed (byte* ptrB = ArrayB) {
                for (; i <= ArrayA.Length - vectorSize; i += vectorSize) {
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

        [Test]
        public void Test99999() {
            var arrayEquals = new ArrayEqualsBenchmark();
            arrayEquals.ItemsCount = 99999;
            arrayEquals.GlobalSetup();
            CheckEqualsTrue(arrayEquals);
            for (int i = 0; i < 31; i++) {
                int index0 = 99999 - 1 - i;
                arrayEquals.ArrayA[index0] = 1;
                CheckEqualsFalse(arrayEquals);
                arrayEquals.ArrayB[index0] = 1;
                CheckEqualsTrue(arrayEquals);
            }
        }

        static void TestHelper(int itemsCount) {
            var arrayEquals = new ArrayEqualsBenchmark();
            arrayEquals.ItemsCount = itemsCount;
            arrayEquals.GlobalSetup();
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