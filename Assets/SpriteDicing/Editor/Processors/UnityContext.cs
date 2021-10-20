using System;
using System.Threading;
using UnityEditor;

namespace SpriteDicing
{
    /// <summary>
    /// Allows executing tasks on Unity editor thread.
    /// </summary>
    public static class UnityContext
    {
        private static SynchronizationContext unityContext;
        private static bool synced => SynchronizationContext.Current == unityContext;

        /// <summary>
        /// Invokes provided action on Unity thread and waits for completion.
        /// </summary>
        public static void Invoke (Action action)
        {
            if (synced) action.Invoke();
            else unityContext.Send(_ => action.Invoke(), null);
        }

        /// <summary>
        /// Invokes provided function on Unity thread and waits for the result.
        /// </summary>
        public static T Invoke<T> (Func<T> function)
        {
            if (synced) return function.Invoke();
            var result = default(T);
            unityContext.Send(_ => result = function.Invoke(), null);
            return result;
        }

        /// <summary>
        /// Invokes provided action on Unity thread, but doesn't wait for completion.
        /// </summary>
        public static void InvokeAsync (Action action)
        {
            if (synced) action.Invoke();
            else unityContext.Post(_ => action.Invoke(), null);
        }

        [InitializeOnLoadMethod]
        private static void AcquireUnityContext ()
        {
            unityContext = SynchronizationContext.Current;
        }
    }
}
