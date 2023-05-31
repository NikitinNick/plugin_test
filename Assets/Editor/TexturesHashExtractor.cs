using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Analyze;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UnityEditor.TexturesExtractor
{
    public class TexturesHashExtractor : Editor
    {
        private static readonly string[] ImageExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".tif",
            ".tiff",
            ".ico",
            ".svg"
        };

        [MenuItem("TEXTURES/Extract hash")]
        public static void HashExtract()
        {
            var selectedFolder = Selection.activeObject;
            var path = AssetDatabase.GetAssetPath(selectedFolder);

            var projectPath = Application.dataPath;
            var assetsString = "Assets";
            var index = projectPath.IndexOf(assetsString, StringComparison.Ordinal);
            projectPath = projectPath.Substring(0, index);
            var combined = Path.Combine(projectPath, path);
            var stringBuilder = new StringBuilder();
            var textures = new Dictionary<string, string>();

            if (Directory.Exists(combined))
            {
                var files = Directory.GetFiles(combined)
                    .Where(file => ImageExtensions.Any(file.ToLower().EndsWith))
                    .ToList();

                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        var relPath = FileUtil.GetProjectRelativePath(file);
                        var texture = AssetDatabase.LoadAssetAtPath<Texture>(relPath);

                        if (texture)
                        {
                            textures.Add(relPath, texture.imageContentsHash.ToString());
                        }
                    }
                }
            }

            if (textures.Count > 0)
            {
                var keyValuePairs = textures.OrderBy(x => x.Key);

                var longestString = keyValuePairs.Max(x => x.Key.Length);

                foreach (var pair in keyValuePairs)
                {
                    string result = pair.Key.PadRight(longestString);
                    stringBuilder.AppendLine($"{result} - {pair.Value}");
                }
            }

            if (stringBuilder.Length > 0)
            {
                File.WriteAllText(Path.Combine(combined, $"{EditorUserBuildSettings.activeBuildTarget}.txt"),
                    stringBuilder.ToString());

                AssetDatabase.Refresh();
            }
        }

        [MenuItem("TEXTURES/Gather textures in Addressable groups")]
        public static void GatherTexturesFromGroups()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            List<AddressableAssetEntry> allEntries = GatherAssetEntries(settings);
            var textures = allEntries
                .Where(x => x.MainAsset is Texture)
                .Select(asset => asset.MainAsset)
                .Cast<Texture>().ToList();
            
            if (textures.Count > 0)
            {
                var texturesDict = new Dictionary<Texture, string>();

                foreach (var texture in textures)
                {
                    texturesDict[texture] = texture.imageContentsHash.ToString();
                }
                
                GatherTexturesWindow.OpenWindow();
            }
        }
        
        // [MenuItem("TEXTURES/Gather textures in Addressable groups")]
        public static void HashExtractFromBundleGroup()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            // string path = ContentUpdateScript.GetContentStateDataPath(true);
            // var modifiedData = new Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>>();
            // AddressablesContentState cacheData = ContentUpdateScript.LoadContentState(path);
            List<AddressableAssetEntry> allEntries = GatherAssetEntries(settings);
            var textures = allEntries
                .Where(x => x.MainAsset is Texture)
                .Select(asset => asset.MainAsset)
                .Cast<Texture>().ToList();

            if (allEntries.Count > 0)
            {
            }
        }

        public static List<AddressableAssetEntry> GatherAssetEntries(AddressableAssetSettings settings)
        {
            var allEntries = new List<AddressableAssetEntry>();
            settings.GetAllAssets(allEntries, false, g =>
            {
                if (g == null)
                    return false;

                if (!g.HasSchema<BundledAssetGroupSchema>())
                {
                    return false;
                }

                if (!g.HasSchema<ContentUpdateGroupSchema>())
                {
                    return false;
                }

                // if (g.GetSchema<ContentUpdateGroupSchema>().StaticContent)
                // {
                //     return true;
                // }

                return true;
            });

            return allEntries;
        }
    }
}