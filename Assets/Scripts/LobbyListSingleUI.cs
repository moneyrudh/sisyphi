using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using TMPro;

public class LobbyListSingleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI availableSlotsUI;

    private Lobby lobby;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => {
            SisyphiGameLobby.Instance.JoinWithId(lobby.Id);
        });
    }

    public void SetLobby(Lobby lobby)
    {
        this.lobby = lobby;
        lobbyNameText.text = lobby.Name;
        int occupiedSlots = SisyphiGameMultiplayer.PLAYER_COUNT - lobby.AvailableSlots;
        availableSlotsUI.text = occupiedSlots + " / " + SisyphiGameMultiplayer.PLAYER_COUNT;
    }
}
