using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class CameraController : NetworkBehaviour
{
    [Header("Input")]
    public InputActionReference mouseLook;

    [Header("Camera Settings")]
    public float distance = 5f;
    public float heightOffset = 1.5f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 45f;
    public float rotationSpeed = 2f;
    public Vector3 offset = new Vector3(0f, 2f, 0f);

    [Header("Smoothing")]
    public float positionSmoothTime = 0.1f;
    public float rotationSmoothTime = 0.1f;

    [Header("Camera Collision")]
    public float collisionRadius = 0.2f;
    public LayerMask collisionLayers;
    private Camera playerCamera;
    // private Vector3 currentVelocity;
    private float currentVerticalAngle;
    private float currentHorizontalAngle;
    private float targetCameraDistance;

    private void OnEnable()
    {
        if (IsOwner)
        {
            mouseLook.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (IsOwner)
        {
            mouseLook.action.Disable();
        }
    }

    void Start()
    {
        if (!IsOwner) return;

        GameObject cam = new GameObject($"PlayerCamera_{OwnerClientId}");
        playerCamera = cam.AddComponent<Camera>();
        if (Camera.main != null) playerCamera.CopyFrom(Camera.main);

        // Vector3 targetPosition = transform.position + offset;
        // playerCamera.transform.position = targetPosition - playerCamera.transform.forward * distance;

        if (Camera.main != null) Camera.main.gameObject.SetActive(false);

        targetCameraDistance = distance;
    }

    private void LateUpdate()
    {
        if (!IsOwner || playerCamera == null) return;

        Vector2 mouseInput = mouseLook.action.ReadValue<Vector2>();
        // float mouseX = mouseInput.x * rotationSpeed;
        // float mouseY = mouseInput.y * rotationSpeed;

        currentHorizontalAngle += mouseInput.x * rotationSpeed;
        currentVerticalAngle -= mouseInput.y * rotationSpeed;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

        Vector3 targetPosition = transform.position + offset;
        Quaternion targetRotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0f);

        // Vector3 targetPosition = transform.position + offset;
        // Vector3 directionFromTarget = targetRotation * Vector3.back;
        // Vector3 targetCameraPosition = targetPosition + directionFromTarget * distance;

        // playerCamera.transform.position = Vector3.SmoothDamp(
        //     playerCamera.transform.position,
        //     targetCameraPosition,
        //     ref currentVelocity,
        //     positionSmoothTime
        // );

        // playerCamera.transform.rotation = Quaternion.Slerp(
        //     playerCamera.transform.rotation,
        //     targetRotation,
        //     1f - Mathf.Exp(-rotationSmoothTime * Time.deltaTime)
        // );

        Vector3 directionToCamera = targetRotation * Vector3.back;
        RaycastHit hit;
        if (Physics.SphereCast(targetPosition, collisionRadius, directionToCamera, out hit, distance, collisionLayers))
        {
            targetCameraDistance = hit.distance;
        }
        else
        {
            targetCameraDistance = distance;
        }

        Vector3 finalPosition = targetPosition + directionToCamera * targetCameraDistance;
        finalPosition = new (finalPosition.x, Mathf.Max(2, finalPosition.y), finalPosition.z);
        playerCamera.transform.position = finalPosition;
        playerCamera.transform.rotation = targetRotation;
    }

    public Quaternion GetCameraRotation()
    {
        return Quaternion.Euler(0, currentHorizontalAngle, 0);
    }
}
