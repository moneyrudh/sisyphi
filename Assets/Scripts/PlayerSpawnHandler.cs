using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnHandler : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public Vector3[] spawnPoints;

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
}
