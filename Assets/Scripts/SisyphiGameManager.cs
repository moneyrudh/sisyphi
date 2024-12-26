using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class SisyphiGameManager: NetworkBehaviour 
{
    public static SisyphiGameManager Instance { get; private set; }

    [SerializeField] private Transform playerPrefab;
    [SerializeField] private GameObject waitingForPlayersGO;

    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart);
    private Dictionary<ulong, bool> playerReadyDictionary;
    private NetworkVariable<NetworkPromptArray> prompts = new NetworkVariable<NetworkPromptArray>(new NetworkPromptArray(4));
    private const float timerDuration = 4f;
    private NetworkVariable<float> countdownTimer = new NetworkVariable<float>(timerDuration);
    private NetworkVariable<float> gameplayTimer = new NetworkVariable<float>(600f);

    public event EventHandler OnStateChanged;

    public enum State 
    {
        WaitingToStart,
        FirstPrompt,
        SecondPrompt,
        PromptGeneration,
        Countdown,
        Playing,
        Finished
    }

    private void Awake()
    {
        Instance = this;
        playerReadyDictionary = new Dictionary<ulong, bool>();
        state.OnValueChanged += State_OnValueChanged;
    }

    [ServerRpc(RequireOwnership=false)]
    public void SetPlayerJoinedServerRpc(ServerRpcParams serverRpcParams=default)
    {
        Debug.Log("Client " + serverRpcParams.Receive.SenderClientId + " ready");
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                allClientsReady = false;
                return;
            }
        }

        Debug.Log("ALL PLAYERS JOINED");
        StartGameClientRpc();
        state.Value = State.Playing;
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        waitingForPlayersGO.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }

    }

    public void State_OnValueChanged(State previous, State current)
    {
        Debug.Log("Current state: " + state.Value.ToString());
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        switch (state.Value)
        {
            case State.WaitingToStart:
                break;
            case State.FirstPrompt:
                countdownTimer.Value -= Time.deltaTime;
                if (countdownTimer.Value < -3f)
                {
                    countdownTimer.Value = timerDuration;
                    state.Value = State.SecondPrompt;
                }
                break;
            case State.SecondPrompt:
                countdownTimer.Value -= Time.deltaTime;
                if (countdownTimer.Value < -3f)
                {
                    countdownTimer.Value = 3f;
                    state.Value = State.PromptGeneration;
                }
                break;
            case State.PromptGeneration:
                break;
            case State.Countdown:
                countdownTimer.Value -= Time.deltaTime;
                if (countdownTimer.Value < 0)
                {
                    countdownTimer.Value = -Mathf.Infinity;
                    state.Value = State.Playing;
                }
                break;
            case State.Playing:
            gameplayTimer.Value -= Time.deltaTime;
                if (gameplayTimer.Value < 0)
                {
                    gameplayTimer.Value = -Mathf.Infinity;
                    state.Value = State.Finished; 
                }
                break;
            case State.Finished:
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPromptServerRpc(int index, string value)
    {
        NetworkPromptArray current = prompts.Value;
        current.StringArray[index] = value;
        prompts.Value = current;
    }

    public bool IsPlayerWaiting()
    {
        return state.Value == State.WaitingToStart;
    }

    public bool IsFirstPrompt()
    {
        return state.Value == State.FirstPrompt;
    }

    public bool IsSecondPrompt()
    {
        return state.Value == State.SecondPrompt;
    }

    public bool IsPromptGenerationState()
    {
        return state.Value == State.PromptGeneration;
    }

    public bool IsCountdown()
    {
        return state.Value == State.Countdown;
    }

    public bool IsGamePlaying()
    {
        return state.Value == State.Playing;
    }

    public bool IsGameOver()
    {
        return state.Value == State.Finished;
    }

    public int GetPlayerIndex()
    {
        return NetworkManager.Singleton.LocalClientId > 0 ? 1 : 0;
    }

    public float GetCountdownTimer()
    {
        return countdownTimer.Value;
    }

    public void SetCountdownState()
    {
        state.Value = State.Countdown;
    }

    public void PrintPrompts()
    {
        Debug.Log("Prompts:");
        foreach (string s in prompts.Value.StringArray)
        {
            Debug.Log(s);
        }
    }

    public List<string> GetPrompts()
    {
        List<string> promptsData = new List<string>();
        foreach (string s in prompts.Value.StringArray)
        {
            promptsData.Add(s);
        }
        return promptsData;
    }
}