using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class HostDisconnectUI : MonoBehaviour 
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TMP_Text disconnectMessage;

    private void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnServerStopped += NetworkManager_OnServerStopped;
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenu);
        });

        Hide();
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log("Client " + clientId + " disconnected.");
        Debug.Log(NetworkManager.ServerClientId);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
            disconnectMessage.text = "Host has disconnected!";
        }
        else
        {
            disconnectMessage.text = "Player has disconnected!";
        }
        Show();
    }

    private void NetworkManager_OnServerStopped(bool wasAHost)
    {
        if (wasAHost)
        {
            Show();
        }
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
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
            NetworkManager.Singleton.OnServerStopped -= NetworkManager_OnServerStopped;
        }
    }
}
