using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private LobbyCreateUI lobbyCreateUI;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private Transform lobbyTemplate;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => {
            SisyphiGameLobby.Instance.LeaveLobby();
            Loader.Load(Loader.Scene.MainMenu);
        });
        createLobbyButton.onClick.AddListener(() => {
            lobbyCreateUI.Show();
        });
        quickJoinButton.onClick.AddListener(() => {
            SisyphiGameLobby.Instance.QuickJoin();
        });
        joinCodeButton.onClick.AddListener(() => {
            SisyphiGameLobby.Instance.JoinWithCode(joinCodeInputField.text);
        });

        lobbyTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        playerNameInputField.text = SisyphiGameMultiplayer.Instance.GetPlayerName();
        playerNameInputField.onValueChanged.AddListener((string newText) => {
            SisyphiGameMultiplayer.Instance.SetPlayerName(newText);
        });

        SisyphiGameLobby.Instance.OnLobbyListChanged += SisyphiGameLobby_OnLobbyListChanged;
        UpdateLobbyList(new List<Lobby>());
    }

    private void SisyphiGameLobby_OnLobbyListChanged(object sender, SisyphiGameLobby.OnLobbyListChangedEventArgs e)
    {
        UpdateLobbyList(e.lobbyList);
    }

    private void UpdateLobbyList(List<Lobby> lobbyList)
    {
        if (lobbyContainer == null) return;
        foreach (Transform child in lobbyContainer)
        {
            if (child == lobbyTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            Transform lobbyTransform = Instantiate(lobbyTemplate, lobbyContainer);
            lobbyTransform.gameObject.SetActive(true);
            if (lobby.AvailableSlots == 0)
            {
                lobbyTransform.GetComponent<Button>().interactable = false;
            }
            lobbyTransform.GetComponent<LobbyListSingleUI>().SetLobby(lobby);
        }
    }
}
