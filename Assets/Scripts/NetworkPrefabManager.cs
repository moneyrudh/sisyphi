using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Unity.Netcode;
#endif

public class NetworkPrefabManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] NetworkPrefabsList networkPrefabsList;
    [SerializeField] private GameObject[] prefabs;

    #if UNITY_EDITOR
    public void LogAllPrefabHashes()
    {
        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null)
            {
                NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    // uint hash = networkObject.GlobalObjectIdHash;
                    // Debug.Log($"Prefab: {prefab.name} has hash: {hash}");
                }
            }
        }
    }

    public void AddPrefabsToNetworkManager()
    {
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager reference is missing!");
            return;
        }

        if (networkPrefabsList == null)
        {
            Debug.LogError("NetworkPrefabsList reference is missing!");
            return;
        }

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null) continue;

            NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogWarning("Prefab " + prefab.name + " does not have a NetworkObject component attached to it. Skipping...");
                continue;
            }

            if(!networkPrefabsList.Contains(prefab))
            {
                NetworkPrefab networkPrefab = new NetworkPrefab();
                networkPrefab.Prefab = prefab;

                networkPrefabsList.Add(networkPrefab);
                Debug.Log("Prefab " + prefab.name + " added to NetworkManager.");
            }
            else
            {
                Debug.LogWarning("Prefab " + prefab.name + " already exists in NetworkManager. Skipping...");
            }
        }

        EditorUtility.SetDirty(networkManager);
    }
    #endif
}
