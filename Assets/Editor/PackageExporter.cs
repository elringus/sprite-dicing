using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class PackageExporter : EditorWindow
{
    private static readonly string PACKAGE_NAME = Application.productName;

    private static bool IsReadyToExport { get { return !string.IsNullOrEmpty(outputPath) && 
                                                !string.IsNullOrEmpty(outputFileName); } }
    private static string outputPath = "C:/Users/Elringus/Desktop";
    private static string outputFileName = PACKAGE_NAME;
    private static string assetsPath = "Assets/" + PACKAGE_NAME;
    private static string namespaceToWrap = PACKAGE_NAME;
    private static string copyright = "// Copyright 2017 Elringus (Artyom Sovetnikov). All Rights Reserved.";
    private static string tabChars = "    ";
    private static Dictionary<string, string> modifiedScripts = new Dictionary<string, string>();

    [MenuItem("Assets/+ Export Package", priority = 20)]
    public static void ExportPackage ()
    {
        if (IsReadyToExport)
            Export();
    }

    [MenuItem("Assets/+ Export Package (Store)", priority = 20)]
    public static void ExportPackageStore ()
    {
        if (IsReadyToExport)
            Export(true);
    }

    private static void Export (bool wrapNamespace = false)
    {
        if (!string.IsNullOrEmpty(namespaceToWrap))
        {
            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith(assetsPath)) continue;
                if (!path.EndsWith(".cs")) continue;

                var fullpath = Application.dataPath.Replace("Assets", "") + path;
                var originalScriptText = File.ReadAllText(fullpath, Encoding.UTF8);

                string scriptText = string.Empty;
                var isImportedScript = path.Contains("ThirdParty");
                var copyright = isImportedScript ? string.Empty : PackageExporter.copyright;
                if (!isImportedScript || wrapNamespace) scriptText += copyright + Environment.NewLine + Environment.NewLine + "namespace " + namespaceToWrap + Environment.NewLine + "{" + Environment.NewLine;
                scriptText += isImportedScript ? originalScriptText : tabChars + originalScriptText.Replace(Environment.NewLine, Environment.NewLine + tabChars);
                if (!isImportedScript || wrapNamespace) scriptText += Environment.NewLine + "}" + Environment.NewLine;
                File.WriteAllText(fullpath, scriptText, Encoding.UTF8);

                modifiedScripts.Add(fullpath, originalScriptText);
            }
        }

        AssetDatabase.ExportPackage(assetsPath, outputPath + "/" + outputFileName + ".unitypackage", ExportPackageOptions.Recurse);

        if (!string.IsNullOrEmpty(namespaceToWrap))
        {
            foreach (var modifiedScript in modifiedScripts)
            {
                File.WriteAllText(modifiedScript.Key, modifiedScript.Value, Encoding.UTF8);
            }
        }
    }
}
