using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.Extensions
{
    public static class Extensions
    {
        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        public static IList<T> Copy<T>(this IList<T> list)
        {
            return list.Select(l => l).ToList();
        }
    }
}