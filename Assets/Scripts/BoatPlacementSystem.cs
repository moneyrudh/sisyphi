using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Linq;

public class BoatPlacementSystem : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject boatPrefab;
    public GameObject boatGhostPrefab;

    [Header("Preview Materials")]
    public Material validPreviewMaterial;
    public Material invalidPreviewMaterial;

    [Header("Placement Settings")]
    public float maxPlacementDistance = 10f;
    public LayerMask waterLayer;
    public LayerMask obstructionLayer;
    public LayerMask boatLayer;

    [Header("Removal Settings")]

    public float removeCheckRadius = 1f;
    public LayerMask removalObstructionLayer;

    [Header("Input")]
    public InputActionReference placeBoatToggle;
    public InputActionReference placeBoatConfirm;
    public InputActionReference removeBoatAction;

    [Header("References")]
    public GameObject boatPreview;
    private Camera playerCamera;
    public bool inPlacementMode;
    private List<Renderer> previewRenderers = new List<Renderer>();
    private bool isValidPlacement = true;

    public NetworkObject placedBoat;
    public NetworkVariable<bool> hasPlacedBoat = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> canRemoveBoat = new NetworkVariable<bool>(true);

    public event System.Action<bool> OnPlacementModeChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("Initializing BoatPlacementSystem.");
        if (!IsOwner) return;

        EnableInputs();
        StartCoroutine(InitializeWithDelay());
        // hasPlacedBoat.OnValueChanged += (oldValue, newValue) => {
        //     if (newValue)
        //     {
        //         DisablePlacementMode();
        //     }
        // };
        // hasPlacedBoat.OnValueChanged += OnHasPlacedBoatChanged;
    }

    private void Start()
    {
        SisyphiGameManager.Instance.GameFinishedEvent += BoatPlacementSystem_OnGameFinished;
    }

    private void BoatPlacementSystem_OnGameFinished(object sender, System.EventArgs e)
    {
        if (!IsOwner) return;
        if (placeBoatToggle != null)
        {
            placeBoatToggle.action.Disable();
            placeBoatToggle.action.started -= HandleBoatToggle;
        }

        if (placeBoatConfirm != null)
        {
            placeBoatConfirm.action.Disable();
            placeBoatConfirm.action.started -= HandleBoatConfirm;
        }

        if (removeBoatAction != null)
        {
            removeBoatAction.action.Disable();
            removeBoatAction.action.started -= HandleBoatRemoval;
        }
    }

    private void EnableInputs()
    {
        if (!IsOwner) return;
        if (placeBoatToggle != null)
        {
            placeBoatToggle.action.Enable();
            placeBoatToggle.action.started += HandleBoatToggle;
        }

        if (placeBoatConfirm != null)
        {
            placeBoatConfirm.action.Enable();
            placeBoatConfirm.action.started += HandleBoatConfirm;
        }

        if (removeBoatAction != null)
        {
            removeBoatAction.action.Enable();
            removeBoatAction.action.started += HandleBoatRemoval;
        }
    }

    private void DisableInputs()
    {
        if (!IsOwner) return;
        if (placeBoatToggle != null)
        {
            placeBoatToggle.action.Disable();
            placeBoatToggle.action.started -= HandleBoatToggle;
        }

        if (placeBoatConfirm != null)
        {
            placeBoatConfirm.action.Disable();
            placeBoatConfirm.action.started -= HandleBoatConfirm;
        }

        if (removeBoatAction != null)
        {
            removeBoatAction.action.Disable();
            removeBoatAction.action.started -= HandleBoatRemoval;
        }
    }

    private void OnHasPlacedBoatChanged(bool previousValue, bool newValue)
    {
        if (!newValue && IsOwner)
        {
            StartCoroutine(ReinitializePreview());
        }
    }

    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForEndOfFrame();
        InitializePreview();
        EnableInputs();
    }

    public void InitializePreview()
    {
        if (!IsOwner) return;

        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError($"Could not find camera for player {OwnerClientId}");
            return;
        }

        CleanupExistingPreview();

        boatPreview = Instantiate(boatGhostPrefab);
        boatPreview.SetActive(false);

        previewRenderers.Clear();
        previewRenderers.AddRange(boatPreview.GetComponentsInChildren<Renderer>());

        var triggerHandler = boatPreview.AddComponent<BoatPreviewTrigger>();
        triggerHandler.Initialize(this);

        foreach (Collider col in boatPreview.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }

        inPlacementMode = false;
    }

    public IEnumerator ReinitializePreview()
    {
        DisableInputs();

        yield return new WaitForEndOfFrame();

        CleanupExistingPreview();

        // playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();

        Debug.Log("ReinitializePreview for client " + OwnerClientId);

        InitializePreview();
        EnableInputs();
    }

    private void CleanupExistingPreview()
    {
        if (boatPreview != null)
        {
            Destroy(boatPreview);
            boatPreview = null;
        }
    }
    
    private void Update()
    {
        if (!IsOwner || playerCamera == null || !inPlacementMode) return;

        if (HasPlacedBoat())
        {
            DisablePlacementMode();
            return;
        }

        UpdatePreviewPosition();
    }

    private void UpdatePreviewPosition()
    {
        if (boatPreview == null)
        {
            Debug.LogWarning("Preview is null in UpdatePreviewPosition, reinitializing...");
            StartCoroutine(ReinitializePreview());
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxPlacementDistance, waterLayer))
        {
            boatPreview.SetActive(true);
            Vector3 position = hit.point;
            position.y = hit.collider.bounds.max.y;
            boatPreview.transform.position = position;

            Vector3 forward = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z).normalized;
            boatPreview.transform.rotation = Quaternion.LookRotation(forward);
        }
        else
        {
            boatPreview.SetActive(false);
        }
    }

    private void HandleBoatToggle(InputAction.CallbackContext context)
    {
        if (!IsSpawned || !IsOwner)
        {
            Debug.LogWarning($"HandleBoatToggle ignored - IsSpawned: {IsSpawned}, IsOwner: {IsOwner}");
            return;
        }

        Debug.Log($"Handling boat toggle for client {OwnerClientId}");

        if (boatPreview == null)
        {
            Debug.Log($"Preview null during toggle, reinitializing for client {OwnerClientId}");
            StartCoroutine(ReinitializePreview());
            return;
        }

        Debug.Log($"Toggling placement mode for client {OwnerClientId}. Current mode: {inPlacementMode}");
        TogglePlacementMode();
    }

    private void HandleBoatConfirm(InputAction.CallbackContext context)
    {
        if (!inPlacementMode || !isValidPlacement || HasPlacedBoat()) return;
        if (boatPreview == null)
        {
            Debug.LogWarning("Boat preview is null, reinitializing...");
            StartCoroutine(ReinitializePreview());
            return;
        }

        // Do a local water check before sending RPC
        Vector3 previewPos = boatPreview.transform.position;
        if (Physics.Raycast(previewPos + Vector3.up * 2, Vector3.down, out RaycastHit waterHit, 10f, waterLayer))
        {
            Vector3 finalPosition = waterHit.point;
            finalPosition.x = previewPos.x;
            finalPosition.z = previewPos.z;
            PlaceBoatServerRpc(finalPosition, boatPreview.transform.rotation);
        }
        else
        {
            Debug.LogWarning("Invalid boat placement position");
            StartCoroutine(ReinitializePreview());
        }
    }

    private void HandleBoatRemoval(InputAction.CallbackContext context)
    {
        Debug.Log("Attempting to remove boat.");
        // if (!hasPlacedBoat.Value || !canRemoveBoat.Value) return;

        if (playerCamera == null)
        {
            Debug.LogError("Player Camera is null");
            return;
        }

        if (placedBoat != null)
        {
            if (playerCamera != null && playerCamera.gameObject.name != $"PlayerCamera_{OwnerClientId}")
            {
                playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
            }
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxPlacementDistance, boatLayer))
            {
                // Then check for obstacles
                Collider[] obstacles = Physics.OverlapSphere(
                    placedBoat.transform.position,
                    removeCheckRadius,
                    removalObstructionLayer
                );

                if (obstacles.Length == 0)
                {
                    RemoveBoatServerRpc();
                }
                else
                {
                    Debug.Log("Cannot remove boat - obstacles detected");
                }
            }
            else
            {
                Debug.Log("Must look at boat to remove it");
            }
        }
    }

    public void SetPlacementValidity(bool isValid)
    {
        isValidPlacement = isValid;
        Material material = isValid ? validPreviewMaterial : invalidPreviewMaterial;
        foreach (Renderer renderer in previewRenderers)
        {
            renderer.material = material;
        }
    }

    public void TogglePlacementMode()
    {
        inPlacementMode = !inPlacementMode;
        Debug.Log($"Placement mode toggled to {inPlacementMode} for client {OwnerClientId}");
        OnPlacementModeChanged?.Invoke(inPlacementMode);
        
        if (!inPlacementMode)
        {
            DisablePlacementMode();
        }
    }

    private void DisablePlacementMode()
    {
        inPlacementMode = false;
        OnPlacementModeChanged?.Invoke(false);
        if (boatPreview != null)
        {
            boatPreview.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership=false)]
    private void PlaceBoatServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        // if (hasPlacedBoat.Value) return;
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"PlaceBoatServerRpc - Client {clientId} attempting to place boat");

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .Values
            .Any(obj => obj.CompareTag("Boat") && obj.OwnerClientId == clientId))
        {
            Debug.Log($"PlaceBoatServerRpc - Client {clientId} already has a boat");
            return;
        }

        Debug.Log($"Spawning boat for client {clientId} at position {position}");
        GameObject boat = Instantiate(boatPrefab, position, rotation);
        NetworkObject networkObject = boat.GetComponent<NetworkObject>();
        // boat.transform.SetPositionAndRotation(position, rotation);
    
        Debug.Log($"PlaceBoatServerRpc - Spawning boat for client {clientId}");
        networkObject.SpawnWithOwnership(clientId);
        // networkObject.ChangeOwnership(clientId);
        // boat.transform.SetPositionAndRotation(position, rotation);
        // networkObject.gameObject.transform.position = position;
        Debug.Log($"Location of boat {networkObject.gameObject.transform.position}");
        Debug.Log($"PlaceBoatServerRpc - Boat spawned with NetworkObjectId: {networkObject.NetworkObjectId}");
        
        // hasPlacedBoat.Value = true;
        UpdateBoatPlacedStateClientRpc(true, new NetworkObjectReference(networkObject));
        SyncBoatPositionClientRpc(
            clientId,
            new NetworkObjectReference(networkObject),
            position,
            networkObject.transform.rotation
        );
        // // Verify position is valid by doing a server-side water check
        // if (Physics.Raycast(position + Vector3.up * 2, Vector3.down, out RaycastHit waterHit, 10f, waterLayer))
        // {
        //     // Use the water hit point to ensure consistent Y position
        //     Vector3 finalPosition = waterHit.point;
        //     finalPosition.x = position.x;
        //     finalPosition.z = position.z;

        //     GameObject boat = Instantiate(boatPrefab, finalPosition, rotation);
        //     NetworkObject networkObject = boat.GetComponent<NetworkObject>();
        //     networkObject.gameObject.transform.position = finalPosition;
        //     networkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
        //     hasPlacedBoat.Value = true;
        //     UpdateBoatPlacedStateClientRpc(true, new NetworkObjectReference(networkObject));
        //     // StartCoroutine(ConfirmBoatSpawnAfterDelay(networkObject, finalPosition, rpcParams.Receive.SenderClientId));
        //     if (!networkObject.IsSpawned)
        //     {
        //     }
        // }
        // else
        // {
        //     // If water check fails, notify client to retry
        //     // RetryBoatPlacementClientRpc(rpcParams.Receive.SenderClientId);
        // }
    }

    [ClientRpc]
    private void RetryBoatPlacementClientRpc(ulong clientId)
    {
        if (IsOwner && OwnerClientId == clientId)
        {
            StartCoroutine(ReinitializePreview());
        }
    }

    // private IEnumerator ConfirmBoatSpawnAfterDelay(NetworkObject networkObject, Vector3 position, ulong clientId)
    // {
    //     yield return new WaitForEndOfFrame();

    //     if (networkObject != null && networkObject.IsSpawned)
    //     {
    //         hasPlacedBoat.Value = true;
    //         UpdateBoatPlacedStateClientRpc(true, new NetworkObjectReference(networkObject));

    //         SyncBoatPositionClientRpc(

    //             networkObject.NetworkObjectId,
    //             position,
    //             networkObject.transform.rotation
    //         );
    //     }
    //     else
    //     {
    //         Debug.LogError("Failed to spawn boat properly.");
    //         RetryBoatPlacementClientRpc(clientId);
    //     }
    // }

    [ClientRpc]
    private void SyncBoatPositionClientRpc(ulong clientId, NetworkObjectReference boatRef, Vector3 position, Quaternion rotation)
    {
        // if (clientId != OwnerClientId) return;
        if (boatRef.TryGet(out NetworkObject boatObj))
        {
            Debug.Log($"SyncBoatPositionClientRpc - Syncing boat position for client {clientId} at position {position}");
            boatObj.gameObject.transform.position = position;
            boatObj.gameObject.transform.rotation = rotation;
            // boatObj.transform.SetPositionAndRotation(position, rotation);
        }
    }

    [ClientRpc]
    private void UpdateBoatPlacedStateClientRpc(bool placed, NetworkObjectReference boatRef)
    {
        Debug.Log($"UpdateBoatPlacedStateClientRpc - Client {OwnerClientId} received update");
    
        if (boatRef.TryGet(out NetworkObject boatObj))
        {
            Debug.Log($"UpdateBoatPlacedStateClientRpc - Got boat reference successfully for {OwnerClientId}");
            placedBoat = boatObj;
            if (IsOwner)
            {
                DisablePlacementMode();
            }
        }
        else
        {
            Debug.LogError($"UpdateBoatPlacedStateClientRpc - Failed to get boat reference for {OwnerClientId}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveBoatServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        // if (!hasPlacedBoat.Value || !canRemoveBoat.Value) return;
        var boat = NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .Values
            .Where(obj => obj != null && obj.IsSpawned)
            .FirstOrDefault(obj => obj.CompareTag("Boat") && obj.OwnerClientId == clientId);

        Debug.Log("In RemoveBoatServerRpc for clientId: " + clientId);
        if (boat != null && boat.IsSpawned)
        {
            // if (placedBoat.OwnerClientId != OwnerClientId) return;
            if (boat.OwnerClientId != clientId)
            {
                Debug.LogError($"Ownership mismatch during boat removal. Expected {clientId}, got {boat.OwnerClientId}");
                return;
            }

            Collider[] obstacles = Physics.OverlapSphere(
                boat.transform.position,
                removeCheckRadius,
                removalObstructionLayer
            );

            if (obstacles.Length == 0)
            {
                ulong boatId = boat.NetworkObjectId;

                boat.Despawn();
                Destroy(boat.gameObject);

                // hasPlacedBoat.Value = false;
                UpdatePlacementStateClientRpc(boatId, clientId);
            }
        }
    }

    private bool HasPlacedBoat()
    {
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .Values
            .Any(obj => obj.CompareTag("Boat") && obj.OwnerClientId == OwnerClientId);
    }

    [ClientRpc]
    private void UpdatePlacementStateClientRpc(ulong boatId, ulong clientId)
    {
        if (!IsOwner) return;
        Debug.Log("UpdatePlacementStateClientRpc for client " + OwnerClientId);
        if (OwnerClientId == clientId)
        {
            if (placedBoat != null && placedBoat.NetworkObjectId == boatId && placedBoat.OwnerClientId == OwnerClientId)
            {
                placedBoat = null;
            }
            Debug.Log("Updating Placement State for client " + clientId);
            StartCoroutine(ReinitializePreview());
        }
    }

    public void SetBoatRemovable(bool canRemove)
    {
        canRemoveBoat.Value = canRemove;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetBoatRemovableServerRpc(bool canRemove)
    {
        canRemoveBoat.Value = canRemove;
    }

    private void OnDestroy()
    {
        // if (IsSpawned && hasPlacedBoat.Value != null)
        // {
        //     hasPlacedBoat.OnValueChanged -= OnHasPlacedBoatChanged;
        // }

        // DisableInputs();
        if (boatPreview != null)
        {
            Destroy(boatPreview);
        }
    }
}
