using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnHandler : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public Vector3[] spawnPoints;
    public GameObject boulderPrefab;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            SetInitialPosition(OwnerClientId);
        }
        else if (IsClient && IsOwner)
        {
            RequestSpawnPositionServerRpc();
        }

        if (IsOwner) SpawnBoulderServerRpc();
    }

    private void SetInitialPosition(ulong clientId)
    {
        int playerIndex = (int)clientId % spawnPoints.Length;
        transform.position = spawnPoints[playerIndex];
        SyncPositionClientRpc(spawnPoints[playerIndex]);
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestSpawnPositionServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        SetInitialPosition(clientId);
    }

    [ClientRpc]
    private void SyncPositionClientRpc(Vector3 position)
    {
        if (!IsServer) transform.position = position;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnBoulderServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        NetworkObject playerObject = NetworkManager.ConnectedClients[clientId].PlayerObject;

        Vector3 spawnPosition = playerObject.transform.position + new Vector3(0, 1f, 4f);
        GameObject boulder = Instantiate(boulderPrefab, transform.position + new Vector3(0, 1f, 4f), Quaternion.identity);
        NetworkObject networkObject = boulder.GetComponent<NetworkObject>();
        networkObject.Spawn();
    }
}
