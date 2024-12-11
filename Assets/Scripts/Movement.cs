using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class Movement : NetworkBehaviour
{
    [Header("References")]
    public InputActionReference movement;
    public InputActionReference jump;
    public GameObject pushingColliders;
    public Transform proximityCheck;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public LayerMask boulderLayer;

    private GameObject boulder;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float pushSpeed = 1.5f;
    public float jumpForce = 5f;
    public float rotationSpeed = 72f;
    public float rotationFromCameraSpeed = 10f;
    public float groundCheckDistance = 0.1f;
    public float gravityMultiplier = 2.5f;
    public float fallMultiplier = 2.5f;
    public float rotationSmoothTime = 0.1f;
    private Vector3 currentRotationVelocity;
    [System.NonSerialized] public Vector2 moveDirection;
    private Rigidbody rb;
    private Camera playerCamera;
    private CameraController cameraController;
    private Animator animator;
    private bool isJumping;
    public static bool isGrounded;
    private bool idle;
    private bool pushing;

    private NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>();

    public void OnEnable()
    {
        movement.action.Enable();
        jump.action.started += Jump;
        if (IsOwner)
        {
        }
    }

    public void OnDisable()
    {
        movement.action.Disable();
        jump.action.started -= Jump;
        if (IsOwner)
        {
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            base.OnNetworkSpawn();
            Debug.Log("Calling TileSetter");
            FindObjectOfType<TileSetter>().SetInitialGrid();
            gameObject.name = "Player_" + OwnerClientId;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        cameraController = GetComponent<CameraController>();
        boulder = GameObject.FindWithTag("Boulder");
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
        if (IsOwner)
        {
            moveDirection = movement.action.ReadValue<Vector2>();
            CheckGrounded();
            UpdateAnimator();
            CheckRockProximity();
        }
        // else
        // {
        //     transform.position = Vector3.Lerp(transform.position, netPosition.Value, Time.deltaTime * 10f);
        //     transform.rotation = Quaternion.Lerp(transform.rotation, netRotation.Value, Time.deltaTime * 10f);
        // }
    }

    private void FixedUpdate()
    {
        if (IsOwner) 
        {
            // Move();
            MoveRelativeToCamera();
            // Rotate();
            ApplyGravity();
            CheckPushing();
            // UpdateNetworkPositionServerRpc(transform.position, transform.rotation);
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

    private void MoveRelativeToCamera()
    {
        if (!IsOwner) return;
        if (playerCamera == null) return;
        
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up).normalized;

        Vector3 movement = (forward * moveDirection.y + right * moveDirection.x).normalized;

        float currentSpeed = pushing ? pushSpeed : (Input.GetKey(KeyCode.LeftShift) ? moveSpeed * sprintMultiplier : moveSpeed);
        rb.MovePosition(rb.position + movement * currentSpeed * Time.fixedDeltaTime);
        // Quaternion targetRotation = Quaternion.LookRotation(movement);
        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            rb.MoveRotation(Quaternion.Lerp(rb.rotation, targetRotation, rotationFromCameraSpeed * Time.fixedDeltaTime));
        }
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
        if (isGrounded && !isJumping)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // Reset Y velocity
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
            animator.SetTrigger("jump");
        }
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.SphereCast(
            groundCheck.position + Vector3.up * 0.2f, // Start slightly above to avoid false negatives
            0.2f, // Radius of sphere
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance + 0.1f,
            groundLayer
        );

        if (isGrounded && rb.velocity.y < 0.1f)
        {
            isJumping = false;
        }
    }

    private void CheckRockProximity()
    {
        RaycastHit hit;
        pushing = Physics.Raycast(proximityCheck.position, transform.TransformDirection(Vector3.forward), out hit, 0.75f, boulderLayer);
        pushingColliders.SetActive(pushing);
        if (pushing)
        {
            boulder = hit.collider.gameObject;
        }
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

        if (pushing && isGrounded)
        {
            animator.SetBool("pushing", true);
            animator.SetBool("fast-push", Input.GetKey(KeyCode.LeftShift));
            animator.speed = isMoving ? 1f : 0.1f;
        }
        else
        {
            animator.speed = 1f;
            animator.SetBool("pushing", false);
            animator.SetBool("sprint", isMoving && isGrounded && Input.GetKey(KeyCode.LeftShift));
            animator.SetBool("run", isMoving && isGrounded && !Input.GetKey(KeyCode.LeftShift));
            animator.SetBool("idle", !isMoving && isGrounded);
        }
    }
}