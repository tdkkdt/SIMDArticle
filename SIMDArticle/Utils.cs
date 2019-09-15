using System;

namespace SIMDArticle {
    public static class Utils {
        static Random rnd = new Random();

        public static byte[] GetByteArray(int size) {
            byte[] result = new byte[size];
            return result;
        }

        public static int[] GetRandomIntArray(int size) {
            var result = new int[size];
            for (int i = 0; i < size; i++) {
                result[i] = GetRandomValue();
            }
            return result;
        }

        public static int GetRandomValue() {
            return rnd.Next(-10000, 10000);
        }
    }
}