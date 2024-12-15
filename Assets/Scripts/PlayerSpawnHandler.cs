using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnHandler : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public Vector3[] spawnPoints;
    public GameObject boulderPrefab;

    [Header("Renderer References")]
    [SerializeField] private List<SkinnedMeshRenderer> hairMeshRenderers;
    [SerializeField] private List<SkinnedMeshRenderer> skinMeshRenderers;
    [SerializeField] private SkinnedMeshRenderer pantMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer eyesMeshRenderer;

    [Header("Material References")]
    [SerializeField] private Material hairMaterial;
    [SerializeField] private Material skinMaterial;
    [SerializeField] private Material pantMaterial;
    [SerializeField] private Material eyesMaterial;

    private Material _hairMaterial;
    private Material _skinMaterial;
    private Material _pantMaterial;
    private Material _eyesMaterial;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            SetInitialPosition(OwnerClientId);
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            SpawnBoulderServerRpc();
        }

        SetPlayerColor();
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == OwnerClientId)
        {
            Destroy(GameObject.Find("Boulder_" + OwnerClientId));
        }
    }

    private void SetInitialPosition(ulong clientId)
    {
        int playerIndex = (int) clientId > 0 ? 1 : 0;
        transform.position = spawnPoints[playerIndex];
        // SyncPositionClientRpc(spawnPoints[playerIndex]);
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

        int index = OwnerClientId == 0 ? 0 : 1;
        Vector3 spawnPosition = spawnPoints[index] + new Vector3(0, 1f, 4f);
        GameObject boulder = Instantiate(boulderPrefab, spawnPosition, Quaternion.identity);
        boulder.name = "Boulder_" + OwnerClientId;
        NetworkObject networkObject = boulder.GetComponent<NetworkObject>();
        networkObject.Spawn();
    }

    public void SetPlayerColor()
    {
        _hairMaterial = new Material(hairMeshRenderers[0].material);
        _skinMaterial = new Material(skinMeshRenderers[0].material);
        _pantMaterial = new Material(pantMeshRenderer.material);
        _eyesMaterial = new Material(eyesMeshRenderer.material);

        foreach (SkinnedMeshRenderer renderer in hairMeshRenderers)
        {
            renderer.material = _hairMaterial;
        }
        foreach (SkinnedMeshRenderer renderer in skinMeshRenderers)
        {
            renderer.material = _skinMaterial;
        }
        pantMeshRenderer.material = _pantMaterial;
        eyesMeshRenderer.material = _eyesMaterial;

        PlayerData playerData = SisyphiGameMultiplayer.Instance.GetPlayerDataFromClientId(OwnerClientId);
        _hairMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.hairColorId);
        _skinMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.skinColorId);
        _pantMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.pantColorId);
        _eyesMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.eyesColorId);
    }
}
