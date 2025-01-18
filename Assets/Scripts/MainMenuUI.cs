using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Pause Menu")]
    [SerializeField] private GameObject PauseMenuGO;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        playButton.onClick.AddListener(() => {
            Loader.Load(Loader.Scene.LobbyScene);
        });
        settingsButton.onClick.AddListener(() => {
            PauseMenuGO.SetActive(true);
        });
        quitButton.onClick.AddListener(() => {
            Application.Quit();
        });

        closeButton.onClick.AddListener(() => {
            Hide();
        });
        Time.timeScale = 1f;
        Hide();
    }
    
    private void Hide()
    {
        PauseMenuGO.SetActive(false);
    }
}
