using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class BoatController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 180f;
    public float rotationSmoothTime = 0.1f;
    public float smoothTime = 0.1f;

    [Header("References")]
    public Transform mountPoint;
    public BoxCollider interactionTrigger;
    public LayerMask waterLayer;

    [Header("Input")]
    public InputActionReference interactAction;
    public InputActionReference movement;

    private NetworkVariable<bool> isMounted = new NetworkVariable<bool>(false);
    private NetworkVariable<Vector3> targetPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> targetRotation = new NetworkVariable<Quaternion>();

    private Movement playerMovement;
    private NetworkObject mountedPlayer;
    private Vector3 currentVelocity;
    private Vector3 currentRotationVelocity;
    private Camera playerCamera;
    private Vector2 moveDirection;
    private Rigidbody rb;

    private void Start()
    {
    }

    private System.Collections.IEnumerator WaitForCamera()
    {
        yield return new WaitForEndOfFrame();
        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
    }

    private void OnEnable()
    {
        if (movement != null)
        {
            movement.action.Enable();
        }

        if (interactAction != null)
        {
            interactAction.action.Enable();
            interactAction.action.started += HandleInteractInput;
        }   
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            movement.action.Disable();
        }

        if (interactAction != null)
        {
            interactAction.action.Disable();
            interactAction.action.started -= HandleInteractInput;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            targetPosition.Value = transform.position;
            targetRotation.Value = transform.rotation;
        }

        rb = GetComponent<Rigidbody>();

        if (IsOwner)
        {
            StartCoroutine(WaitForCamera());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsSpawned) return;



        if (IsOwner && isMounted.Value)
        {
            // HandleBoatMovement();
            moveDirection = movement.action.ReadValue<Vector2>();
            UpdateMountedPlayerPosition();
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned) return;

        if (IsOwner && isMounted.Value)
        {
            HandleBoatMovement();
        }
        else if (!IsOwner)
        {
            // rb.MovePosition(Vector3.SmoothDamp(
            //     rb.position,
            //     targetPosition.Value,
            //     ref currentVelocity,
            //     smoothTime
            // ));

            // rb.MoveRotation(Quaternion.Slerp(
            //     rb.rotation,
            //     targetRotation.Value,
            //     Time.fixedDeltaTime / smoothTime
            // ));
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition.Value,
                ref currentVelocity,
                smoothTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation.Value,
                Time.deltaTime / smoothTime
            );
        }
    }

    private void HandleBoatMovement()
    {
        if (playerCamera == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up).normalized;

        Vector3 _movement = (forward * moveDirection.y + right * moveDirection.x).normalized;

        if (_movement != Vector3.zero)
        {
            // Calculate new position
            Vector3 newPosition = rb.position + _movement * moveSpeed * Time.fixedDeltaTime;

            if (Physics.Raycast(newPosition + Vector3.up * 2, Vector3.down, out RaycastHit hit, 10f, waterLayer))
            {
                newPosition.y = hit.point.y;

                // Calculate rotationz
                Vector3 smoothedForward = Vector3.SmoothDamp(
                    transform.forward,
                    _movement,
                    ref currentRotationVelocity,
                    rotationSmoothTime
                );

                Quaternion newRotation = Quaternion.LookRotation(smoothedForward);

                // transform.position = newPosition;
                // transform.rotation = newRotation;
                rb.MovePosition(newPosition);
                rb.MoveRotation(newRotation);

                // Sync to network
                UpdateBoatTransformServerRpc(newPosition, newRotation);
            }
        }
        // float vertical = Input.GetAxis("Vertical");
        // float horizontal = Input.GetAxis("Horizontal");

        // Vector3 moveDirection = transform.forward * vertical;
        // float rotation = horizontal * rotationSpeed * Time.deltaTime;

        // if (vertical != 0 || horizontal != 0)
        // {
        //     Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;
        //     Quaternion newRotation = transform.rotation * Quaternion.Euler(0, rotation, 0);

        //     if (Physics.Raycast(newPosition + Vector3.up * 2, Vector3.down, out RaycastHit hit, 10f, waterLayer))
        //     {
        //         newPosition.y = hit.point.y;
        //         UpdateBoatTransformServerRpc(newPosition, newRotation);
        //     }
        // }
    }

    [ServerRpc]
    private void UpdateBoatTransformServerRpc(Vector3 newPosition, Quaternion newRotation)
    {
        targetPosition.Value = newPosition;
        targetRotation.Value = newRotation;
    }

    private void HandleInteractInput(InputAction.CallbackContext context)
    {
        if (!IsOwner || !IsSpawned) return;

        if (isMounted.Value)
        {
            RequestDismountServerRpc();
        }
        else if (IsInRange())
        {
            RequestMountServerRpc();
        }
    }

    private bool IsInRange()
    {
        if (!IsOwner || interactionTrigger == null) return false;

        var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer == null) return false;

        return interactionTrigger.bounds.Contains(localPlayer.transform.position);
    }

    [ServerRpc]
    private void RequestMountServerRpc(ServerRpcParams rpcParams = default)
    {
        if (isMounted.Value) return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        if (clientId != OwnerClientId)
        {
            Debug.LogWarning($"Player {clientId} attempted to mount boat owned by {OwnerClientId}");
            return;
        }

        NetworkObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (player != null)
        {
            mountedPlayer = player;
            isMounted.Value = true;
            HandleMountClientRpc(new NetworkObjectReference(player));
        }
    }

    [ClientRpc]
    private void HandleMountClientRpc(NetworkObjectReference playerRef)
    {
        if (!playerRef.TryGet(out NetworkObject playerObj)) return;

        if (playerObj.IsOwner)
        {
            playerMovement = playerObj.GetComponent<Movement>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }
        }
    }

    [ServerRpc]
    private void RequestDismountServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!isMounted.Value) return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        if (clientId != OwnerClientId) return;

        isMounted.Value = false;
        HandleDismountClientRpc(new NetworkObjectReference(mountedPlayer));
        mountedPlayer = null;
    }

    [ClientRpc]
    private void HandleDismountClientRpc(NetworkObjectReference playerRef)
    {
        if (!playerRef.TryGet(out NetworkObject playerObj)) return;

        if (playerObj.IsOwner)
        {
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
                playerMovement = null;
            }
        }
    }

    private void UpdateMountedPlayerPosition()
    {
        if (!isMounted.Value || mountPoint == null || mountedPlayer == null) return;

        mountedPlayer.transform.position = mountPoint.position;
        mountedPlayer.transform.rotation = mountPoint.rotation;
    }
}