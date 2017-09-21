// Copyright 2012-2017 Elringus (Artyom Sovetnikov). All Rights Reserved.

namespace UnityCommon
{
    #if UNITY_EDITOR
    using UnityEngine;
    
    /// <summary>
    /// The field will store reference to an asset folder.
    /// Should only be used in editor code.
    /// </summary>
    public class FolderAssetAttribute : PropertyAttribute { }
    #endif
    
}
