using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static SpriteDicing.Editors.EditorProperties;

namespace SpriteDicing.Editors
{
    public class DicedSpriteSerializer
    {
        private readonly SerializedObject serializedObject;
        private readonly DicedSpriteAtlas target;
        private readonly string atlasPath;

        public DicedSpriteSerializer (SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            target = serializedObject.targetObject as DicedSpriteAtlas;
            atlasPath = AssetDatabase.GetAssetPath(target);
        }

        public void Serialize (IEnumerable<Sprite> sprites)
        {
            var newSprites = new List<Sprite>(sprites);
            if (DecoupleSpriteData) SerializeDecoupled(newSprites);
            else SerializeEmbedded(newSprites);
            SetSpritesProperty(newSprites);
            AssetDatabase.SaveAssets();
        }

        private void SerializeDecoupled (List<Sprite> newSprites)
        {
            DeleteEmbeddedSprites();
            var folderPath = GetOrCreateGeneratedSpritesFolder();
            foreach (var path in Directory.GetFiles(folderPath, "*.asset", SearchOption.TopDirectoryOnly))
                UpdateExistingSprite(path);
            foreach (var sprite in newSprites)
                if (!AssetDatabase.IsMainAsset(sprite))
                    AssetDatabase.CreateAsset(sprite, Path.Combine(folderPath, $"{sprite.name}.asset"));
            GeneratedSpritesFolderGuidProperty.stringValue = AssetDatabase.AssetPathToGUID(folderPath);
            serializedObject.ApplyModifiedProperties();

            void DeleteEmbeddedSprites ()
            {
                foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(atlasPath))
                    UnityEngine.Object.DestroyImmediate(asset, true);
                AssetDatabase.SaveAssets();
            }

            string GetOrCreateGeneratedSpritesFolder ()
            {
                var existingPath = AssetDatabase.GUIDToAssetPath(GeneratedSpritesFolderGuid);
                if (AssetDatabase.IsValidFolder(existingPath)) return existingPath;
                var parentPath = Path.GetDirectoryName(atlasPath);
                var folderName = Path.GetFileNameWithoutExtension(atlasPath);
                var fullPath = Path.Combine(parentPath, folderName);
                Directory.CreateDirectory(fullPath);
                return fullPath;
            }

            void UpdateExistingSprite (string path)
            {
                var existingSpriteName = Path.GetFileNameWithoutExtension(path);
                if (newSprites.Find(s => s.name == existingSpriteName) is Sprite newSprite)
                {
                    var existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    EditorUtility.CopySerialized(newSprite, existingSprite);
                    newSprites[newSprites.IndexOf(newSprite)] = existingSprite;
                }
                else AssetDatabase.DeleteAsset(path);
            }
        }

        private void SerializeEmbedded (List<Sprite> newSprites)
        {
            DeleteDecoupledSprites();
            foreach (var sprite in AssetDatabase.LoadAllAssetRepresentationsAtPath(atlasPath))
                UpdateExistingSprite(sprite as Sprite);
            AssetDatabase.SaveAssets();
            foreach (var sprite in newSprites)
                if (!AssetDatabase.IsSubAsset(sprite))
                    AssetDatabase.AddObjectToAsset(sprite, target);

            void DeleteDecoupledSprites ()
            {
                var folderPath = AssetDatabase.GUIDToAssetPath(GeneratedSpritesFolderGuid);
                if (AssetDatabase.IsValidFolder(folderPath))
                    AssetDatabase.DeleteAsset(folderPath);
            }

            void UpdateExistingSprite (Sprite existingSprite)
            {
                if (newSprites.Find(s => s.name == existingSprite.name) is Sprite newSprite)
                {
                    EditorUtility.CopySerialized(newSprite, existingSprite);
                    newSprites[newSprites.IndexOf(newSprite)] = existingSprite;
                }
                else UnityEngine.Object.DestroyImmediate(existingSprite, true);
            }
        }

        private void SetSpritesProperty (IEnumerable<Sprite> value)
        {
            var fieldInfo = target.GetType().GetField(SpritesProperty.name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo is null) throw new InvalidOperationException();
            var list = (List<Sprite>)fieldInfo.GetValue(target);
            list.Clear();
            list.AddRange(value);
            serializedObject.Update();
        }
    }
}
