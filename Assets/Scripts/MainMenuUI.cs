using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button quitButton;

    [Header("Cursor Settings")]
    [SerializeField] private Texture2D cursorTexture;
    private CursorMode cursorMode = CursorMode.Auto;
    public Vector2 hotSpot = Vector2.zero;

    [Header("Pause Menu")]
    [SerializeField] private GameObject PauseMenuGO;
    [SerializeField] private Button closeButton;

    [Header("How To Play Menu")]
    [SerializeField] private GameObject HowToPlayGO;
    [SerializeField] private Button htpCloseButton;

    private void Awake()
    {
        Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);

        playButton.onClick.AddListener(() => {
            Loader.Load(Loader.Scene.LobbyScene);
        });
        settingsButton.onClick.AddListener(() => {
            PauseMenuGO.SetActive(true);
        });
        howToPlayButton.onClick.AddListener(() => {
            HowToPlayGO.SetActive(true);
        });
        quitButton.onClick.AddListener(() => {
            Application.Quit();
        });

        closeButton.onClick.AddListener(() => {
            HideSettings();
        });

        htpCloseButton.onClick.AddListener(() => {
            HideHTP();
        });

        Time.timeScale = 1f;
        HideSettings();
        HideHTP();
    }
    
    private void HideSettings()
    {
        PauseMenuGO.SetActive(false);
    }

    private void HideHTP()
    {
        HowToPlayGO.SetActive(false);
    }
}
