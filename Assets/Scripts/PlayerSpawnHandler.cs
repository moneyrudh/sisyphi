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
    private int currentCheckpoint = -1;
    private CheckpointAction currentCheckpointAction;
    private Vector3 checkpointPosition;

    private NetworkVariable<bool> isRespawnCooldown = new NetworkVariable<bool>(false);
    private float respawnCooldownTime = 3f;
    private float minYPosition = -10f;
    private Coroutine respawnCooldownCoroutine;

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
        if (!IsOwner) return;

        if (transform.position.y < minYPosition)
        {
            TryRespawnPlayer();
        }

        if (boulder != null && boulder.transform.position.y < minYPosition)
        {
            TryRespawnBoulder();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            TryRespawnPlayer();
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            TryRespawnBoulder();
        }
    }

    private void TryRespawnPlayer()
    {
        if (!isRespawnCooldown.Value)
        {
            SetInitialPositionServerRpc();
            StartRespawnCooldownServerRpc();
        }
    }

    private void TryRespawnBoulder()
    {
        if (!isRespawnCooldown.Value)
        {
            ResetBoulderPositionServerRpc();
            StartRespawnCooldownServerRpc();
        }
    }

    [ServerRpc]
    private void StartRespawnCooldownServerRpc()
    {
        isRespawnCooldown.Value = true;
        if (respawnCooldownCoroutine != null) StopCoroutine(respawnCooldownCoroutine);
        respawnCooldownCoroutine = StartCoroutine(RespawnCooldown());
    }

    private IEnumerator RespawnCooldown()
    {
        yield return new WaitForSeconds(respawnCooldownTime);
        isRespawnCooldown.Value = false;
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == OwnerClientId)
        {
            Destroy(GameObject.Find("Boulder_" + playerIndex));
        }
    }

    public void SetCheckpoint(CheckpointAction checkpointAction, int checkpointIndex, Vector3 position)
    {
        if (checkpointIndex == currentCheckpoint) return;
        if (currentCheckpointAction != null) currentCheckpointAction.StopFireParticles();
        currentCheckpoint = checkpointIndex;
        checkpointPosition = position;
        currentCheckpointAction = checkpointAction;
    }

    public int GetCurrentCheckpoint()
    {
        return currentCheckpoint;
    }

    private void SetInitialPosition()
    {
        // int playerIndex = (int) clientId > 0 ? 1 : 0;
        // GetComponent<Rigidbody>().velocity = Vector3.zero;
        if (currentCheckpoint == -1)
        {
            transform.position = spawnPoints[playerIndex];
        }
        else transform.position = checkpointPosition + new Vector3(0, 1.5f, 0);
        // SyncPositionClientRpc(spawnPoints[playerIndex]);
    }

    [ServerRpc]
    private void SetInitialPositionServerRpc()
    {
        SetInitialPositionClientRpc();
    }

    [ClientRpc]
    private void SetInitialPositionClientRpc()
    {
        SetInitialPosition();
    }

    private void ResetBoulderPosition()
    {
        if (boulder == null) return;

        Rigidbody rb = boulder.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;

        Vector3 resetPosition = currentCheckpoint == -1 ? 
            spawnPoints[playerIndex] + new Vector3(0, 1f, 4f) :
            checkpointPosition + new Vector3(0, 3f, 0);
    
        boulder.transform.position = resetPosition;
        boulder.transform.rotation = Quaternion.identity;

        StartCoroutine(ReenablePhysics(rb));
    }

    private IEnumerator ReenablePhysics(Rigidbody rb)
    {
        yield return new WaitForFixedUpdate();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    [ServerRpc]
    private void ResetBoulderPositionServerRpc()
    {
        ResetBoulderPositionClientRpc();
    }

    [ClientRpc]
    private void ResetBoulderPositionClientRpc()
    {
        ResetBoulderPosition();
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
