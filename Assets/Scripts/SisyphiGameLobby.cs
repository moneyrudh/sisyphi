using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class SisyphiGameLobby : MonoBehaviour
{
    public static SisyphiGameLobby Instance { get; private set; }

    private Lobby joinedLobby;
    private float heartBeatTimer = 30f;

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync();

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void Update()
    {
        HandleHeartBeat();
    }

    private void HandleHeartBeat()
    {
        if (IsLobbyHost())
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer <= 0f)
            {
                float heartBeatTimerMax = 15f;
                heartBeatTimer = heartBeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, SisyphiGameMultiplayer.PLAYER_COUNT, new CreateLobbyOptions {
                IsPrivate = isPrivate,
            });

            SisyphiGameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterScene);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void QuickJoin()
    {
        try {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            SisyphiGameMultiplayer.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void JoinWithCode(string lobbyCode)
    {
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            SisyphiGameMultiplayer.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }
}