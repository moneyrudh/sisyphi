 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    private bool isPlayerReady = false;
    private bool canReady = false;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenu); 
        });
        readyButton.onClick.AddListener(() => {
            isPlayerReady = !isPlayerReady;
            CharacterSelectReady.Instance.SetPlayerReady(isPlayerReady);
            mainMenuButton.interactable = !isPlayerReady;
            readyButtonText.text = isPlayerReady ? "UNREADY" : "READY";
        });
        readyButton.interactable = false;
    }

    private void FixedUpdate()
    {
        if (!canReady)
        {
            if (SisyphiGameMultiplayer.Instance.GetPlayerCount() == 2)
            {
                canReady = true;
                readyButton.interactable = true;
            }
        }
    }
}
