using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class HostDisconnectUI : NetworkBehaviour
{
    [SerializeField] private Button mainMenuButton;

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
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Show();
        }
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
