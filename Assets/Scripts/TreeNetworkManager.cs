using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// This goes on the parent Tree Tile object
public class TreeNetworkManager : NetworkBehaviour
{
    // private Dictionary<int, NetworkVariable<bool>> treeStates = new Dictionary<int, NetworkVariable<bool>>();
    // private List<DamageTree> childTrees;

    // public override void OnNetworkSpawn()
    // {
    //     childTrees = new List<DamageTree>();
    //     // Get all child DamageTree components
    //     foreach (DamageTree tree in GetComponentsInChildren<DamageTree>())
    //     {
    //         int treeIndex = childTrees.Count;
    //         childTrees.Add(tree);
            
    //         // Create a network variable for this tree's state
    //         var treeState = new NetworkVariable<bool>(false);
    //         treeStates[treeIndex] = treeState;
            
    //         // Setup tree with its index
    //         tree.Initialize(this, treeIndex);
    //     }
    // }

    // public void DamageTreeServerRpc(int treeIndex)
    // {
    //     if (!IsServer) return;
        
    //     if (treeStates.TryGetValue(treeIndex, out NetworkVariable<bool> treeState))
    //     {
    //         DamageTree tree = childTrees[treeIndex];
    //         tree.Damage();
            
    //         if (tree.IsDead)
    //         {
    //             treeState.Value = true;
    //             SpawnLogsServerRpc(treeIndex);
    //         }
    //     }
    // }

    // [ServerRpc]
    // private void SpawnLogsServerRpc(int treeIndex)
    // {
    //     if (treeIndex >= childTrees.Count) return;
        
    //     DamageTree tree = childTrees[treeIndex];
    //     Transform treeTransform = tree.transform;
        
    //     LogCount logCount = treeTransform.GetComponent<LogCount>();
    //     if (logCount != null)
    //     {
    //         int count = logCount.count;
    //         int logIndex = count - 1;
            
    //         // Spawn the log as a network object
    //         GameObject logPrefab = FindObjectOfType<TileSetter>().logs[logIndex].log;
    //         GameObject log = Instantiate(logPrefab, treeTransform.position, Quaternion.identity);
            
    //         log.AddComponent<LogCount>().count = count * 3;
            
    //         NetworkObject netObj = log.GetComponent<NetworkObject>();
    //         netObj.Spawn();
    //     }
    // }
}