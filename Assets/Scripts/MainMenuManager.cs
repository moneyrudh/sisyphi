using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }

        if (SisyphiGameMultiplayer.Instance != null)
        {
            Destroy(SisyphiGameMultiplayer.Instance.gameObject);
        }

        if (SisyphiGameLobby.Instance != null)
        {
            Destroy(SisyphiGameLobby.Instance.gameObject);
        }
    }
}
