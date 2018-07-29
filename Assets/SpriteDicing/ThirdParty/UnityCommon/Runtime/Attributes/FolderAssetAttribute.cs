#if UNITY_EDITOR
using UnityEngine;

namespace UnityCommon
{
    /// <summary>
    /// The field will store reference to an asset folder.
    /// Should only be used in editor code.
    /// </summary>
    public class FolderAssetAttribute : PropertyAttribute { }
}
#endif
