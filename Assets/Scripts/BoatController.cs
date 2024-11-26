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
    public LayerMask obstructionLayer;
    public Transform obstructionTransform;

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

    // private void HandleBoatMovement()
    // {
    //     if (playerCamera == null) return;

    //     // Handle rotation
    //     if (moveDirection.x != 0)
    //     {
    //         Quaternion targetRotation = transform.rotation * Quaternion.Euler(0f, moveDirection.x * rotationSpeed * Time.fixedDeltaTime, 0f);
                
    //             // Assuming obstructionTransform is where you're checking for collision
    //             RaycastHit[] hits = Physics.RaycastAll(
    //                 obstructionTransform.position,
    //                 transform.right * Mathf.Sign(moveDirection.x),
    //                 0.5f,
    //                 obstructionLayer
    //             );

    //             if (hits.Length > 0)
    //             {
    //                 // Get the closest hit
    //                 RaycastHit closestHit = hits[0];
    //                 foreach (RaycastHit hit in hits)
    //                 {
    //                     if (hit.distance < closestHit.distance)
    //                         closestHit = hit;
    //                 }

    //                 // Move along the normal (projected on horizontal plane)
    //                 Vector3 slideDirection = Vector3.ProjectOnPlane(closestHit.normal, Vector3.up).normalized;
    //                 Vector3 newPosition = rb.position + slideDirection * moveSpeed * Time.fixedDeltaTime * 2f;

    //                 if (Physics.Raycast(newPosition + Vector3.up * 2, Vector3.down, out RaycastHit waterHit, 10f, waterLayer))
    //                 {
    //                     newPosition.y = waterHit.point.y;
    //                     rb.MovePosition(newPosition);
    //                 }
    //             }
    //             else
    //             {
    //                 rb.MoveRotation(targetRotation);
    //             }
    //     }

    //     if (moveDirection.y != 0)
    //     {
    //         Vector3 moveDir = transform.forward * moveDirection.y;
    //         Vector3 rightPerpendicular = Vector3.Cross(Vector3.up, moveDir).normalized;
    //         Vector3 leftPerpendicular = -rightPerpendicular;
    //         Vector3 topRight = (moveDir + rightPerpendicular).normalized;
    //         Vector3 topLeft = (moveDir + leftPerpendicular).normalized;
    //         Vector3 desiredPosition = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;
    //         Vector3 extents = GetComponent<Collider>().bounds.extents;

    //         // Check for collisions in the movement direction
    //         float moveDistance = Mathf.Abs(extents.z) * 2;
    //         Ray ray = new Ray(rb.position + Vector3.up * 0.2f, moveDir);
    //         Ray ray1 = new Ray(rb.position + Vector3.up * 0.2f, rightPerpendicular);
    //         Ray ray2 = new Ray(rb.position + Vector3.up * 0.2f, leftPerpendicular);
    //         Ray ray3 = new Ray(rb.position + Vector3.up * 0.2f, topRight);
    //         Ray ray4 = new Ray(rb.position + Vector3.up * 0.2f, topLeft);
    //         if (
    //             !Physics.Raycast(ray, out RaycastHit obstacleHit, moveDistance * 1.25f, obstructionLayer) &&
    //             !Physics.Raycast(ray1, out RaycastHit obstacleHit1, moveDistance / 1.5f, obstructionLayer) &&
    //             !Physics.Raycast(ray2, out RaycastHit obstacleHit2, moveDistance / 1.5f, obstructionLayer) &&
    //             !Physics.Raycast(ray3, out RaycastHit obstacleHit3, moveDistance, obstructionLayer) &&
    //             !Physics.Raycast(ray4, out RaycastHit obstacleHit4, moveDistance, obstructionLayer) &&
    //             !IsCollidingWithLayerMask(obstructionTransform, obstructionLayer)
    //         )
    //         {
    //             // No obstacle, check water surface
    //             if (Physics.Raycast(desiredPosition + Vector3.up * 2, Vector3.down, out RaycastHit waterHit, 10f, waterLayer))
    //             {
    //                 desiredPosition.y = waterHit.point.y;
    //                 rb.MovePosition(desiredPosition);
    //             }
    //         }
    //         else if (IsCollidingWithLayerMask(obstructionTransform, obstructionLayer))
    //         {
    //             RaycastHit[] hits = Physics.RaycastAll(
    //                 obstructionTransform.position,
    //                 moveDir,
    //                 0.5f,
    //                 obstructionLayer
    //             );

    //             if (hits.Length > 0)
    //             {
    //                 // Get the closest hit
    //                 RaycastHit closestHit = hits[0];
    //                 foreach (RaycastHit hit in hits)
    //                 {
    //                     if (hit.distance < closestHit.distance)
    //                         closestHit = hit;
    //                 }

    //                 // Move along the normal (projected on horizontal plane)
    //                 Vector3 slideDirection = Vector3.ProjectOnPlane(closestHit.normal, Vector3.up).normalized;
    //                 Vector3 newPosition = rb.position + slideDirection * moveSpeed * Time.fixedDeltaTime * 2f;

    //                 if (Physics.Raycast(newPosition + Vector3.up * 2, Vector3.down, out RaycastHit waterHit, 10f, waterLayer))
    //                 {
    //                     newPosition.y = waterHit.point.y;
    //                     rb.MovePosition(newPosition);
    //                 }
    //             }
    //         }
    //     }

    //     // Always sync position and rotation to network
    //     UpdateBoatTransformServerRpc(rb.position, rb.rotation);
    // }

    private void HandleBoatMovement()
    {
        if (playerCamera == null) return;

        if (moveDirection.x != 0) HandleRotation();
        if (moveDirection.y != 0) HandleForwardMovement();

        UpdateBoatTransformServerRpc(rb.position, rb.rotation);
    }

    private void HandleRotation()
    {
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0f, moveDirection.x * rotationSpeed * Time.fixedDeltaTime, 0f);
        if (CanRotate())
        {
            rb.MoveRotation(targetRotation);
        }
        else
        {
            HandleCollisionResponse(transform.right * Mathf.Sign(moveDirection.x));
        }
    }

    private bool CanRotate()
    {
        var hits = Physics.RaycastAll(
            obstructionTransform.position,
            transform.right * Mathf.Sign(moveDirection.x),
            0.5f,
            obstructionLayer
        );
        return hits.Length == 0;
    }

    private void HandleForwardMovement()
    {
        Vector3 moveDir = transform.forward * moveDirection.y;
        Vector3 desiredPosition = CalculateDesiredPosition(moveDir);

        if (CanMoveToPosition(moveDir))
        {
            MoveToPosition(desiredPosition);
        }
        else if (IsCollidingWithLayerMask(obstructionTransform, obstructionLayer))
        {
            HandleCollisionResponse(moveDir);
        }
    }

    private Vector3 CalculateDesiredPosition(Vector3 moveDir)
    {
        return rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;
    }

    private bool CanMoveToPosition(Vector3 moveDir)
    {
        Vector3 extents = GetComponent<Collider>().bounds.extents;
        float moveDistance = Mathf.Abs(extents.z) * 2;

        var rays = CalculateCollisionRays(moveDir, rb.position + Vector3.up * 0.2f);
        return !AnyRayHitsObstacle(rays, moveDistance);
    }

    private (Ray main, Ray right, Ray left, Ray topRight, Ray topLeft) CalculateCollisionRays(Vector3 moveDir, Vector3 origin)
    {
        Vector3 rightPerpendicular = Vector3.Cross(Vector3.up, moveDir).normalized;
        Vector3 leftPerpendicular = -rightPerpendicular;
        Vector3 topRight = (moveDir + rightPerpendicular).normalized;
        Vector3 topLeft = (moveDir + leftPerpendicular).normalized;

        return (
            new Ray(origin, moveDir),
            new Ray(origin, rightPerpendicular),
            new Ray(origin, leftPerpendicular),
            new Ray(origin, topRight),
            new Ray(origin, topLeft)
        );
    }

    private bool AnyRayHitsObstacle((Ray main, Ray right, Ray left, Ray topRight, Ray topLeft) rays, float moveDistance)
    {
        return Physics.Raycast(rays.main, moveDistance * 1.25f, obstructionLayer)
            || Physics.Raycast(rays.right, moveDistance / 1.5f, obstructionLayer)
            || Physics.Raycast(rays.left, moveDistance / 1.5f, obstructionLayer)
            || Physics.Raycast(rays.topRight, moveDistance, obstructionLayer)
            || Physics.Raycast(rays.topLeft, moveDistance, obstructionLayer)
            || IsCollidingWithLayerMask(obstructionTransform, obstructionLayer);
    }

    private void HandleCollisionResponse(Vector3 direction)
    {
        var hits = Physics.RaycastAll(
            obstructionTransform.position,
            direction,
            0.5f,
            obstructionLayer
        );

        if (hits.Length > 0)
        {
            RaycastHit closestHit = GetClosestHit(hits);
            Vector3 slideDirection = Vector3.ProjectOnPlane(closestHit.normal, Vector3.up).normalized;
            Vector3 newPosition = rb.position + slideDirection * moveSpeed * Time.fixedDeltaTime * 2f;
            
            MoveToPosition(newPosition);
        }
    }

    private RaycastHit GetClosestHit(RaycastHit[] hits)
    {
        RaycastHit closestHit = hits[0];
        foreach (RaycastHit hit in hits)
        {
            if (hit.distance < closestHit.distance)
                closestHit = hit;
        }
        return closestHit;
    }

    private void MoveToPosition(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 2, Vector3.down, out RaycastHit waterHit, 10f, waterLayer))
        {
            position.y = waterHit.point.y;
            rb.MovePosition(position);
        }
    }

    bool IsCollidingWithLayerMask(Transform transform, LayerMask layerMask)
    {
        return Physics.CheckSphere(transform.position, 0.1f, layerMask);
    }

    private void FixBoatPosition()
    {

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
