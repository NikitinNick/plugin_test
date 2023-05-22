using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Source
{
    public static class AddressablesHelper
    {
        public static string[] FallbackLabels = {"default"};

        private static readonly List<IResourceLocation> BufferResourceLocations = new();

        /// <summary>
        /// Load asset. If some asset required downloading dependencies - automatic start loading, but throw warning.
        /// </summary>
        /// <param name="keyLabelsPair">Asset information</param>
        /// <param name="callback">Callback for checking loading process</param>
        /// <param name="trackLoadingProgress">If TRUE, progressCallback called only ones and when complete</param>
        /// <typeparam name="TAsset">Asset type</typeparam>
        public static async Task LoadAssetAsync<TAsset>(KeyLabelsPair keyLabelsPair,
            Action<AsyncOperationHandle<TAsset>> callback, bool trackLoadingProgress = false)
        {
            // check if Addressables initialized, otherwise, wait.
            var initCheckHandle = Addressables.InitializeAsync();
            await initCheckHandle.Task;
            
            BufferResourceLocations.Clear();

            // collect locations by key labels pair
            await CollectResourceLocation<TAsset>(keyLabelsPair.Key, keyLabelsPair.Labels, BufferResourceLocations);

            // check if locations found. If locations count is 0, need fallback
            if (BufferResourceLocations.Count == 0)
            {
                Debug.LogWarning(
                    $"Requested asset ({keyLabelsPair}) was not found.");
                
                // 3-rd step: if labels list is null or empty - this is last step for fallback. Show error.
                if (keyLabelsPair.Labels == null || !keyLabelsPair.Labels.Any())
                {
                    Debug.LogError(
                        $"Asset \"{keyLabelsPair.Key}\" of type {typeof(TAsset)} does not exist in Addressables");
                    return;
                }

                // 2-nd step: if labels list is default and locations not found. Trying to get asset only by key.
                if (CompareCollections(keyLabelsPair.Labels, FallbackLabels))
                {
                    Debug.LogWarning(
                        $"Requested asset ({keyLabelsPair.Key}) with default label location not found. Try load asset only by asset name.");
                    LoadAssetAsync(KeyLabelsPair.CreateByKey(keyLabelsPair.Key), callback, trackLoadingProgress);
                    return;
                }

                // 1-st step: locations were not found with requested labels. Trying to get asset by fallback label.
                Debug.LogWarning(
                    $"Requested asset ({keyLabelsPair}) was not found. Trying to load asset with fallback labels.");
                LoadAssetAsync(KeyLabelsPair.Create(keyLabelsPair.Key, FallbackLabels), callback, trackLoadingProgress);
                
                return;
            }

            // check if multiple assets were found by request
            if (BufferResourceLocations.Count > 1)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine();
                for (var index = 0; index < BufferResourceLocations.Count; index++)
                {
                    var currentLocation = BufferResourceLocations[index];
                    stringBuilder.AppendLine($"\t{index + 1}. {currentLocation.InternalId}");
                }

                Debug.LogWarning(
                    $"Multiple assets was found by request ({keyLabelsPair}), return first. \n[{stringBuilder}]");
            }

            // check if all dependencies was loaded
            var sizeHandler = Addressables.GetDownloadSizeAsync(BufferResourceLocations[0]);

            await sizeHandler.Task;

            var isNeedDownload = sizeHandler.Result > 0;
            if (isNeedDownload)
            {
                Debug.LogWarning($"For {typeof(TAsset).Name} \"{keyLabelsPair.Key}\" start downloading dependencies");
            }

            // loading asset by location
            var infoProgressTimer = 0f;
            var assetHandle = Addressables.LoadAssetAsync<TAsset>(BufferResourceLocations[0]);
            while (!assetHandle.IsDone)
            {
                // send load state
                if (trackLoadingProgress)
                {
                    callback(assetHandle);
                }

                await Task.Yield();
            }

            // last call as done
            callback(assetHandle);
        }

        /// <summary>
        /// Release asset
        /// </summary>
        /// <param name="obj">Target object</param>
        /// <typeparam name="TObject">Object type</typeparam>
        public static void Release<TObject>(TObject obj)
        {
            Addressables.Release(obj);
        }

        private static async Task CollectResourceLocation<TAsset>(string key, IList<string> labels,
            List<IResourceLocation> locationsBuffer)
        {
            var locations = labels != null && labels.Any()
                ? await Addressables
                    .LoadResourceLocationsAsync(labels, Addressables.MergeMode.Intersection, typeof(TAsset)).Task
                : await Addressables.LoadResourceLocationsAsync(key, typeof(TAsset)).Task;

            if (locations != null)
            {
                foreach (var resourceLocation in locations)
                {
                    if (resourceLocation.PrimaryKey.Equals(key))
                    {
                        locationsBuffer.Add(resourceLocation);
                    }
                }
            }
        }

        private static bool CompareCollections<T>(IEnumerable<T> lhs, IEnumerable<T> rhs)
        {
            Dictionary<T, int> countDict = new Dictionary<T, int>();
            
            foreach (T item in lhs)
            {
                countDict[item]++;
            }

            foreach (T item in rhs)
            {
                if (countDict.ContainsKey(item))
                {
                    countDict[item]--;
                    if (countDict[item] == 0)
                        countDict.Remove(item);
                }
                else
                {
                    return false;
                }
            }

            return countDict.Count == 0;
        }
    }
}