using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using TMPro;

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

    [Header("Name Display")]
    [SerializeField] private Transform nameTextTransform;

    private Transform otherPlayerTransform;
    private Camera playerCamera;

    private Material _hairMaterial;
    private Material _skinMaterial;
    private Material _pantMaterial;
    private Material _eyesMaterial;

    private int playerIndex;
    private GameObject boulder;
    private int currentCheckpoint = -1;
    private int boulderCheckpoint = -1;
    private CheckpointAction currentCheckpointAction;
    private Vector3 checkpointPosition;
    private Vector3 boulderCheckpointPosition;

    private NetworkVariable<bool> isRespawnCooldown = new NetworkVariable<bool>(false);
    private float respawnCooldownTime = 3f;
    private float minYPosition = -20f;
    private Coroutine respawnCooldownCoroutine;

    private void Awake()
    {
        SisyphiGameManager.Instance.OnStateChanged += PlayerSpawnHandler_SetPlayerState;

        ToggleScripts(false);
    }

    private void PlayerSpawnHandler_SetPlayerState(object sender, System.EventArgs e)
    {
        if (IsOwner && SisyphiGameManager.Instance.IsGamePlaying())
        {
            Cursor.lockState = CursorLockMode.Locked;
            ToggleScripts(true);
            boulder.GetComponent<Rigidbody>().isKinematic = false;
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
            GetComponent<BoulderSkillSystem>().InitializeBoulderSkillSystemServerRpc();

            StartCoroutine(WaitForOtherPlayerCoroutine());
            SisyphiGameManager.Instance.PlayerReadyServerRpc();
        }
        SetPlayerColor();
    }

    private IEnumerator WaitForOtherPlayerCoroutine()
    {
        yield return new WaitForSeconds(1f);
        FindOtherPlayer();
        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
    }

    private void FindOtherPlayer()
    {
        if (!IsOwner) return;

        PlayerSpawnHandler[] players = FindObjectsOfType<PlayerSpawnHandler>();
        foreach (PlayerSpawnHandler player in players)
        {
            if (player != this)
            {
                otherPlayerTransform = player.transform;
                break;
            }
        }
    }

    private void Update()
    {
        if (!SisyphiGameManager.Instance.IsGamePlaying()) return;
        if (!IsOwner) return;

        if (transform.position.y < minYPosition)
        {
            TryRespawnPlayer();
        }

        if (boulder != null && boulder.transform.position.y < minYPosition)
        {
            TryRespawnBoulder();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryRespawnPlayer();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            TryRespawnBoulder();
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        if (nameTextTransform == null) return;
        if (!SisyphiGameManager.Instance.IsGamePlaying()) return;
        if (playerCamera == null) return;

        PlayerSpawnHandler[] allPlayers = FindObjectsOfType<PlayerSpawnHandler>();
        foreach (PlayerSpawnHandler player in allPlayers)
        {
            if (player.nameTextTransform == null) continue;

            // Get the direction from each name text to the current player's camera
            Vector3 directionToCamera = (player.nameTextTransform.position - playerCamera.transform.position).normalized;
            
            // Calculate the rotation that would make the text face the camera
            Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
            
            // Only apply the Y-axis rotation to keep text upright
            player.nameTextTransform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
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

    public void SetBoulderCheckpoint(int checkpointIndex, Vector3 position)
    {
        if (checkpointIndex == boulderCheckpoint) return;
        boulderCheckpoint = checkpointIndex;
        boulderCheckpointPosition = position;
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
        movement.ResetMovement();
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

        Vector3 resetPosition = boulderCheckpoint == -1 ? 
            spawnPoints[playerIndex] + new Vector3(0, 1f, 4f) :
            boulderCheckpointPosition + new Vector3(0, 3f, 0);
    
        boulder.transform.position = resetPosition;
        boulder.transform.rotation = Quaternion.identity;

        if (SisyphiGameManager.Instance.IsGamePlaying()) StartCoroutine(ReenablePhysics(rb));
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
        Vector3 spawnPosition = spawnPoints[index] + new Vector3(0, -1f, 4f);
        GameObject _boulder = Instantiate(boulderPrefab, spawnPosition, Quaternion.identity);
        NetworkObject networkObject = _boulder.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(clientId);

        AssignBoulderClientRpc(clientId, new NetworkObjectReference(networkObject), index);
    }

    [ClientRpc]
    private void AssignBoulderClientRpc(ulong clientId, NetworkObjectReference boulderRef, int index)
    {
        if (boulderRef.TryGet(out NetworkObject boulderNetObj))
        {
            boulder = boulderNetObj.gameObject;
            boulder.GetComponent<Rigidbody>().isKinematic = true;
            boulderNetObj.gameObject.name = "Boulder_" + index;
            SetBoulderMaterial();
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
        nameTextTransform.GetComponent<TMP_Text>().text = playerData.playerName.ToString();
    }

    public void SetBoulderMaterial()
    {
        PlayerData playerData = SisyphiGameMultiplayer.Instance.GetPlayerDataFromClientId(OwnerClientId);
        boulder.GetComponent<Renderer>().material = SisyphiGameMultiplayer.Instance.GetBoulderMaterial(playerData.boulderMaterialId);
    }

    private void ToggleScripts(bool active)
    {
        movement.enabled = active;
        buildingSystem.enabled = active;
        playerFarm.enabled = active;
    }

    public bool isOwner()
    {
        return IsOwner;
    }
}
