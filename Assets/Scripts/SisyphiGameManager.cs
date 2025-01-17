using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;

public class SisyphiGameManager: NetworkBehaviour 
{
    public static SisyphiGameManager Instance { get; private set; }

    [SerializeField] private Transform playerPrefab;
    [SerializeField] private GameObject waitingForPlayersGO;

    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart);
    private Dictionary<ulong, bool> playerReadyDictionary;
    private NetworkVariable<NetworkPromptArray> prompts = new NetworkVariable<NetworkPromptArray>(new NetworkPromptArray(4));
    private const float timerDuration = 4f;
    private NetworkVariable<int> cinematicCompletionCount = new NetworkVariable<int>(0);
    private NetworkVariable<float> countdownTimer = new NetworkVariable<float>(timerDuration);
    private NetworkVariable<float> gameplayTimer = new NetworkVariable<float>(600f);

    public event EventHandler GameFinishedEvent;
    public NetworkVariable<bool> gameOver = new NetworkVariable<bool>(false);

    public event EventHandler OnStateChanged;

    public enum State 
    {
        WaitingToStart,
        FirstPrompt,
        SecondPrompt,
        PromptGeneration,
        Cinematic,
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

    private void Start()
    {
        SoundManager.Instance.Stop("Theme");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetSkyboxServerRpc()
    {
        SetSkyboxClientRpc();
    }

    [ClientRpc]
    public void SetSkyboxClientRpc()
    {
        SkyboxMaterial skyboxMaterial = SisyphiGameMultiplayer.Instance.GetSkyboxData();
        RenderSettings.skybox = skyboxMaterial.material;
        RenderSettings.fog = true;
        RenderSettings.fogColor = skyboxMaterial.fogColor;
        RenderSettings.fogMode = skyboxMaterial.fogMode;
        switch (skyboxMaterial.fogMode)
        {
            case FogMode.Linear:
            {
                RenderSettings.fogStartDistance = skyboxMaterial.startDistance;
                RenderSettings.fogEndDistance = skyboxMaterial.endDistance;
            }
            break;
            case FogMode.Exponential:
            {
                RenderSettings.fogDensity = skyboxMaterial.density;
            }
            break;
        }
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

        StartGameClientRpc();
        // state.Value = State.Playing;
        state.Value = State.FirstPrompt;
        // StartCinematicClientRpc();
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
            SetSkyboxServerRpc();
        }

    }

    public void State_OnValueChanged(State previous, State current)
    {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (SceneManager.GetActiveScene().name == Loader.Scene.EndScene.ToString()) return;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if (gameOver.Value) return;

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
            case State.Cinematic:
                if (cinematicCompletionCount.Value == SisyphiGameMultiplayer.PLAYER_COUNT)
                {
                    state.Value = State.Countdown;
                    cinematicCompletionCount.Value = 0;
                }
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
                    SetGameFinishedServerRpc();
                }
                break;
            case State.Finished:
                GameFinishedEvent?.Invoke(this, EventArgs.Empty);
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

    [ServerRpc(RequireOwnership = false)]
    public void SetGameFinishedServerRpc()
    {
        state.Value = State.Finished;
        gameOver.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CinematicCompleteServerRpc(ServerRpcParams serverRpcParams = default)
    {
        cinematicCompletionCount.Value++;
    }

    [ServerRpc]
    public void StartCinematicServerRpc()
    {
        state.Value = State.Cinematic;
        StartCinematicClientRpc();
    }

    [ClientRpc]
    private void StartCinematicClientRpc()
    {
        NetworkObject localPlayerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObj != null)
        {
            CameraController cameraController = localPlayerObj.GetComponent<CameraController>();
            cameraController?.StartCinematic();
        }
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

    public bool IsCinematicState()
    {
        return state.Value == State.Cinematic;
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

    public float GetGameplayTimer()
    {
        return gameplayTimer.Value;
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

    private void OnDestroy()
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
        }
    }
}