using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Text.Json.Serialization;

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

    [Header("Sensitivity")]
    public float mouseSensitivityMultiplier = 1f;
    private float currentSensitivity = 1f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.1f;
    public float rotationSmoothTime = 0.1f;

    [Header("Camera Collision")]
    public float collisionRadius = 0.2f;
    public LayerMask collisionLayers;

    [Header("Cinematic Points")]
    public Transform cinematicPointA;
    public Transform cinematicPointB;
    private bool isCinematicActive = false;
    private Vector3 initialOffset;
    private float cinematicLerpSpeed = 0.1f;

    private Camera playerCamera;
    // private Vector3 currentVelocity;
    private float currentVerticalAngle;
    private float currentHorizontalAngle;
    private float targetCameraDistance;
    private const float minCameraY = 2f;

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

        playerCamera.transform.position = cinematicPointA.position;
        playerCamera.transform.rotation = cinematicPointA.rotation;
        cam.AddComponent<AudioListener>();
        Debug.Log("ALREADY SET BRUH");
        targetCameraDistance = distance;
    }

    public void StartCinematic()
    {
        if (!IsOwner) return;
        isCinematicActive = true;
        initialOffset = offset;
        StartCoroutine(PlayCinematicSequence());
    }

    private IEnumerator PlayCinematicSequence()
    {
        mouseLook.action.Disable();

        playerCamera.transform.position = cinematicPointA.position;
        playerCamera.transform.rotation = cinematicPointA.rotation;
        yield return new WaitForSeconds(2f);
        Debug.Log("POINT A TO B START");
        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * cinematicLerpSpeed;
            playerCamera.transform.position = Vector3.Lerp(cinematicPointA.position, cinematicPointB.position, elapsedTime);
            playerCamera.transform.rotation = Quaternion.Lerp(cinematicPointA.rotation, cinematicPointB.rotation, elapsedTime);
            yield return null;
        }

        Debug.Log("POINT A TO B END");
        yield return new WaitForSeconds(2f);
        Debug.Log("POINT B TO PLAYER START");

        Vector3 playerPosition = transform.position + initialOffset;
        Vector3 initialCameraPosition = playerPosition + (Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0f) * Vector3.back * distance);
        initialCameraPosition.y = Mathf.Max(minCameraY, initialCameraPosition.y);
        Quaternion initialCameraRotation = Quaternion.LookRotation(playerPosition - initialCameraPosition);

        elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * cinematicLerpSpeed * 6f;
            playerCamera.transform.position = Vector3.Lerp(cinematicPointB.position, initialCameraPosition, elapsedTime);
            playerCamera.transform.rotation = Quaternion.Lerp(cinematicPointB.rotation, initialCameraRotation, elapsedTime);
            yield return null;
        }

        Debug.Log("POINT B TO PLAYER END");
        yield return new WaitForSeconds(1f);

        isCinematicActive = false;
        mouseLook.action.Enable();

        SisyphiGameManager.Instance.CinematicCompleteServerRpc();
    }

    private void LateUpdate()
    {
        if (!SisyphiGameManager.Instance.IsGamePlaying()) return;
        if (!IsOwner || playerCamera == null || isCinematicActive) return;

        Vector2 mouseInput = mouseLook.action.ReadValue<Vector2>();
        // float mouseX = mouseInput.x * rotationSpeed;
        // float mouseY = mouseInput.y * rotationSpeed;

        currentHorizontalAngle += mouseInput.x * rotationSpeed * currentSensitivity;
        currentVerticalAngle -= mouseInput.y * rotationSpeed * currentSensitivity;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

        Vector3 targetPosition = transform.position + offset;
        Quaternion targetRotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0f);
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
        finalPosition.y = Mathf.Max(minCameraY, finalPosition.y);
        
        playerCamera.transform.position = finalPosition;

        Vector3 lookTarget = transform.position + offset;
        // playerCamera.transform.rotation = targetRotation;
        playerCamera.transform.rotation = Quaternion.LookRotation(lookTarget - finalPosition);
    }

    public Quaternion GetCameraRotation()
    {
        return Quaternion.Euler(0, currentHorizontalAngle, 0);
    }

    public void UpdateSensitivity(float sensitivity)
    {
        currentSensitivity = sensitivity * mouseSensitivityMultiplier;
    }
}
