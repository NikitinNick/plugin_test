using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Analyze
{
    public class GatherTexturesWindow : EditorWindow
    {
        private readonly Dictionary<Texture, string> _texturesData = new Dictionary<Texture, string>();

        private Vector2 _scrollPosition = Vector2.zero;

        [MenuItem("Addressable/Analyze textures window")]
        public static void OpenWindow()
        {
            var window = GetWindow<GatherTexturesWindow>(false, "Gathered textures");
            window.minSize = new Vector2(400f, 300f);

            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                if (GUILayout.Button("Gather textures from Addressable groups"))
                {
                    GatherFromAddressableGroups();
                }

                if (GUILayout.Button("Gather all textures"))
                {
                    GatherAllTexturesInProject();
                }

                bool guiEnabled = GUI.enabled;
                bool hasData = _texturesData is {Count: > 0};

                GUI.enabled = hasData;
                if (GUILayout.Button("Export to a text file"))
                {
                    string path = EditorUtility.SaveFilePanel("Export textures data", "Assets", "result", "txt");

                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        var stringBuilder = new StringBuilder();
                        var pathAndHash = _texturesData.ToDictionary(
                            kvp => AssetDatabase.GetAssetPath(kvp.Key),
                            kvp => kvp.Value);
                            
                        var keyValuePairs = pathAndHash.OrderBy(x => x.Key);

                        var longestString = keyValuePairs.Max(x => x.Key.Length);

                        foreach (var pair in keyValuePairs)
                        {
                            string result = pair.Key.PadRight(longestString);
                            stringBuilder.AppendLine($"{result} - {pair.Value}");
                        }

                        File.WriteAllText(path, stringBuilder.ToString());
                    }
                }
                
                GUI.enabled = guiEnabled;

                if (hasData)
                {
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                    foreach (var textureData in _texturesData)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.ObjectField(textureData.Key, typeof(Texture), false, GUILayout.Width(100f));
                            EditorGUILayout.LabelField("-", GUILayout.Width(20f));
                            EditorGUILayout.TextField(textureData.Value, GUILayout.MinWidth(70f));
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void GatherFromAddressableGroups()
        {
            _texturesData.Clear();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            List<AddressableAssetEntry> allEntries = GatherAssetEntries(settings);
            var textures = allEntries
                .Where(x => x.MainAsset is Texture)
                .Select(asset => asset.MainAsset)
                .Cast<Texture>().ToList();

            if (textures.Count > 0)
            {
                foreach (var texture in textures)
                {
                    _texturesData[texture] = texture.imageContentsHash.ToString();
                }
            }
        }

        private void GatherAllTexturesInProject()
        {
            _texturesData.Clear();

            var texturesPaths = AssetDatabase.FindAssets("t:Texture", new[] {"Assets"});
            List<Texture> textures = new List<Texture>();

            if (texturesPaths.Length > 0)
            {
                foreach (var pathGUID in texturesPaths)
                {
                    string texturePath = AssetDatabase.GUIDToAssetPath(pathGUID);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);

                    if (texture)
                    {
                        textures.Add(texture);
                    }
                }
            }

            if (textures.Count > 0)
            {
                foreach (var texture in textures)
                {
                    _texturesData[texture] = texture.imageContentsHash.ToString();
                }
            }
        }

        private List<AddressableAssetEntry> GatherAssetEntries(AddressableAssetSettings settings)
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

                return true;
            });

            return allEntries;
        }
    }
}