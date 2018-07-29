using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityCommon
{
    public struct FolderAsset<T>
    {
        public readonly string Name, Path;
        public readonly T Object;
        public AssetImporter Importer { get { return AssetImporter.GetAtPath(Path); } }

        public FolderAsset (string name, string path, T @object)
        {
            Name = name;
            Path = path;
            Object = @object;
        }
    }

    /// <summary>
    /// Provides utils to work with folder assets.
    /// Should only be used in editor code.
    /// </summary>
    public class FolderAssetHelper
    {
        public Object FolderObject { get; private set; }
        public string Path { get { return AssetDatabase.GetAssetPath(FolderObject); } }
        public string FullPath { get { return Application.dataPath.GetBefore("Assets") + Path; } }

        public FolderAssetHelper (Object folderObject)
        {
            FolderObject = folderObject;
            if (FolderObject == null || !AssetDatabase.IsValidFolder(Path))
            {
                Debug.LogError(string.Format("Object '{0}' is not a folder.", FolderObject ? FolderObject.name : "null"));
                return;
            }
        }

        public FolderAssetHelper (string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var folderGuid = AssetDatabase.CreateFolder(path.GetBeforeLast("/"), path.GetAfter("/"));
                var folderAssetPath = AssetDatabase.GUIDToAssetPath(folderGuid);
                Debug.Assert(AssetDatabase.IsValidFolder(folderAssetPath));
                FolderObject = AssetDatabase.LoadAssetAtPath<Object>(folderAssetPath);
            }
            else FolderObject = AssetDatabase.LoadAssetAtPath<Object>(path);
        }

        public List<T> SetContainedAssets<T> (List<T> objectsToSave) where T : Object
        {
            var assetsToDestroy = LoadContainedAssets<T>()
                .Where(containedAsset => !objectsToSave.Exists(objectToSave => objectToSave.name == containedAsset.Object.name)).ToList();
            assetsToDestroy.ForEach(assetToDestroy => AssetDatabase.DeleteAsset(assetToDestroy.Path));

            var savedObjects = new List<T>();
            objectsToSave.ForEach(objectToSave => savedObjects.Add(objectToSave.CreateOrReplaceAsset<T>(Path + "/" + objectToSave.name + ".asset")));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return savedObjects;
        }

        /// <summary>
        /// Loads asset objects contained in the current folder.
        /// </summary>
        /// <typeparam name="T">Type of the asset objects to load. Use 'UnityEngine.Object' to load any of them.</typeparam>
        /// <param name="includeSubfolders">Whether to load assets inside subfolders of the current folder.</param>
        /// <param name="prependSubfolderNames">Whether to prepend asset names with the subfolder name. Eg: SubfolderName.AssetName</param>
        public List<FolderAsset<T>> LoadContainedAssets<T> (bool includeSubfolders = false, bool prependSubfolderNames = false) where T : Object
        {
            return new HashSet<string>(AssetDatabase.FindAssets("", new[] { Path }))
                .Select(assetGuid => AssetDatabase.GUIDToAssetPath(assetGuid))
                .Select(assetPath => new FolderAsset<T>(
                    ExtractAssetName(assetPath, prependSubfolderNames), assetPath,
                    AssetDatabase.LoadAssetAtPath<Object>(assetPath) as T))
                .Where(folderAsset => folderAsset.Object != null && (includeSubfolders || !folderAsset.Path.GetAfter(Path + "/").Contains("/")))
                .ToList();
        }

        private string ExtractAssetName (string assetPath, bool prependSubfolderNames)
        {
            var pathWithoutExtension = assetPath.Replace("." + assetPath.GetAfter("."), string.Empty);
            if (prependSubfolderNames) return pathWithoutExtension.GetAfter(Path + "/").Replace("/", ".");
            else return pathWithoutExtension.GetAfter("/");
        }
    }
}
