// Copyright 2012-2017 Elringus (Artyom Sovetnikov). All Rights Reserved.

namespace UnityCommon
{
    using UnityEngine;
    
    public static class MathUtils
    {
        public static bool IsEven (this int intValue)
        {
            return intValue % 2 == 0;
        }
    
        public static bool IsWithin (this int value, int minimum, int maximum)
        {
            return value >= minimum && value <= maximum;
        }
    
        public static bool IsWithin (this float value, float minimum, float maximum)
        {
            return value >= minimum && value <= maximum;
        }
    
        public static Vector3 SmoothStep (Vector3 from, Vector3 to, float progress)
        {
            var x = Mathf.SmoothStep(from.x, to.x, progress);
            var y = Mathf.SmoothStep(from.y, to.y, progress);
            var z = Mathf.SmoothStep(from.z, to.z, progress);
    
            return new Vector3(x, y, z);
        }
    
        public static bool Contains (this Rect rect, float x, float y)
        {
            return rect.Contains(new Vector2(x, y));
        }
    
        public static Rect Scale (this Rect rect, float scale)
        {
            return new Rect(rect.position * scale, rect.size * scale);
        }
    
        public static Rect Scale (this Rect rect, Vector2 scale)
        {
            return new Rect(new Vector2(rect.position.x * scale.x, rect.position.y * scale.y),
                new Vector2(rect.size.x * scale.x, rect.size.y * scale.y));
        }
    
        public static Rect Scale (this Rect rect, float scaleX, float scaleY)
        {
            return rect.Scale(new Vector2(scaleX, scaleY));
        }
    
        public static Rect Crop (this Rect rect, float cropAmount)
        {
            return new Rect(rect.position - Vector2.one * cropAmount, rect.size + Vector2.one * cropAmount * 2f);
        }
    
        public static bool IsIndexValid<T> (this T[] array, int index)
        {
            return array.Length > 0 && index >= 0 && index < array.Length;
        }
    
        public static int ToNearestEven (this int value, int upperLimit = int.MaxValue)
        {
            return (value % 2 == 0) ? value : Mathf.Min(value + 1, upperLimit);
        }
    
        /// <summary>
        /// For 'source' rectangle inside 'target' rectangle, get the maximum scale factor 
        /// that permits the 'source' rectangle to be scaled without stretching or squashing.
        /// </summary>
        /// <param name="targetSize">Size of the rectangle to be scaled to.</param>
        /// <param name="sourceSize">Size of the rectangle to scale.</param>
        /// <returns>Maximum scale factor preserving aspect ratio.</returns>
        public static float MaxScaleKeepAspect (Vector2 targetSize, Vector2 sourceSize)
        {
            var targetAspect = targetSize.x / targetSize.y;
            var sourceAspect = sourceSize.x / sourceSize.y;
    
            if (targetAspect > sourceAspect)
                return targetSize.y / sourceSize.y;
            return targetSize.x / sourceSize.x;
        }
    }
    
}
