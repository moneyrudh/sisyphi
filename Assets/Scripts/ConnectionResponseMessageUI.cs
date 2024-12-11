using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class ConnectionResponseMessageUI : MonoBehaviour
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

        Hide();
    }

    private void SisyphiGameMultiplayer_OnFailedToJoinGame(object sender, System.EventArgs e)
    {
        Show();

        // Debug.Log(NetworkManager.Singleton.DisconnectReason);
        messageText.text = "Failed to connect to server.";
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
    }
}
