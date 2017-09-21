// Copyright 2012-2017 Elringus (Artyom Sovetnikov). All Rights Reserved.

namespace UnityCommon
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    
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
    
        public static IEnumerable<T> DistinctBy<T, TKey> (this IEnumerable<T> items, Func<T, TKey> property, IEqualityComparer<TKey> propertyComparer = null)
        {
            var comparer = new GeneralPropertyComparer<T, TKey>(property, propertyComparer);
            return items.Distinct(comparer);
        }
    
        public static float ProgressOf<T> (this List<T> list, T currentItem)
        {
            return list.IndexOf(currentItem) / (float)list.Count;
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
