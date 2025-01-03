using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerSpawnHandler : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public Vector3[] spawnPoints;
    public GameObject boulderPrefab;

    [Header("Script References")]
    [SerializeField] private Movement movement;
    [SerializeField] private PlayerFarm playerFarm;
    [SerializeField] private BuildingSystem buildingSystem;

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

    private int playerIndex;
    private GameObject boulder;

    private void Awake()
    {
        SisyphiGameManager.Instance.OnStateChanged += PlayerSpawnHandler_SetPlayerState;

        ToggleScripts(false);
    }

    private void PlayerSpawnHandler_SetPlayerState(object sender, System.EventArgs e)
    {
        if (SisyphiGameManager.Instance.IsGamePlaying())
        {
            Cursor.lockState = CursorLockMode.Locked;
            ToggleScripts(true);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }

        if (IsOwner)
        {
            playerIndex = SisyphiGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(OwnerClientId);
            SetInitialPosition();
            SisyphiGameManager.Instance.SetPlayerJoinedServerRpc();
            SpawnBoulderServerRpc();
            SetPlayerColor();
            GetComponent<BoulderSkillSystem>().InitializeBoulderSkillSystemServerRpc();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SetInitialPosition();
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ResetBoulderPosition();
            // DespawnBoulderServerRpc();
            // SpawnBoulderServerRpc();
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == OwnerClientId)
        {
            Destroy(GameObject.Find("Boulder_" + playerIndex));
        }
    }

    private void SetInitialPosition()
    {
        // int playerIndex = (int) clientId > 0 ? 1 : 0;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        transform.position = spawnPoints[playerIndex];
        // SyncPositionClientRpc(spawnPoints[playerIndex]);
    }

    private void ResetBoulderPosition()
    {
        Vector3 position = spawnPoints[playerIndex] + new Vector3(0, 1f, 4f);
        boulder.transform.position = position;
        boulder.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestSpawnPositionServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        SetInitialPosition();
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

        int index = SisyphiGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(clientId);
        Vector3 spawnPosition = spawnPoints[index] + new Vector3(0, 1f, 4f);
        GameObject _boulder = Instantiate(boulderPrefab, spawnPosition, Quaternion.identity);
        NetworkObject networkObject = _boulder.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(clientId);

        AssignBoulderClientRpc(clientId, new NetworkObjectReference(networkObject), index);
    }

    [ClientRpc]
    private void AssignBoulderClientRpc(ulong clientId, NetworkObjectReference boulderRef, int index)
    {
        if (OwnerClientId == clientId)
        {
            if (boulderRef.TryGet(out NetworkObject boulderNetObj))
            {
                boulder = boulderNetObj.gameObject;
                boulderNetObj.gameObject.name = "Boulder_" + index;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnBoulderServerRpc(ServerRpcParams serverRpcParams = default)
    {
        int index = SisyphiGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        GameObject boulder = GameObject.Find("Boulder_" + index);
        boulder.GetComponent<NetworkObject>().Despawn();
        Destroy(boulder);
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

    private void ToggleScripts(bool active)
    {
        movement.enabled = active;
        buildingSystem.enabled = active;
        playerFarm.enabled = active;
    }
}
