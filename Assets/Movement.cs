using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    public InputActionReference movement;
    public InputActionReference jump;
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 0.1f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float gravityMultiplier = 2.5f;
    public float fallMultiplier = 2.5f;
    public float rotationSpeed = 72f;

    private Vector2 moveDirection;
    private Rigidbody rb;
    private Animator animator;
    private bool isJumping;
    private bool isGrounded;
    private bool idle;
    private bool pushing;

    private void OnEnable()
    {
        movement.action.Enable();
        jump.action.started += Jump;
    }

    private void OnDisable()
    {
        movement.action.Disable();
        jump.action.started -= Jump;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        moveDirection = movement.action.ReadValue<Vector2>();
        CheckGrounded();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Move();
        Rotate();
        ApplyGravity();
    }

    private void Move()
    {
        Vector3 movement = new Vector3(moveDirection.x, 0, moveDirection.y).normalized;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void Rotate() {
        if (moveDirection != Vector2.zero) {
            Quaternion toRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, moveDirection.y), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void Jump(InputAction.CallbackContext context)
    {
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
        if (Input.GetKey(KeyCode.LeftShift) && !pushing) {
            pushing = true;
            animator.SetBool("pushing", pushing);
            moveSpeed = 1.5f;
            return;
        } 
        // if (!isMoving && pushing) {
        //     pushing = false;
        //     animator.SetBool("pushing", pushing);
        //     moveSpeed = 5f;
        // }
        if (Input.GetKeyUp(KeyCode.LeftShift) && pushing) {
            pushing = false;
            animator.SetBool("pushing", pushing);
            moveSpeed = 5f;
        }
        
        if (!pushing) {
            animator.SetBool("run", isMoving && isGrounded && !pushing);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("idle", !isMoving && isGrounded && rb.velocity.x <= 0.1f);
        }
    }
}