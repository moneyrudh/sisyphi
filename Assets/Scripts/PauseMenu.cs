using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject pauseMenuGO;
    [SerializeField] private GameObject quitConfirmationGO;

    private bool isPauseMenuOpen;

    private void Start()
    {
        quitButton.onClick.AddListener(() => {
            quitConfirmationGO.SetActive(true);
        });

        isPauseMenuOpen = false;
        Hide();
    }

    private void Update()
    {
        if (!SisyphiGameManager.Instance.IsGamePlaying()) return;
        if (!isPauseMenuOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            isPauseMenuOpen = true;
            Show();
        }
        else if(isPauseMenuOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            isPauseMenuOpen = false;
            Hide();
        }
    }

    private void Show()
    {
        pauseMenuGO.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Hide()
    {
        pauseMenuGO.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
