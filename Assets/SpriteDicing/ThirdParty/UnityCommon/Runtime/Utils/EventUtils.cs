// Copyright 2012-2017 Elringus (Artyom Sovetnikov). All Rights Reserved.

namespace UnityCommon
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using System.Collections.Generic;
    using UnityEngine.Events;
    
    public static class EventUtils
    {
        /// <summary>
        /// Get top-most hovered gameobject.
        /// </summary>
        public static GameObject GetHoveredGameObject (this EventSystem eventSystem)
        {
            var pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
    
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);
    
            if (raycastResults.Count > 0)
                return raycastResults[0].gameObject;
            else return null;
        }
    
        public static void SafeInvoke (this UnityAction action)
        {
            if (action != null) action.Invoke();
        }
    
        public static void SafeInvoke<T0> (this UnityAction<T0> action, T0 arg0)
        {
            if (action != null) action.Invoke(arg0);
        }
    
        public static void SafeInvoke<T0, T1> (this UnityAction<T0, T1> action, T0 arg0, T1 arg1)
        {
            if (action != null) action.Invoke(arg0, arg1);
        }
    
        public static void SafeInvoke<T0, T1, T2> (this UnityAction<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
        {
            if (action != null) action.Invoke(arg0, arg1, arg2);
        }
    }
    
}
