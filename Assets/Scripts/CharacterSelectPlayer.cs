using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class CharacterSelectPlayer : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] private GameObject readyGameObject;
    [SerializeField] private PlayerVisual playerVisual;
    [SerializeField] private Button kickButton;
    [SerializeField] private TextMeshPro playerNameText;
    
    private void Awake()
    {
        kickButton.onClick.AddListener(() => {
            PlayerData playerData = SisyphiGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            SisyphiGameLobby.Instance.KickPlayer(playerData.playerId.ToString());
            SisyphiGameMultiplayer.Instance.KickPlayer(playerData.clientId);
        });
    }

    private void Start()
    {
        SisyphiGameMultiplayer.Instance.OnPlayerDataNetworkListChanged += SisyphiGameMultiplayer_OnPlayerNetworkListChanged;
        CharacterSelectReady.Instance.OnReadyPlayer += CharacterSelectReady_OnReadyChanged;

        readyGameObject.SetActive(false);
        kickButton.gameObject.SetActive(NetworkManager.Singleton.IsServer && playerIndex > 0);

        UpdatePlayer();
    }

    private void CharacterSelectReady_OnReadyChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }

    private void SisyphiGameMultiplayer_OnPlayerNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }

    private void UpdatePlayer()
    {
        if (SisyphiGameMultiplayer.Instance.IsPlayerIndexConnected(playerIndex))
        {
            Show();

            PlayerData playerData = SisyphiGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            readyGameObject.SetActive(CharacterSelectReady.Instance.IsPlayerReady(playerData.clientId));

            MaterialCategory materialCategory = SisyphiGameMultiplayer.Instance.GetCurrentMaterialCategory(playerData.clientId);
            Color color = Color.black;
            switch (materialCategory)
            {
                case MaterialCategory.Hair:
                    color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.hairColorId);
                    break;
                case MaterialCategory.Skin:
                    color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.skinColorId);
                    break;
                case MaterialCategory.Pant:
                    color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.pantColorId);
                    break;
                case MaterialCategory.Eyes:
                    color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.eyesColorId);
                    break;
            }

            playerNameText.text = playerData.playerName.ToString();
            
            playerVisual.SetPlayerColor(materialCategory, color);
        }
        else
        {
            Hide();
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
        SisyphiGameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= SisyphiGameMultiplayer_OnPlayerNetworkListChanged;
        CharacterSelectReady.Instance.OnReadyPlayer -= CharacterSelectReady_OnReadyChanged;
    }
}
