using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

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
    private GameObject boatPreview;
    private Camera playerCamera;
    private bool inPlacementMode;
    private List<Renderer> previewRenderers = new List<Renderer>();
    private bool isValidPlacement = true;

    private NetworkObject placedBoat;
    private NetworkVariable<bool> hasPlacedBoat = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> canRemoveBoat = new NetworkVariable<bool>(true);

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
        hasPlacedBoat.OnValueChanged += OnHasPlacedBoatChanged;
    }

    private void EnableInputs()
    {
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
    }

    private void InitializePreview()
    {
        if (!IsOwner) return;

        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError($"Could not find camera for player {OwnerClientId}");
            return;
        }

        if (boatPreview != null)
        {
            Destroy(boatPreview);
            boatPreview = null;
        }

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

    private IEnumerator ReinitializePreview()
    {
        yield return new WaitForEndOfFrame();

        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();

        if (boatPreview != null)
        {
            Destroy(boatPreview);
            boatPreview = null;
        }

        InitializePreview();
        EnableInputs();
    }
    
    private void Update()
    {
        if (!IsOwner || playerCamera == null || !inPlacementMode) return;

        if (hasPlacedBoat.Value)
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
        Debug.Log(hasPlacedBoat.Value + " Toggling");
        if (hasPlacedBoat.Value) return;

        if (boatPreview == null)
        {
            StartCoroutine(ReinitializePreview());
            return;
        }

        TogglePlacementMode();
    }

    private void HandleBoatConfirm(InputAction.CallbackContext context)
    {
        if (!inPlacementMode || !isValidPlacement || hasPlacedBoat.Value) return;
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
            PlaceBoatServerRpc(previewPos, boatPreview.transform.rotation);
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
        if (!hasPlacedBoat.Value || !canRemoveBoat.Value) return;

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

    private void TogglePlacementMode()
    {
        inPlacementMode = !inPlacementMode;
        if (!inPlacementMode)
        {
            DisablePlacementMode();
        }
    }

    private void DisablePlacementMode()
    {
        inPlacementMode = false;
        if (boatPreview != null)
        {
            boatPreview.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership=false)]
    private void PlaceBoatServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        if (hasPlacedBoat.Value) return;

        // Verify position is valid by doing a server-side water check
        if (Physics.Raycast(position + Vector3.up * 2, Vector3.down, out RaycastHit waterHit, 10f, waterLayer))
        {
            // Use the water hit point to ensure consistent Y position
            Vector3 finalPosition = waterHit.point;
            finalPosition.x = position.x;
            finalPosition.z = position.z;

            GameObject boat = Instantiate(boatPrefab, finalPosition, rotation);
            NetworkObject networkObject = boat.GetComponent<NetworkObject>();
            networkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId);

            hasPlacedBoat.Value = true;
            UpdateBoatPlacedStateClientRpc(true, networkObject);
        }
        else
        {
            // If water check fails, notify client to retry
            RetryBoatPlacementClientRpc(rpcParams.Receive.SenderClientId);
        }
    }

    [ClientRpc]
    private void RetryBoatPlacementClientRpc(ulong clientId)
    {
        if (IsOwner && OwnerClientId == clientId)
        {
            StartCoroutine(ReinitializePreview());
        }
    }


    [ClientRpc]
    private void UpdateBoatPlacedStateClientRpc(bool placed, NetworkObjectReference boatRef)
    {
        if (boatRef.TryGet(out NetworkObject boatObj))
        {
            placedBoat = boatObj;
        }

        if (IsOwner)
        {
            DisablePlacementMode();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveBoatServerRpc()
    {
        if (!hasPlacedBoat.Value || !canRemoveBoat.Value) return;

        if (placedBoat != null)
        {
            if (placedBoat.OwnerClientId != OwnerClientId) return;

            Collider[] obstacles = Physics.OverlapSphere(
                placedBoat.transform.position,
                removeCheckRadius,
                removalObstructionLayer
            );

            if (obstacles.Length == 0)
            {
                ulong boatId = placedBoat.NetworkObjectId;
                placedBoat.Despawn();
                Destroy(placedBoat.gameObject);

                hasPlacedBoat.Value = false;
                UpdatePlacementStateClientRpc(boatId);
            }
        }
    }

    [ClientRpc]
    private void UpdatePlacementStateClientRpc(ulong boatId)
    {
        if (placedBoat != null && placedBoat.NetworkObjectId == boatId && placedBoat.OwnerClientId == OwnerClientId)
        {
            placedBoat = null;
        }
        StartCoroutine(ReinitializePreview());
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
        if (IsSpawned && hasPlacedBoat.Value != null)
        {
            hasPlacedBoat.OnValueChanged -= OnHasPlacedBoatChanged;
        }

        DisableInputs();
        if (boatPreview != null)
        {
            Destroy(boatPreview);
        }
    }
}
