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
    private bool hasPlacedBoat = false;
    private bool canRemoveBoat = true;

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

    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForEndOfFrame();
        InitializePreview();
    }

    private void InitializePreview()
    {
        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError($"Could not find camera for player {OwnerClientId}");
            return;
        }

        boatPreview = Instantiate(boatGhostPrefab);
        boatPreview.SetActive(false);

        previewRenderers.AddRange(boatPreview.GetComponentsInChildren<Renderer>());

        var triggerHandler = boatPreview.AddComponent<BoatPreviewTrigger>();
        triggerHandler.Initialize(this);

        foreach (Collider col in boatPreview.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }
    }
    
    private void Update()
    {
        if (!IsOwner || playerCamera == null || !inPlacementMode) return;

        if (hasPlacedBoat)
        {
            DisablePlacementMode();
            return;
        }

        UpdatePreviewPosition();
    }

    private void UpdatePreviewPosition()
    {
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
        if (hasPlacedBoat) return;
        TogglePlacementMode();
    }

    private void HandleBoatConfirm(InputAction.CallbackContext context)
    {
        if (!inPlacementMode || !isValidPlacement || hasPlacedBoat) return;
        PlaceBoatServerRpc(boatPreview.transform.position, boatPreview.transform.rotation);
    }

    private void HandleBoatRemoval(InputAction.CallbackContext context)
    {
        Debug.Log("Attempting to remove boat.");
        Debug.Log("hasPlacedBoat" + hasPlacedBoat + "canRemoveBoat" + canRemoveBoat);
        if (!hasPlacedBoat || !canRemoveBoat) return;

        if (placedBoat != null)
        {
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
                Debug.Log("Cannot remove boat -  obstacles detected");
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
    private void PlaceBoatServerRpc(Vector3 position, Quaternion rotation)
    {
        if (hasPlacedBoat) return;

        GameObject boat = Instantiate(boatPrefab, position, rotation);
        NetworkObject networkObject = boat.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(OwnerClientId);

        UpdateBoatPlacedStateClientRpc(true, networkObject);

        var boatController = boat.GetComponent<BoatController>();
        if (boatController != null)
        {
            // boatController.InitializeOwnership(OwnerClientId);
        }
    }

    [ClientRpc]
    private void UpdateBoatPlacedStateClientRpc(bool placed, NetworkObjectReference boatRef)
    {
        hasPlacedBoat = placed;
        if (boatRef.TryGet(out NetworkObject boatObj))
        {
            placedBoat = boatObj;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveBoatServerRpc()
    {
        if (!hasPlacedBoat || !canRemoveBoat) return;

        if (placedBoat != null)
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxPlacementDistance, boatLayer))
            {
                Collider[] obstacles = Physics.OverlapSphere(
                    placedBoat.transform.position,
                    removeCheckRadius,
                    removalObstructionLayer
                );

                if (obstacles.Length == 0)
                {
                    // BoatController boatController = placedBoat.GetComponent<BoatController>();
                    // if (boatController != null)
                    // {
                    //     boatController.OnBoatRemoved();
                    // }
                    UpdatePlacementStateClientRpc();
                    if (OwnerClientId != placedBoat.OwnerClientId) return;
                    placedBoat.Despawn();
                    Destroy(placedBoat.gameObject);
                }
            }
        }
    }

    [ClientRpc]
    private void UpdatePlacementStateClientRpc()
    {
        if (OwnerClientId != placedBoat.OwnerClientId) return;
        StartCoroutine(UpdatePlacementState());
    }

    private IEnumerator UpdatePlacementState()
    {
        yield return new WaitForSeconds(0.2f);
        hasPlacedBoat = false;
        placedBoat = null;
    }

    public void SetBoatRemovable(bool canRemove)
    {
        canRemoveBoat = canRemove;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetBoatRemovableServerRpc(bool canRemove)
    {
        canRemoveBoat = canRemove;
    }

    private void OnDestroy()
    {
        DisableInputs();
        if (boatPreview != null)
        {
            Destroy(boatPreview);
        }
    }
}
