using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseSettings : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private Slider mouseSensitivitySlider;
    private CameraController playerCamera;

    private void Start()
    {
        mouseSensitivitySlider.onValueChanged.AddListener(HandleMouseSensitivityChanged);

        mouseSensitivitySlider.value = 1f;

        StartCoroutine(FindLocalPlayer());
    }

    private IEnumerator FindLocalPlayer()
    {
        yield return new WaitForSeconds(0.1f);

        CameraController[] cameras = FindObjectsOfType<CameraController>();
        foreach (CameraController cam in cameras)
        {
            if (cam.IsOwner)
            {
                playerCamera = cam;
                break;
            }
        }
    }

    private void HandleMouseSensitivityChanged(float sensitivity)
    {
        if (playerCamera != null)
        {
            playerCamera.UpdateSensitivity(sensitivity);
        }
    }

    private void OnDestroy()
    {
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.RemoveAllListeners();
    }
}
