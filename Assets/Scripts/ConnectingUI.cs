using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectingUI : MonoBehaviour
{
    private void Start()
    {
        SisyphiGameMultiplayer.Instance.OnTryingToJoinGame += SisyphiGameMultiplayer_OnTryingToJoinGame;
        SisyphiGameMultiplayer.Instance.OnFailedToJoinGame += SisyphiGameManager_OnFailedToJoinGame;
        Hide();
    }

    private void SisyphiGameManager_OnFailedToJoinGame(object sender, System.EventArgs e) 
    {
        Hide();
    }

    private void SisyphiGameMultiplayer_OnTryingToJoinGame(object sender, System.EventArgs e)
    {
        Show();
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
        SisyphiGameMultiplayer.Instance.OnTryingToJoinGame -= SisyphiGameMultiplayer_OnTryingToJoinGame;
        SisyphiGameMultiplayer.Instance.OnFailedToJoinGame -= SisyphiGameManager_OnFailedToJoinGame;
    }
}
