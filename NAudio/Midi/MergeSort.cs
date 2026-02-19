using System;
using System.Buffers;
using System.Collections.Generic;

namespace NAudio.Utils
{
    class MergeSort
    {
        /// <summary>
        /// Stable MergeSort using a temporary buffer for O(n log n) performance.
        /// </summary>
        static void Sort<T>(IList<T> list, int lowIndex, int highIndex, IComparer<T> comparer, T[] buffer)
        {
            if (lowIndex >= highIndex)
            {
                return;
            }

            var midIndex = (lowIndex + highIndex) / 2;

            Sort(list, lowIndex, midIndex, comparer, buffer);
            Sort(list, midIndex + 1, highIndex, comparer, buffer);

            // If already merged (left max <= right min), skip merge
            if (comparer.Compare(list[midIndex], list[midIndex + 1]) <= 0)
            {
                return;
            }

            // Merge using temporary buffer for O(n) merge instead of O(n^2) shifting
            var leftLen = midIndex - lowIndex + 1;
            for (var i = 0; i < leftLen; i++)
            {
                buffer[i] = list[lowIndex + i];
            }

            var leftIdx = 0;
            var rightIdx = midIndex + 1;
            var writeIdx = lowIndex;

            while (leftIdx < leftLen && rightIdx <= highIndex)
            {
                // Use <= for stability: equal elements from left half come first
                if (comparer.Compare(buffer[leftIdx], list[rightIdx]) <= 0)
                {
                    list[writeIdx++] = buffer[leftIdx++];
                }
                else
                {
                    list[writeIdx++] = list[rightIdx++];
                }
            }

            // Copy remaining left elements (right elements are already in place)
            while (leftIdx < leftLen)
            {
                list[writeIdx++] = buffer[leftIdx++];
            }
        }

        /// <summary>
        /// MergeSort a list of comparable items
        /// </summary>
        public static void Sort<T>(IList<T> list) where T : IComparable<T>
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            Sort(list, Comparer<T>.Default);
        }

        /// <summary>
        /// MergeSort a list
        /// </summary>
        public static void Sort<T>(IList<T> list, IComparer<T> comparer)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            var count = list.Count;
            if (count <= 1) return;

            var buffer = ArrayPool<T>.Shared.Rent(count);
            try
            {
                Sort(list, 0, count - 1, comparer, buffer);
            }
            finally
            {
                ArrayPool<T>.Shared.Return(buffer, clearArray: true);
            }
        }
    }
}
