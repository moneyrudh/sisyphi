using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class Movement : NetworkBehaviour
{
    public InputActionReference movement;
    public InputActionReference jump;
    public GameObject pushingColliders;
    public Transform proximityCheck;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public LayerMask boulderLayer;

    private GameObject rock;

    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 0.1f;
    public float gravityMultiplier = 2.5f;
    public float fallMultiplier = 2.5f;
    public float rotationSpeed = 72f;
    public float rotationSmoothTime = 0.1f;
    private Vector3 currentRotationVelocity;
    private Vector2 moveDirection;
    private Rigidbody rb;
    private Animator animator;
    private bool isJumping;
    private bool isGrounded;
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

    // private void OnNetworkSpawn()
    // {
    //     if (IsOwner)
    //     {
    //         movement.action.Enable();
    //         jump.action.started += Jump;
    //     }
    // }

    // private void OnNetworkDespawn()
    // {
    //     if (IsOwner)
    //     {
    //         movement.action.Disable();
    //         jump.action.started -= Jump;
    //     }
    // }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        rock = GameObject.FindWithTag("Rock");
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
            Move();
            Rotate();
            ApplyGravity();
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
        Vector3 movement = new Vector3(moveDirection.x, 0, moveDirection.y).normalized;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        // if (pushing) {
        //     rock.GetComponent<Rigidbody>().MovePosition(rock.GetComponent<Rigidbody>().position + movement * moveSpeed / 3f * Time.fixedDeltaTime);
        // }
    }

    private void Rotate() {
        if (!IsOwner) return;
        if (pushing) 
        {
            Vector3 relativePos = (rock.transform.position - transform.position).normalized;
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
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundLayer);

        if (isGrounded && rb.velocity.y < 0.1f)
        {
            isJumping = false;
        }
    }

    private void CheckRockProximity()
    {
        if (Physics.Raycast(proximityCheck.position, transform.TransformDirection(Vector3.forward), 0.75f, boulderLayer))
        {
            pushing = true;
            pushingColliders.SetActive(true);
        }
        else
        {
            pushing = false;
            pushingColliders.SetActive(false);
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
        bool isMoving = moveDirection.magnitude > 0.1f;
        if (pushing && isGrounded) {
            animator.SetBool("pushing", true);
            if (Input.GetKey(KeyCode.LeftShift)) {
                animator.SetBool("fast-push", true);
                moveSpeed = 2.5f;
            } else {
                animator.SetBool("fast-push", false);
                moveSpeed = 1.5f;
            }
            if (!isMoving) {
                animator.speed = 0.1f;
            } else {
                animator.speed = 1;
            }
            return;
        }

        animator.speed = 1;
        if (!pushing) {
            animator.SetBool("pushing", pushing);
            if (Input.GetKey(KeyCode.LeftShift)) {
                animator.SetBool("sprint", isMoving && isGrounded);
                animator.SetBool("run", false);
                moveSpeed = 7.5f;
            } else {
                animator.SetBool("run", isMoving && isGrounded);
                animator.SetBool("sprint", false);
                moveSpeed = 5f;
            }
            animator.SetBool("idle", !isMoving && isGrounded && rb.velocity.x <= 0.1f);
        }
    }
}