using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class BoatController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 10f; //180f;
    public float rotationSmoothTime = 2f; //0.1f;
    public float smoothTime = 0; //0.1f;

    [Header("References")]
    public Transform mountPoint;
    public LayerMask boulderLayer;
    public BoxCollider interactionTrigger;
    public LayerMask waterLayer;
    public LayerMask obstructionLayer;
    public Transform obstructionTransform;
    public Transform boulderDetectionPoint;

    [Header("Input")]
    public InputActionReference interactAction;
    public InputActionReference movement;

    [Header("Movement Speed Settings")]
    public float noBoulderSpeedMultiplier = 2f;
    public float smallBoulderSpeedMultiplier = 1.5f;
    public float mediumBoulderSpeedMultiplier = 1f;
    public float largeBoulderSpeedMultiplier = 0f;

    [Header("Rotation Speed Settings")]
    public float noBoulderRotationMultiplier = 2f;
    public float smallBoulderRotationMultiplier = 1.5f;
    public float mediumBoulderRotationMultiplier = 1f;
    public float largeBoulderRotationMultiplier = 0f;

    private NetworkVariable<bool> hasBoulder = new NetworkVariable<bool>(false);
    private NetworkVariable<NetworkObjectReference> currentBoulderRef = new NetworkVariable<NetworkObjectReference>();
    private NetworkObject currentBoulderNetObj;
    private BoulderController currentBoulderController;
    private NetworkVariable<BoulderSize> currentBoulderSize = new NetworkVariable<BoulderSize>(BoulderSize.Medium);

    public NetworkVariable<bool> isMounted = new NetworkVariable<bool>(false);
    private NetworkVariable<Vector3> targetPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> targetRotation = new NetworkVariable<Quaternion>();

    private NetworkVariable<NetworkObjectReference> mountedPlayerRef = new NetworkVariable<NetworkObjectReference>();
    private NetworkVariable<Vector3> mountedPlayerPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> mountedPlayerRotation = new NetworkVariable<Quaternion>();

    private Movement playerMovement;
    private NetworkObject mountedPlayer;
    private Vector3 currentVelocity;
    private Vector3 currentRotationVelocity;
    private Camera playerCamera;
    private Vector2 moveDirection;
    private Rigidbody rb;
    private BoulderProperties currentBoulderProperties;

    private bool isSoundPlaying = false;

    private void Start()
    {
        currentBoulderProperties = null;
        SisyphiGameManager.Instance.GameFinishedEvent += BoatController_OnGameFinished;
    }

    private void BoatController_OnGameFinished(object sender, System.EventArgs e)
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

    private System.Collections.IEnumerator WaitForCamera()
    {
        yield return new WaitForEndOfFrame();
        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
    }

    private void OnEnable()
    {
        // if (!IsOwner) return;

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
        rb.isKinematic = true;
        // rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (IsOwner)
        {
            if (movement != null)
            {
                movement.action.Enable();
                Debug.Log($"Enabling movement on spawn for boat {NetworkObjectId}");
            }
            StartCoroutine(WaitForCamera());
        }
    }

    private bool CanProcessMovement()
    {
        if (!IsSpawned || !IsOwner) return false;
        if (movement == null || !movement.action.enabled)
        {
            Debug.LogWarning($"Movement check failed for boat {NetworkObjectId}. Action enabled: {movement?.action.enabled}");
            movement?.action.Enable();  // Try to re-enable if disabled
            return false;
        }
        return true;
    }

    void Update()
    {
        if (!CanProcessMovement()) return;

        // if (!IsSpawned) return;

        if (IsOwner && isMounted.Value)
        {
            // HandleBoatMovement();
            UpdateMountedPlayerPosition();
            moveDirection = movement.action.ReadValue<Vector2>();
        }

        if (IsOwner)
        {
            UpdateBoulderProperties();
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned) return;

        if (IsOwner && isMounted.Value)
        {
            HandleBoatMovement();
        }
        else if (!IsOwner && isMounted.Value)
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

        float currentSpeedMultiplier = GetCurrentSpeedMultiplier();
        float currentRotationMultiplier = GetCurrentRotationMultiplier();

        if (currentSpeedMultiplier <= 0) return;

        if (moveDirection.x != 0) HandleRotation(currentRotationMultiplier);
        if (moveDirection.y != 0) HandleForwardMovement(currentSpeedMultiplier);

        if (IsServer || IsHost)
        {
            targetPosition.Value = rb.position;
            targetRotation.Value = rb.rotation;
        }
        else
        {
            UpdateBoatTransformServerRpc(rb.position, rb.rotation);
        }
    }

    private void HandleRotation(float rotationMultiplier)
    {
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0f, moveDirection.x * rotationSpeed * rotationMultiplier * Time.fixedDeltaTime, 0f);
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

    private void HandleForwardMovement(float currentSpeedMultiplier)
    {
        Vector3 moveDir = transform.forward * moveDirection.y;
        Vector3 desiredPosition = CalculateDesiredPosition(moveDir, currentSpeedMultiplier);

        bool canMove = CanMoveToPosition(moveDir);
        bool shouldPlaySound = moveDirection.y != 0 && canMove;

        if (shouldPlaySound != isSoundPlaying)
        {
            if (shouldPlaySound)
            {
                StartBoatSoundServerRpc();
            }
            else
            {
                StopBoatSoundServerRpc();
            }
            isSoundPlaying = shouldPlaySound;
        }

        if (canMove)
        {
            MoveToPosition(desiredPosition);
        }
        else if (IsCollidingWithLayerMask(obstructionTransform, obstructionLayer))
        {
            HandleCollisionResponse(transform.forward);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartBoatSoundServerRpc()
    {
        CheckExistingSourcesClientRpc();
        NetworkedSoundManager.Instance.AttachContinuousSoundClientRpc("Boat", new NetworkObjectReference(gameObject));
    }

    [ServerRpc(RequireOwnership = false)]
    private void StopBoatSoundServerRpc()
    {
        NetworkedSoundManager.Instance.StopContinuousSoundClientRpc(new NetworkObjectReference(gameObject));
    }

    [ClientRpc]
    private void CheckExistingSourcesClientRpc()
    {
        if (TryGetComponent<AudioSource>(out AudioSource _source))
        {
            Destroy(_source);
        }
    }

    private Vector3 CalculateDesiredPosition(Vector3 moveDir, float speedMultiplier)
    {
        return rb.position + moveDir * moveSpeed * speedMultiplier * Time.fixedDeltaTime;
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
        bool IsValidObstacle(Ray ray, float distance)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, distance, obstructionLayer);
            foreach (RaycastHit hit in hits)
            {
                NetworkObject netObj = hit.collider.GetComponentInParent<NetworkObject>();
                if (netObj != null && netObj.gameObject.layer == 12)
                {
                    // If it's a boat, only count if it's another player's
                    if (netObj.OwnerClientId != OwnerClientId) return true;
                }
                else
                {
                    // If it's not a boat (land/terrain), it's always valid
                    return true;
                }
            }
            return false;
        }

        bool IsValidSphereHit()
        {
            Collider[] hits = Physics.OverlapSphere(obstructionTransform.position, 0.1f, obstructionLayer);
            foreach (Collider hit in hits)
            {
                NetworkObject netObj = hit.GetComponentInParent<NetworkObject>();
                if (netObj != null && netObj.gameObject.layer == 12)
                {
                    // If it's a boat, only count if it's another player's
                    if (netObj.OwnerClientId != OwnerClientId) return true;
                }
                else
                {
                    // If it's not a boat (land/terrain), it's always valid
                    return true;
                }
            }
            return false;
        }

        return (moveDirection.y > 0 ? IsValidObstacle(rays.main, moveDistance * 1.5f) : IsValidObstacle(rays.main, moveDistance))
            || IsValidObstacle(rays.right, moveDistance / 1.5f)
            || IsValidObstacle(rays.left, moveDistance / 1.5f)
            || IsValidObstacle(rays.topRight, moveDistance)
            || IsValidObstacle(rays.topLeft, moveDistance)
            || IsValidSphereHit();
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

        SyncTransformClientRpc(newPosition, newRotation);
    }

    [ClientRpc]
    private void SyncTransformClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner)
        {
            rb.MovePosition(position);
            rb.MoveRotation(rotation);
        }
    }

    private void HandleInteractInput(InputAction.CallbackContext context)
    {
        if (!IsOwner || !IsSpawned) return;

        if (isMounted.Value)
        {
            RequestDismountServerRpc(isMounted.Value);
        }
        else if (IsInRange())
        {
            RequestMountServerRpc(isMounted.Value);
        }
    }

    private bool IsBoulderInRange(ulong clientId)
    {
        if (boulderDetectionPoint == null) return false;

        int index = SisyphiGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(clientId);
        var boulder = GameObject.Find($"Boulder_{index}");
        if (boulder == null) return false;

        BoxCollider detectionCollider = boulderDetectionPoint.GetComponent<BoxCollider>();
        if (detectionCollider == null) return false;

        return detectionCollider.bounds.Contains(boulder.transform.position);
    }

    private bool IsInRange()
    {
        if (!IsOwner || interactionTrigger == null) return false;

        var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer == null) return false;

        return interactionTrigger.bounds.Contains(localPlayer.transform.position);
    }

    [ServerRpc]
    private void RequestMountServerRpc(bool mounted, ServerRpcParams rpcParams = default)
    {
        if (mounted) return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        if (clientId != OwnerClientId)
        {
            Debug.LogWarning($"Player {clientId} attempted to mount boat owned by {OwnerClientId}");
            return;
        }

        NetworkObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (player != null)
        {
            // mountedPlayer = player;
            isMounted.Value = true;
            mountedPlayerRef.Value = new NetworkObjectReference(player);
            
            int index = SisyphiGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(clientId);
            var boulder = GameObject.Find($"Boulder_{index}");
            if (boulder != null && IsBoulderInRange(clientId))
            {
                NetworkObject boulderNetObj = boulder.GetComponent<NetworkObject>();
                if (boulderNetObj != null)
                {
                    hasBoulder.Value = true;
                    currentBoulderRef.Value = new NetworkObjectReference(boulderNetObj);
                    currentBoulderController = boulder.GetComponent<BoulderController>();
                    if (currentBoulderController != null)
                    {
                        currentBoulderController.SetMountedState(true);
                    }
                    UpdateBoulderSizeClientRpc(currentBoulderController.GetBoulderProperties().boulderSize);
                }
            }
            HandleMountClientRpc(new NetworkObjectReference(player));
        }
    }

    [ClientRpc]
    private void HandleMountClientRpc(NetworkObjectReference playerRef)
    {
        // if (!IsOwner) return;
        if (!playerRef.TryGet(out NetworkObject playerObj)) return;

        if (playerObj.IsOwner)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            playerObj.GetComponent<Animator>().SetTrigger("mount");
            SetValues(playerObj);
        }

        if (IsServer)
        {
            mountedPlayerPosition.Value = mountPoint.position;
            mountedPlayerRotation.Value = mountPoint.rotation;
        }
    }

    private void SetValues(NetworkObject playerObj)
    {
        playerMovement = playerObj.GetComponent<Movement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            // var playerRb = playerObj.GetComponent<Rigidbody>();
            // if (playerRb) playerRb.isKinematic = true;
        }
    }

    [ServerRpc]
    private void RequestDismountServerRpc(bool mounted, ServerRpcParams rpcParams = default)
    {
        if (!mounted) return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        if (clientId != OwnerClientId) return;

        isMounted.Value = false;
        if (currentBoulderController != null)
        {
            currentBoulderController.SetMountedState(false);
        }
        HandleDismountClientRpc(mountedPlayerRef.Value);

        // mountedPlayer = null;
        mountedPlayerRef.Value = default;
    }

    [ClientRpc]
    private void HandleDismountClientRpc(NetworkObjectReference playerRef)
    {
        // if (!IsOwner) return;
        if (!playerRef.TryGet(out NetworkObject playerObj)) return;

        if (playerObj.IsOwner)
        {
            // rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            playerObj.GetComponent<Animator>().SetTrigger("unmount");
            ResetValues();
        }

        if (IsServer)
        {
            hasBoulder.Value = false;
            currentBoulderRef.Value = default;
            currentBoulderController = null;
            currentBoulderSize.Value = BoulderSize.Medium;
        }
    }

    private void ResetValues()
    {
        if (playerMovement != null)
        {
            // var playerRb = playerObj.GetComponent<Rigidbody>();
            // if (playerRb) playerRb.isKinematic = false;

            playerMovement.enabled = true;
            playerMovement = null;
        }
    }

    private void UpdateMountedPlayerPosition()
    {
        // if (mountPoint == null)
        // {
        //     Debug.LogError("MOUNT POINT WTF");
            
        // }
        // if (mountedPlayer == null)
        // {
        //     Debug.LogError("WTF BRUH");
        // }
        // if (!isMounted.Value)
        // {
        //     Debug.LogError("Bro stop.");
        // }
        if (!isMounted.Value || mountPoint == null) return;

        if (mountedPlayerRef.Value.TryGet(out NetworkObject playerObj))
        {
            if (IsOwner && playerObj.IsOwner)
            {
                playerObj.transform.position = mountPoint.position;
                playerObj.transform.rotation = mountPoint.rotation;
                if (hasBoulder.Value && boulderDetectionPoint != null)
                {
                    if (currentBoulderRef.Value.TryGet(out NetworkObject boulderObj))
                    {
                        boulderObj.transform.position = boulderDetectionPoint.position;
                        // SyncBoulderPositionServerRpc(boulderObj.transform.position);
                    }
                }
            }
        }
    }

    [ServerRpc]
    private void SyncBoulderPositionServerRpc(Vector3 position)
    {
        if (currentBoulderRef.Value.TryGet(out NetworkObject boulderObj))
        {
            SyncBoulderPositionClientRpc(position, boulderObj.NetworkObjectId);
        }
    }

    [ClientRpc]
    private void SyncBoulderPositionClientRpc(Vector3 position, ulong boulderNetId)
    {
        if (!IsOwner)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(boulderNetId, out NetworkObject boulderObj))
            {
                boulderObj.transform.position = position;
            }
        }
    }

    [ClientRpc]
    private void UpdateMountedPlayerTransformClientRpc(Vector3 position, Quaternion rotation)
    {
        if (IsOwner) return; // Skip the owner as they've already updated

        if (mountedPlayer != null)
        {
            mountedPlayer.transform.position = position;
            mountedPlayer.transform.rotation = rotation;
        }
    }

    // UPDATING BOULDER PROPERTIES

    private void UpdateBoulderProperties()
    {
        if (!hasBoulder.Value || currentBoulderRef.Value.TryGet(out NetworkObject boulderObj) == false) return;

        BoulderController boulderController = boulderObj.GetComponent<BoulderController>();
        if (boulderController != null)
        {
            BoulderSize newSize = boulderController.GetBoulderProperties().boulderSize;
            if (currentBoulderSize.Value != newSize)
            {
                if (IsServer)
                {
                    currentBoulderSize.Value = newSize;
                }
                else
                {
                    UpdateBoulderSizeServerRpc(newSize);
                }
            }
        }
    }

    [ServerRpc]
    private void UpdateBoulderSizeServerRpc(BoulderSize newSize)
    {
        currentBoulderSize.Value = newSize;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Boulder"))
        {
            Debug.Log("TRIGGER ENTERED: " + other.name);
            hasBoulder.Value = true;
            NetworkObject boulderNetObj = other.GetComponent<NetworkObject>();
            if (boulderNetObj != null)
            {
                currentBoulderRef.Value = new NetworkObjectReference(boulderNetObj);
                currentBoulderController = other.GetComponent<BoulderController>();
                UpdateBoulderSizeClientRpc(currentBoulderController.GetBoulderProperties().boulderSize);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (!isMounted.Value && other.CompareTag("Boulder"))
        {
            // Debug.Log("TRIGGER EXITED: " + other.name);
            hasBoulder.Value = false;
            currentBoulderRef.Value = default;
            currentBoulderController = null;
            currentBoulderNetObj = null;
            UpdateBoulderSizeClientRpc(BoulderSize.Medium);
        }
    }

    [ClientRpc]
    private void UpdateBoulderSizeClientRpc(BoulderSize size)
    {
        if (IsServer) currentBoulderSize.Value = size;
    }

    private float GetCurrentSpeedMultiplier()
    {
        if (!hasBoulder.Value) return noBoulderSpeedMultiplier;

        switch (currentBoulderSize.Value)
        {
            case BoulderSize.Small:
                return smallBoulderSpeedMultiplier;
            case BoulderSize.Medium:
                return mediumBoulderSpeedMultiplier;
            case BoulderSize.Large:
                return largeBoulderSpeedMultiplier;
            default:
                return mediumBoulderSpeedMultiplier;
        }
    }

    private float GetCurrentRotationMultiplier()
    {
        if (!hasBoulder.Value) return noBoulderRotationMultiplier;

        switch (currentBoulderSize.Value)
        {
            case BoulderSize.Small:
                return smallBoulderRotationMultiplier;
            case BoulderSize.Medium:
                return mediumBoulderRotationMultiplier;
            case BoulderSize.Large:
                return largeBoulderRotationMultiplier;
            default:
                return mediumBoulderRotationMultiplier;
        }
    }
}
