using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityCommon
{
    public static class LinqUtils
    {
        /// <summary>
        /// Removes last item in the list.
        /// </summary>
        public static void RemoveLastItem<T> (this List<T> list, Predicate<T> predicate = null)
        {
            if (list == null || list.Count == 0) return;

            var elementIndex = predicate == null ? list.Count - 1 : list.FindLastIndex(predicate);
            if (elementIndex >= 0)
                list.RemoveAt(elementIndex);
        }

        public static int GetArrayHashCode<T> (this T[] array)
        {
            return ArrayEqualityComparer<T>.GetHashCode(array);
        }

        public static bool IsIndexValid<T> (this T[] array, int index)
        {
            return array.Length > 0 && index >= 0 && index < array.Length;
        }

        public static bool IsIndexValid<T> (this List<T> list, int index)
        {
            return list.Count > 0 && index >= 0 && index < list.Count;
        }

        public static T[] Append<T> (this T[] array, T item)
        {
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = item;
            return array;
        }

        public static T Random<T> (this List<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            var randomIndex = UnityEngine.Random.Range(0, list.Count);
            return list[randomIndex];
        }

        public static T Random<T> (this T[] array)
        {
            if (array == null || array.Length == 0) return default(T);
            var randomIndex = UnityEngine.Random.Range(0, array.Length);
            return array[randomIndex];
        }

        public static T[] AppendRange<T> (this T[] array, T[] items)
        {
            var itemsCount = items.Length;
            Array.Resize(ref array, array.Length + itemsCount);
            for (int i = 0; i < itemsCount; i++)
                array[array.Length - (i + 1)] = items[i];
            return array;
        }

        public static IEnumerable<T> DistinctBy<T, TKey> (this IEnumerable<T> items, Func<T, TKey> property, IEqualityComparer<TKey> propertyComparer = null)
        {
            var comparer = new GeneralPropertyComparer<T, TKey>(property, propertyComparer);
            return items.Distinct(comparer);
        }

        public static float ProgressOf<T> (this List<T> list, T currentItem)
        {
            return list.IndexOf(currentItem) / (float)list.Count;
        }

        public static IList<T> Swap<T> (this IList<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }

        /// <summary>
        /// Orders the elements of <paramref name="source"/> collection in a way that no element depends on any previous element.
        /// </summary>
        /// <param name="source">The collection to order.</param>
        /// <param name="getDependencies">Function used to retrieve element's dependencies.</param>
        /// <remarks>Based on: https://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp </remarks>
        public static IList<T> TopologicalOrder<T> (this IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies)
        {
            var sorted = new List<T>();
            var visited = new Dictionary<T, bool>();

            foreach (var item in source)
                Visit(item, getDependencies, sorted, visited);

            return sorted;
        }

        private static void Visit<T> (T item, Func<T, IEnumerable<T>> getDependencies, List<T> sorted, Dictionary<T, bool> visited)
        {
            var inProcess = default(bool);
            var alreadyVisited = visited.TryGetValue(item, out inProcess);

            if (alreadyVisited)
            {
                if (inProcess) Debug.LogWarning($"Cyclic dependency found while performing topological ordering of {typeof(T).Name}.");
            }
            else
            {
                visited[item] = true;

                var dependencies = getDependencies(item);
                if (dependencies != null)
                {
                    foreach (var dependency in dependencies)
                        Visit(dependency, getDependencies, sorted, visited);
                }

                visited[item] = false;
                sorted.Add(item);
            }
        }
    }

    public class GeneralPropertyComparer<T, TKey> : IEqualityComparer<T>
    {
        private Func<T, TKey> property;
        private IEqualityComparer<TKey> propertyComparer;

        public GeneralPropertyComparer (Func<T, TKey> property, IEqualityComparer<TKey> propertyComparer = null)
        {
            this.property = property;
            this.propertyComparer = propertyComparer;
        }

        public bool Equals (T first, T second)
        {
            var firstProperty = property.Invoke(first);
            var secondProperty = property.Invoke(second);
            if (propertyComparer != null) return propertyComparer.Equals(firstProperty, secondProperty);
            if (firstProperty == null && secondProperty == null) return true;
            else if (firstProperty == null ^ secondProperty == null) return false;
            else return firstProperty.Equals(secondProperty);
        }

        public int GetHashCode (T obj)
        {
            var prop = property.Invoke(obj);
            if (propertyComparer != null) return propertyComparer.GetHashCode(prop);
            return (prop == null) ? 0 : prop.GetHashCode();
        }
    }

    /// <summary>
    /// Allows comparing arrays using equality comparer of the array items.
    /// Type of the array items should provide a valid comparer for this to work.
    /// Implementation based on: https://stackoverflow.com/a/7244729/1202251
    /// </summary>
    /// <typeparam name="T">Type of the items contained in the array.</typeparam>
    public sealed class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        private static readonly EqualityComparer<T> ITEMS_COMPARER = EqualityComparer<T>.Default;

        public static bool Equals (T[] first, T[] second)
        {
            if (first == second) return true;
            if (first == null || second == null) return false;
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
                if (!ITEMS_COMPARER.Equals(first[i], second[i])) return false;
            return true;
        }

        public static int GetHashCode (T[] array)
        {
            unchecked
            {
                if (array == null) return 0;
                var hash = 17;
                foreach (T item in array)
                    hash = hash * 31 + ITEMS_COMPARER.GetHashCode(item);
                return hash;
            }
        }

        bool IEqualityComparer<T[]>.Equals (T[] first, T[] second)
        {
            return Equals(first, second);
        }

        int IEqualityComparer<T[]>.GetHashCode (T[] array)
        {
            return GetHashCode(array);
        }
    }
}
