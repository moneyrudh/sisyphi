using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NetworkPrefabManager))]
public class NetworkPrefabManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        NetworkPrefabManager manager = (NetworkPrefabManager)target;

        if (GUILayout.Button("Add Prefabs to NetworkManager"))
        {
            manager.AddPrefabsToNetworkManager();
        }

        if (GUILayout.Button("Log Prefab Hashes"))
        {
            manager.LogAllPrefabHashes();
        }
    }
}