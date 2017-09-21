// Copyright 2012-2017 Elringus (Artyom Sovetnikov). All Rights Reserved.

namespace UnityCommon
{
    using UnityEngine;
    
    /// <summary>
    /// The field will be edited as an integer popup selection field.
    /// </summary>
    public class IntPopupAttribute : PropertyAttribute
    {
        /// <summary>
        /// Available options for the popup value.
        /// </summary>
        public int[] Values { get; private set; }
    
        public IntPopupAttribute (params int[] values)
        {
            Values = values;
        }
    }
    
}
