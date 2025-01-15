using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using Unity.Netcode;

public class BoatResetSystem : NetworkBehaviour
{
    [Header("Input")]
    public InputActionReference resetBoatAction;

    [Header("References")]
    private BoatPlacementSystem placementSystem;
    private Camera playerCamera;
    public float removeCheckRadius = 1f;
    public LayerMask removalObstructionLayer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;

        placementSystem = GetComponent<BoatPlacementSystem>();
        SisyphiGameManager.Instance.GameFinishedEvent += BoatResetSystem_OnGameFinished;
        StartCoroutine(InitializeWithDelay());
        EnableInputs();
    }

    private void EnableInputs()
    {
        if (resetBoatAction != null)
        {
            resetBoatAction.action.Enable();
            resetBoatAction.action.started += HandleBoatReset;
        }
    }

    private void DisableInputs()
    {
        if (resetBoatAction != null)
        {
            resetBoatAction.action.Disable();
            resetBoatAction.action.started -= HandleBoatReset;
        }
    }

    private void BoatResetSystem_OnGameFinished(object sender, System.EventArgs e)
    {
        if (resetBoatAction != null)
        {
            resetBoatAction.action.Disable();
            resetBoatAction.action.started -= HandleBoatReset;
        }
    }

    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForEndOfFrame();
        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
    }

    private void HandleBoatReset(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        var spawnedBoat = NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .Values
            .FirstOrDefault(obj => obj.CompareTag("Boat") && obj.OwnerClientId == OwnerClientId);

        if (spawnedBoat != null)
        {
            Debug.Log($"Found boat at position: {spawnedBoat.transform.position}");
                // Then check for obstacles
            Collider[] obstacles = Physics.OverlapSphere(
                spawnedBoat.transform.position,
                removeCheckRadius,
                removalObstructionLayer
            );

            if (obstacles.Length == 0)
            {
                CreateBoatIndicatorServerRpc(spawnedBoat.transform.position);

                ResetBoatServerRpc();
            }
            else
            {
                Debug.Log("Cannot remove boat - obstacles detected");
            }
        }
    }

    [ServerRpc]
    private void ResetBoatServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var boat = NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .Values
            .FirstOrDefault(obj => obj.CompareTag("Boat") && obj.OwnerClientId == serverRpcParams.Receive.SenderClientId);

        if (boat != null)
        {
            ulong boatId = boat.NetworkObjectId;

            var boatController = boat.GetComponent<BoatController>();
            if (boatController != null)
            {
                // HandleForceDismountClientRpc(new NetworkObjectReference(boat));
            }

            boat.Despawn();
            Destroy(boat.gameObject);
            
            NotifyBoatResetClientRpc(boatId);
        }
    }

    [ClientRpc]
    private void NotifyBoatResetClientRpc(ulong boatId)
    {
        if (!IsOwner) return;

        placementSystem.InitializePreview();
        placementSystem.placedBoat = null;
        StartCoroutine(placementSystem.ReinitializePreview());
        SoundManager.Instance.PlayOneShot("PickUpBoat");
        Debug.Log("Boat has been reset. You can now place a new boat.");
        PlayerHUD.Instance.HandleBoatHUD(false);
    }

    [ServerRpc]
    private void CreateBoatIndicatorServerRpc(Vector3 position)
    {
        CreateBoatIndicatorClientRpc(position);
    }

    [ClientRpc]
    private void CreateBoatIndicatorClientRpc(Vector3 position)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.position = position;
        indicator.transform.localScale = Vector3.one * 0.5f;

        var renderer = indicator.GetComponent<Renderer>();
        renderer.material.color = Color.red;

        Destroy(indicator, 5f);
    }

    private void OnDestroy()
    {
        DisableInputs();
    }
}
