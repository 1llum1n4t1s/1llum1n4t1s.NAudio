using System;
using System.Collections.Generic;

namespace NAudio.Utils;

internal class MergeSort
{
    /// <summary>
    /// Stable MergeSort. The list is sorted in place; the merge step uses a scratch
    /// buffer the size of the left half (allocated once per top-level call), so the
    /// worst case is O(n log n) rather than the O(n^2) of shifting elements one by one.
    /// </summary>
    private static void Sort<T>(IList<T> list, int lowIndex, int highIndex, IComparer<T> comparer, T[] scratch)
    {
        if (lowIndex >= highIndex)
        {
            return;
        }

        int midIndex = (lowIndex + highIndex) / 2;

        // Partition the list into two lists and Sort them recursively
        Sort(list, lowIndex, midIndex, comparer, scratch);
        Sort(list, midIndex + 1, highIndex, comparer, scratch);

        // Merge the two sorted lists. The left run is copied aside; the right run stays
        // where it is. The write position never overtakes the unread right element, so
        // nothing unread is overwritten. Comparisons happen in exactly the same order
        // (left head vs right head) as the previous shifting merge, so the output -
        // including the relative order of equal elements - is identical.
        int leftLength = midIndex - lowIndex + 1;
        for (int i = 0; i < leftLength; i++)
        {
            scratch[i] = list[lowIndex + i];
        }

        int left = 0;
        int right = midIndex + 1;
        int write = lowIndex;

        // Until the first right element is taken, every left element is already in its
        // final position. Those writes are skipped so that an already-sorted list is never
        // written to (as with the previous shifting merge), which keeps List<T> enumerators
        // taken before sorting valid.
        while ((left < leftLength) && (right <= highIndex))
        {
            // MRH, if use < 0 sort is not stable
            if (comparer.Compare(scratch[left], list[right]) <= 0)
            {
                if (right != midIndex + 1)
                {
                    list[write] = scratch[left];
                }
                write++;
                left++;
            }
            else
            {
                list[write++] = list[right++];
            }
        }

        // Whatever remains of the left run goes back in (unless nothing moved); a remaining
        // right run is already in its final position.
        if (right != midIndex + 1)
        {
            while (left < leftLength)
            {
                list[write++] = scratch[left++];
            }
        }
    }

    private static void SortAll<T>(IList<T> list, IComparer<T> comparer)
    {
        if (list.Count < 2)
        {
            return;
        }

        // The largest left run is ceil(n / 2) elements
        var scratch = new T[(list.Count + 1) / 2];
        Sort(list, 0, list.Count - 1, comparer, scratch);
    }

    /// <summary>
    /// MergeSort a list of comparable items
    /// </summary>
    public static void Sort<T>(IList<T> list) where T : IComparable<T>
    {
        SortAll(list, Comparer<T>.Default);
    }

    /// <summary>
    /// MergeSort a list
    /// </summary>
    public static void Sort<T>(IList<T> list, IComparer<T> comparer)
    {
        SortAll(list, comparer);
    }
}
