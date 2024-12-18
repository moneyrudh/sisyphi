 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using Unity.Services.Lobbies.Models;

public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    private bool isPlayerReady = false;
    private bool canReady = false;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenu); 
        });
        readyButton.onClick.AddListener(() => {
            UpdateReadyButton(!isPlayerReady);
        });
        readyButton.interactable = false;
    }

    private void Start()
    {
        Lobby lobby = SisyphiGameLobby.Instance.GetLobby();

        lobbyNameText.text = "Lobby Name: " + lobby.Name;
        lobbyCodeText.text = "Lobby Code: " + lobby.LobbyCode;
    }

    private void FixedUpdate()
    {
        if (SisyphiGameMultiplayer.Instance.GetPlayerCount() == 2)
        {
            canReady = true;
            readyButton.interactable = true;
        }
        else
        {
            UpdateReadyButton(false);
            readyButton.interactable = false;
        }
    }

    private void UpdateReadyButton(bool ready)
    {
        isPlayerReady = ready;
        CharacterSelectReady.Instance.SetPlayerReady(isPlayerReady);
        mainMenuButton.interactable = !isPlayerReady;
        readyButtonText.text = isPlayerReady ? "UNREADY" : "READY";
    }
}
