using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.VisualScripting;

public class Movement : NetworkBehaviour
{
    [Header("References")]
    public InputActionReference movement;
    public InputActionReference jump;
    public GameObject pushingColliders;
    public Transform proximityCheck;
    public Transform groundCheck;
    public Transform headCheck;
    public Transform boulderCheck;
    public LayerMask groundLayer;
    public LayerMask boulderLayer;
    public LayerMask wallObstacleLayer;
    public LayerMask groundObstacleLayer;

    private GameObject boulder;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float pushSpeed = 2f;
    public float fallingVelocity = -2f;
    public float jumpForce = 5f;
    public float rotationSpeed = 72f;
    public float rotationFromCameraSpeed = 10f;
    public float groundCheckDistance = 0.1f;
    public float groundCheckRadius = 0.1f;
    public float wallCheckRadius = 0.5f;
    public float boulderCheckRadius = 0.75f;
    public float groundCollisionDistance = 0.75f;
    public float wallCollisionDistance = 1f;
    public float boulderCollisionDistance = 0f;
    public float gravityMultiplier = 2.5f;
    public float fallMultiplier = 2.5f;
    public float rotationSmoothTime = 0.1f;
    public float impactAnticipationDistance = 2f;
    public float anticipatedTimeToImpact = 0.6f;
    private Vector3 currentRotationVelocity;
    [System.NonSerialized] public Vector2 moveDirection;
    private Rigidbody rb;
    private Camera playerCamera;
    private CameraController cameraController;
    private Animator animator;
    private bool canMove;
    private bool isJumping;
    public bool isGrounded;
    private bool idle;
    private bool pushing;
    private bool isFalling;
    private bool isFreeFalling;
    private float jumpYPosition;
    private float fallYPosition;
    private bool beingHit = false;

    private NetworkVariable<float> animationSpeed = new NetworkVariable<float>(1f);
    private NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>();
    private NetworkVariable<bool> netPushing = new NetworkVariable<bool>();
    private NetworkVariable<Vector3> netBoulderPosition = new NetworkVariable<Vector3>();
    private Rigidbody boulderRb;
    private BoulderController boulderController;

    public void OnEnable()
    {
        movement.action.Enable();
        jump.action.started += Jump;
    }

    public void Movement_OnGameFinished(object sender, System.EventArgs e)
    {
        canMove = false;
        animator.speed = 1;
        // pushing = false;
        // ResetAnimator();
    }

    public void ResetMovement()
    {
        canMove = true;
        pushing = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            base.OnNetworkSpawn();
            Debug.Log("Calling TileSetter");
            FindObjectOfType<TileSetter>().SetInitialGrid();
        }
        
        gameObject.name = "Player_" + OwnerClientId;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        cameraController = GetComponent<CameraController>();
        boulder = GameObject.FindWithTag("Boulder");
        canMove = true;
        isFalling = false;
        pushing = false;
        isFreeFalling = false;
        SisyphiGameManager.Instance.GameFinishedEvent += Movement_OnGameFinished;

        if (IsOwner)
        {
            StartCoroutine(WaitForCamera());
        }
    }

    private System.Collections.IEnumerator WaitForCamera()
    {
        yield return new WaitForEndOfFrame();
        playerCamera = GameObject.Find($"PlayerCamera_{OwnerClientId}")?.GetComponent<Camera>();
    }

    void Update()
    {
        // if (SisyphiGameManager.Instance.IsGameOver()) return;
        // if (!SisyphiGameManager.Instance.IsGamePlaying()) return;
        
        if (IsOwner)
        {
            moveDirection = movement.action.ReadValue<Vector2>();
            CheckGrounded();
            if (!beingHit) UpdateAnimator();
            CheckRockProximity();

            if (pushing && Input.GetKey(KeyCode.LeftShift))
            {
                rb.mass = 3;
            }
            else
            {
                rb.mass = 1;
            }
        }
    }

    private void FixedUpdate()
    {
        if (SisyphiGameManager.Instance.IsGameOver()) return;
        if (!SisyphiGameManager.Instance.IsGamePlaying()) return;
       
        if (IsOwner) 
        {
            // Move();
            if (canMove) MoveRelativeToCamera();
            // Rotate();
            ApplyGravity();
            if (canMove) CheckPushing();
            UpdateBoulderPosition();
            // UpdateNetworkPositionServerRpc(transform.position, transform.rotation);
        }
    }

    private void UpdateBoulderPosition()
    {
        if (!IsOwner && boulder != null)
        {
            boulder.transform.position = Vector3.Lerp(boulder.transform.position, netBoulderPosition.Value, Time.fixedDeltaTime * 10f);
        }
    }

    [ServerRpc]
    private void UpdateNetworkPositionServerRpc(Vector3 position, Quaternion rotation)
    {
        netPosition.Value = position;
        netRotation.Value = rotation;
    }

    private void Move()
    {
        if (!IsOwner) return;
        if (playerCamera == null) return;

        Vector3 movement = new Vector3(moveDirection.x, 0, moveDirection.y).normalized;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        // if (pushing) {
        //     boulder.GetComponent<Rigidbody>().MovePosition(boulder.GetComponent<Rigidbody>().position + movement * moveSpeed / 3f * Time.fixedDeltaTime); 
        // }
    }

    private bool CanMoveInDirection(Vector3 direction)
    {
        if (pushing) return true;
        if (direction == Vector3.zero) return true;

        direction = direction.normalized;

        LayerMask currentWallObstacleLayer = isJumping ?
            wallObstacleLayer | (1 << 7) :
            wallObstacleLayer;

        RaycastHit hit;
        bool headBlocked = Physics.Raycast(
            headCheck.position, 
            direction, 
            out hit,
            wallCollisionDistance, 
            currentWallObstacleLayer,
            QueryTriggerInteraction.Ignore
        );

        if (headBlocked)
        {
            Debug.Log("HIT! Object: " + hit.collider.name);
        }
        
        bool groundBlocked = Physics.Raycast(
            groundCheck.position, 
            direction, 
            out hit,
            groundCollisionDistance, 
            groundObstacleLayer,
            QueryTriggerInteraction.Ignore
        );

        if (groundBlocked)
        {
            Debug.Log("HIT! Object: " + hit.collider.name);
        }

        // bool boulderBlocked = Physics.Raycast(
        //     boulderCheck.position,
        //     direction,
        //     out hit,
        //     boulderCollisionDistance,
        //     boulderLayer,
        //     QueryTriggerInteraction.Ignore
        // );

        // if (boulderBlocked)
        // {
        //     Debug.Log("BOULDER HIT");
        // }
        
        // DrawSphereCast(
        //     boulderCheck.position,
        //     direction,
        //     boulderCheckRadius,
        //     boulderCollisionDistance,
        //     boulderBlocked ? Color.red : Color.green
        // );
        
        DrawSphereCast(
            headCheck.position,
            direction,
            wallCheckRadius,
            wallCollisionDistance,
            headBlocked ? Color.red : Color.green
        );
        
        // Standard raycast debug for ground check
        Debug.DrawRay(
            groundCheck.position, 
            direction * 0.2f, 
            groundBlocked ? Color.red : Color.green,
            0.5f
        );

        return !headBlocked && !groundBlocked;
    }

    private bool CanRotateInDirection(Vector3 direction)
    {
        if (pushing) return true;
        if (direction == Vector3.zero) return true;

        direction = direction.normalized;

        RaycastHit hit;
        bool boulderBlocked = Physics.Raycast(
            boulderCheck.position,
            direction,
            out hit,
            boulderCollisionDistance,
            boulderLayer,
            QueryTriggerInteraction.Ignore
        );

        if (boulderBlocked)
        {
            Debug.Log("BOULDER HIT");
        }
        
        DrawSphereCast(
            boulderCheck.position,
            direction,
            boulderCheckRadius,
            boulderCollisionDistance,
            boulderBlocked ? Color.red : Color.green
        );

        return !boulderBlocked;
    }

    private void DrawSphereCast(Vector3 origin, Vector3 direction, float radius, float distance, Color color)
    {
        // Draw the starting sphere
        DrawWireSphere(origin, radius, color);
        
        // Draw the ending sphere
        Vector3 endPosition = origin + direction * distance;
        DrawWireSphere(endPosition, radius, color);
        
        // Draw the connecting lines between spheres
        Vector3 up = Vector3.up * radius;
        Vector3 right = Vector3.right * radius;
        Vector3 forward = Vector3.forward * radius;

        // Draw lines connecting the spheres
        Debug.DrawLine(origin + up, endPosition + up, color, 0.5f);
        Debug.DrawLine(origin - up, endPosition - up, color, 0.5f);
        Debug.DrawLine(origin + right, endPosition + right, color, 0.5f);
        Debug.DrawLine(origin - right, endPosition - right, color, 0.5f);
        Debug.DrawLine(origin + forward, endPosition + forward, color, 0.5f);
        Debug.DrawLine(origin - forward, endPosition - forward, color, 0.5f);
    }

    private void DrawWireSphere(Vector3 position, float radius, Color color)
    {
        float segments = 16;
        float angleStep = 360f / segments;
        
        // Draw three circles for each major axis
        for (int axis = 0; axis < 3; axis++)
        {
            for (float angle = 0; angle < 360; angle += angleStep)
            {
                float nextAngle = angle + angleStep;
                
                // Convert angles to radians
                float rad1 = angle * Mathf.Deg2Rad;
                float rad2 = nextAngle * Mathf.Deg2Rad;
                
                Vector3 p1 = position;
                Vector3 p2 = position;
                
                switch (axis)
                {
                    case 0: // XY plane
                        p1 += new Vector3(Mathf.Cos(rad1) * radius, Mathf.Sin(rad1) * radius, 0);
                        p2 += new Vector3(Mathf.Cos(rad2) * radius, Mathf.Sin(rad2) * radius, 0);
                        break;
                    case 1: // XZ plane
                        p1 += new Vector3(Mathf.Cos(rad1) * radius, 0, Mathf.Sin(rad1) * radius);
                        p2 += new Vector3(Mathf.Cos(rad2) * radius, 0, Mathf.Sin(rad2) * radius);
                        break;
                    case 2: // YZ plane
                        p1 += new Vector3(0, Mathf.Cos(rad1) * radius, Mathf.Sin(rad1) * radius);
                        p2 += new Vector3(0, Mathf.Cos(rad2) * radius, Mathf.Sin(rad2) * radius);
                        break;
                }
                
                Debug.DrawLine(p1, p2, color, 0.5f);
            }
        }
    }

    private void MoveRelativeToCamera()
    {
        if (!IsOwner) return;
        if (playerCamera == null) return;
        
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up).normalized;

        Vector3 movement = (forward * moveDirection.y + right * moveDirection.x).normalized;

        if (CanMoveInDirection(movement))
        {
            float currentSpeed = pushing ? pushSpeed : (Input.GetKey(KeyCode.LeftShift) ? moveSpeed * sprintMultiplier : moveSpeed);
            rb.MovePosition(rb.position + movement * currentSpeed * Time.fixedDeltaTime);

            // Quaternion targetRotation = Quaternion.LookRotation(movement);
        }
        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            rb.MoveRotation(Quaternion.Lerp(rb.rotation, targetRotation, rotationFromCameraSpeed * Time.fixedDeltaTime));
        }
    }

    [ServerRpc]
    private void MoveBoulderServerRpc(Vector3 movement)
    {
        if (!netPushing.Value) return;
        if (boulder != null)
        {
            Rigidbody rb = boulder.GetComponent<Rigidbody>();
            rb.MovePosition(rb.position + movement);
        }
    }

    [ServerRpc]
    private void SyncBoulderPositionServerRpc(Vector3 position)
    {
        netBoulderPosition.Value = position;
    }

    private void CheckPushing()
    {
        if (!IsOwner) return;
        if (pushing) 
        {
            Vector3 relativePos = (boulder.transform.position - transform.position).normalized;
            // Quaternion toRotation = Quaternion.LookRotation(relativePos, Vector3.up);
            // transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.fixedDeltaTime);

            relativePos.y = 0;

            Vector3 smoothedForward = Vector3.SmoothDamp(
                transform.forward,
                relativePos,
                ref currentRotationVelocity,
                rotationSmoothTime
            );
            
            if (smoothedForward != Vector3.zero) {
                transform.rotation = Quaternion.LookRotation(smoothedForward);
            }
        }
    }

    private void Rotate() {
        if (!IsOwner) return;
        if (pushing) 
        {
            Vector3 relativePos = (boulder.transform.position - transform.position).normalized;
            // Quaternion toRotation = Quaternion.LookRotation(relativePos, Vector3.up);
            // transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.fixedDeltaTime);

            relativePos.y = 0;

            Vector3 smoothedForward = Vector3.SmoothDamp(
                transform.forward,
                relativePos,
                ref currentRotationVelocity,
                rotationSmoothTime
            );
            
            if (smoothedForward != Vector3.zero) {
                transform.rotation = Quaternion.LookRotation(smoothedForward);
            }
        }
        else if (moveDirection != Vector2.zero) {
            Quaternion toRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, moveDirection.y), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (pushing) return;
        if (isGrounded && !isJumping)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // Reset Y velocity
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
            isFalling = false;
            jumpYPosition = transform.position.y;
            animator.SetBool("impact", false);
            animator.SetTrigger("jump");
        }
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.SphereCast(
            groundCheck.position + Vector3.up * 0.2f, // Start slightly above to avoid false negatives
            0.2f, // Radius of sphere
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance + 0.1f,
            groundLayer
        );

        if (!isGrounded && !wasGrounded && !isJumping && rb.velocity.y < -0.5f && !isFreeFalling)
        {
            isFreeFalling = true;
            fallYPosition = transform.position.y;
            Debug.Log("Playing should be falling at position " + fallYPosition);
        }

        if (!isGrounded && rb.velocity.y < 0)
        {
            bool aboutToLand = Physics.SphereCast(
                groundCheck.position + Vector3.up * 0.2f,
                impactAnticipationDistance,
                Vector3.down,
                out RaycastHit anticipationHit,
                impactAnticipationDistance,
                groundLayer
            );

            if (aboutToLand)
            {
                float timeToImpact = anticipationHit.distance / Mathf.Abs(rb.velocity.y);
                if (timeToImpact < anticipatedTimeToImpact)
                {
                    if (isFalling)
                    {
                        if (jumpYPosition - transform.position.y > 15)
                        {
                            // Set fall flat animation
                            animator.SetBool("impact", true);
                            animator.SetBool("landed", false);
                            SetMovement(false);
                        }
                        else if (jumpYPosition - transform.position.y > 5)
                        {
                            // Set minor impact animation
                            animator.SetBool("impact", false);
                            animator.SetBool("landed", false);
                            SetMovement(false);
                        }
                        else if (fallYPosition - transform.position.y > 15)
                        {
                            animator.SetBool("impact", true);
                            animator.SetBool("landed", false);
                            SetMovement(false);
                        }
                        else if (fallYPosition - transform.position.y > 5)
                        {
                            animator.SetBool("impact", false);
                            animator.SetBool("landed", false);
                            SetMovement(false);
                        }
                        else
                        {
                            animator.SetBool("landed", true);
                            SetMovement(true);
                        }
                        isFalling = false;
                    }
                }
            }
        }

        if (isGrounded && !wasGrounded)
        {
            Debug.Log("Landed");
            // animator.SetTrigger("landed");
            
            animator.SetBool("falling", false);
        }

        if (isGrounded && wasGrounded && rb.velocity.y < 0.1f)
        {
            isJumping = false;
            isFalling = false;
            isFreeFalling = false;
            fallYPosition = -999f;
            jumpYPosition = -999f;
        }
    }

    private void CheckRockProximity()
    {
        RaycastHit hit;
        bool wasPushing = pushing;
        pushing = Physics.Raycast(proximityCheck.position, transform.TransformDirection(Vector3.forward), out hit, 0.75f, boulderLayer);
        pushingColliders.SetActive(pushing);

        if (pushing)
        {
            boulder = hit.collider.gameObject;
            if (!wasPushing)
            {
                boulderController = boulder.GetComponent<BoulderController>();
                // Request boulder ownership when starting to push
                boulderController.OnPlayerApproach(NetworkObject);
                
                if (IsServer)
                {
                    netPushing.Value = true;
                }
                else 
                {
                    UpdatePushingStateServerRpc(true);
                }
            }
        }
        else if (wasPushing)
        {
            // Release boulder ownership when stopping push
            boulderController?.OnPlayerLeave(NetworkObject);
            
            if (IsServer) 
            {
                netPushing.Value = false;
            }
            else 
            {
                UpdatePushingStateServerRpc(false);
            }
            boulderController = null;
            boulderRb = null;
        }
    }

    [ServerRpc]
    private void UpdatePushingStateServerRpc(bool isPushing)
    {
        netPushing.Value = isPushing;
    }

    private void ApplyGravity()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !jump.action.IsPressed())
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void UpdateAnimator()
    {
        bool isMoving = moveDirection.magnitude > 0;

        if (SisyphiGameManager.Instance.IsGameOver())
        {
            SetAnimatorSpeedServerRpc(1f);
            animator.SetBool("pushing", false);
            animator.SetBool("sprint", false);
            animator.SetBool("run", false);
            animator.SetBool("idle", true);
            return;
        }
        if (!isGrounded && rb.velocity.y < 0f && !isFalling && transform.position.y < jumpYPosition)
        {
            animator.SetBool("falling", true);
            animator.SetBool("landed", false);
            isFalling = true;
        }
        else if (!isGrounded && !isJumping && !isFalling && (fallYPosition - transform.position.y) > 1.5f)
        {
            Debug.Log("SETTING ANIMATION");
            animator.SetBool("falling", true);
            animator.SetBool("landed", false);
            isFalling = true;
        }
        else if (pushing && isGrounded && canMove)
        {
            animator.SetBool("pushing", true);
            animator.SetBool("fast-push", Input.GetKey(KeyCode.LeftShift));
            // animator.speed = isMoving ? 1f : 0.1f;
            SetAnimatorSpeedServerRpc(isMoving ? 1f : 0.1f);
        }
        else if (canMove)
        {
            // animator.speed = 1f;
            SetAnimatorSpeedServerRpc(1f);
            animator.SetBool("pushing", false);
            animator.SetBool("sprint", isMoving && isGrounded && Input.GetKey(KeyCode.LeftShift));
            animator.SetBool("run", isMoving && isGrounded && !Input.GetKey(KeyCode.LeftShift));
            animator.SetBool("idle", !isMoving && isGrounded);
        }
    }

    private void ResetAnimator()
    {
        animator.SetBool("pushing", false);
        animator.SetBool("fast-push", false);
        animator.SetBool("sprint", false);
        animator.SetBool("run", false);
        animator.SetBool("idle", true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetAnimatorSpeedServerRpc(float speed, ServerRpcParams serverRpcParams = default)
    {
        animationSpeed.Value = speed;
        UpdateAnimatorSpeedClientRpc(serverRpcParams.Receive.SenderClientId, speed);
    }

    [ClientRpc]
    private void UpdateAnimatorSpeedClientRpc(ulong clientId, float speed)
    {
        if (OwnerClientId != clientId) return;
        if (animator == null) return;
        animator.speed = speed;
    }

    public void AllowMovement()
    {
        if (!IsOwner) return;
        canMove = true;
        animator.SetBool("impact", false);
        animator.SetBool("landed", true);
        jumpYPosition = -999f;
    }

    public void PlayFootstepSound()
    {
        SoundManager.Instance.PlayAtPosition("Footstep", transform.position);
    }

    public void PlayFallFlatSound()
    {
        SoundManager.Instance.PlayAtPosition("FallFlat", transform.position);
    }

    public void PlayJumpSound()
    {
        SoundManager.Instance.PlayAtPosition("Jump", transform.position);
    }

    public void PlayPlayerLandSound()
    {
        SoundManager.Instance.PlayAtPosition("PlayerLand", transform.position);
    }

    public void PlayGettingUpSound()
    {
        SoundManager.Instance.PlayAtPosition("GettingUp", transform.position);
    }

    [ServerRpc]
    private void PlayFootstepSoundServerRpc()
    {
        PlayFootstepSoundClientRpc();
    }

    [ClientRpc]
    private void PlayFootstepSoundClientRpc()
    {
        SoundManager.Instance.PlayAtPosition("Footstep", transform.position);
    }

    public void SetMovement(bool move)
    {
        canMove = move;
    }

    public void GetHit()
    {
        if (IsOwner && animator != null) animator.SetTrigger("hit");
    }

    public void GettingHit()
    {
        beingHit = true;
    }

    public void NotGettingHit()
    {
        beingHit = false;
    }

    public void DisableMovement()
    {
        canMove = false;
    }

    public void EnableMovement()
    {
        canMove = true;
    }

    private void OnDestroy()
    {
        SisyphiGameManager.Instance.GameFinishedEvent -= Movement_OnGameFinished;
        if (movement != null) movement.action.Disable();
        if (jump != null) jump.action.started -= Jump;
    }
}