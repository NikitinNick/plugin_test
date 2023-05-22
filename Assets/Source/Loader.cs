using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Source;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Loader : MonoBehaviour
{
    // Start is called before the first frame update
    private async void Start()
    {
        List<string> cachePaths = new List<string>();
        Caching.GetAllCachePaths(cachePaths);

        var sb = new StringBuilder();
        sb.AppendLine("Cache paths:");

        for (int i = 0; i < cachePaths.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {cachePaths[i]}");
        }

        Debug.Log(sb.ToString());

        await InstantiateAsset("Cube");
        await InstantiateAsset("Sphere", "help", "ua");
        await InstantiateAsset("Plain", "en", "help", "ios");
        await InstantiateAsset("Capsule", "ua", "help", "ios");
        await InstantiateAsset("Cylinder", "help", "ios", "rrr");
    }

    private async Task InstantiateAsset(string assetName, params string[] labels)
    {
        await Task.Delay(500);

        try
        {
            await AddressablesHelper.LoadAssetAsync<GameObject>(KeyLabelsPair.Create(assetName, labels),
                LoadingProgress, true);
        }
        catch (Exception e)
        {
            Debug.Log($"EXCEPTION: {e}");
        }
    }

    private void LoadingProgress(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.IsDone)
        {
            Instantiate(handle.Result);
        }
        else
        {
            Debug.Log($"Loading asset ({handle.DebugName}): {handle.PercentComplete:P}");
        }
    }
}