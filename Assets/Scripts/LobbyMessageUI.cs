using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class LobbyMessageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(Hide);
    }

    private void Start()
    {
        SisyphiGameMultiplayer.Instance.OnFailedToJoinGame += SisyphiGameMultiplayer_OnFailedToJoinGame;
        SisyphiGameLobby.Instance.OnCreateLobbyStarted += SisyphiGameLobby_OnCreateLobbyStarted;
        SisyphiGameLobby.Instance.OnCreateLobbyFailed += SisyphiGameLobby_OnCreateLobbyFailed;
        SisyphiGameLobby.Instance.OnJoinStarted += SisyphiGameLobby_OnJoinStarted;
        SisyphiGameLobby.Instance.OnJoinFailed += SisyphiGameLobby_OnJoinFailed;
        SisyphiGameLobby.Instance.OnQuickJoinFailed += SisyphiGameLobby_OnQuickJoinFailed;

        Hide();
    }

    private void SisyphiGameLobby_OnJoinStarted(object sender, System.EventArgs e)
    {
        ShowMessage("Attempting to join game...");
    }

    private void SisyphiGameLobby_OnJoinFailed(object sender, System.EventArgs e)
    {
        ShowMessage("Failed to join game...");
    }

    private void SisyphiGameLobby_OnQuickJoinFailed(object sender, System.EventArgs e)
    {
        ShowMessage("Failed to quick join...");
    }

    private void SisyphiGameLobby_OnCreateLobbyFailed(object sender, System.EventArgs e)
    {
        ShowMessage("Lobby creation failed...");
    }


    private void SisyphiGameLobby_OnCreateLobbyStarted(object sender, System.EventArgs e)
    {
        ShowMessage("Creating lobby...");
    }

    private void SisyphiGameMultiplayer_OnFailedToJoinGame(object sender, System.EventArgs e)
    {
        if (NetworkManager.Singleton.DisconnectReason == "")
        {
            ShowMessage("Failed to connect to server.");
        }
        else
        {
            ShowMessage(NetworkManager.Singleton.DisconnectReason);
        }
    }

    private void ShowMessage(string message)
    {
        Show();
        messageText.text = message;
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        SisyphiGameMultiplayer.Instance.OnFailedToJoinGame -= SisyphiGameMultiplayer_OnFailedToJoinGame;
        SisyphiGameLobby.Instance.OnCreateLobbyStarted -= SisyphiGameLobby_OnCreateLobbyStarted;
        SisyphiGameLobby.Instance.OnCreateLobbyFailed -= SisyphiGameLobby_OnCreateLobbyFailed;
        SisyphiGameLobby.Instance.OnJoinStarted -= SisyphiGameLobby_OnJoinStarted;
        SisyphiGameLobby.Instance.OnJoinFailed -= SisyphiGameLobby_OnJoinFailed;
        SisyphiGameLobby.Instance.OnQuickJoinFailed -= SisyphiGameLobby_OnQuickJoinFailed;
    }
}
