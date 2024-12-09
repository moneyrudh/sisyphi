using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_InputField playerNameInput;

    [Header("Scene References")]
    [SerializeField] private string lobbySceneName = "LobbyScene";
    // Start is called before the first frame update
    void Start()
    {
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            playerNameInput.text = savedName;
        }

        hostButton.onClick.AddListener(OnHostGame);
        joinButton.onClick.AddListener(OnJoinGame);
        quitButton.onClick.AddListener(OnQuitGame);
        playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);
    }

    private void OnHostGame()
    {
        PlayerPrefs.SetInt("IsHost", 1);

        // SET UP LOBBY

        SceneManager.LoadScene(lobbySceneName);
    }

    private void OnJoinGame()
    {
        PlayerPrefs.SetInt("IsHost", 0);

        // SET UP LOBBY

        SceneManager.LoadScene(lobbySceneName);
    }

    private void OnQuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OnPlayerNameChanged(string newName)
    {
        PlayerPrefs.SetString("PlayerName", newName);
        PlayerPrefs.Save();
    }
}
