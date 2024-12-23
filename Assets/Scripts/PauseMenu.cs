using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject pauseMenuGO;

    private bool isPauseMenuOpen;

    private void Start()
    {
        quitButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenu);
        });

        isPauseMenuOpen = false;
        Hide();
    }

    private void Update()
    {
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
