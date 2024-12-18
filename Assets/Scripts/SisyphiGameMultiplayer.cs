using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;

public class SisyphiGameMultiplayer : NetworkBehaviour
{
    public const int PLAYER_COUNT = 2;
    private const string PLAYER_PREFS_NAME_MULTIPLAYER = "PlayerNameMultiplayer";

    private NetworkList<PlayerData> playerDataNetworkList;
    [SerializeField] private List<Color> playerColors;
    private NetworkList<NetworkedMaterialCategory> currentMaterialCategory;
    public static SisyphiGameMultiplayer Instance { get; private set; }

    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler OnPlayerDataNetworkListChanged;
    public event EventHandler OnMaterialCategoryNetworkListChanged;

    private string playerName;

    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerDataNetworkList = new NetworkList<PlayerData>();
        currentMaterialCategory = new NetworkList<NetworkedMaterialCategory>();
        
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
        currentMaterialCategory.OnListChanged += MaterialCategory_OnListChanged;

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_NAME_MULTIPLAYER, "Player_" + UnityEngine.Random.Range(1000, 9999));
    }

    public void ChangeCurrentMaterialCategory(MaterialCategory materialCategory)
    {
        int playerIndex = GetPlayerDataIndexFromClientId(NetworkManager.Singleton.LocalClientId);
        if (playerIndex >= 0 && playerIndex < currentMaterialCategory.Count)
        {
            UpdateMaterialCategoryServerRpc(playerIndex, materialCategory);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateMaterialCategoryServerRpc(int playerIndex, MaterialCategory category)
    {
        if (playerIndex >= 0 && playerIndex < currentMaterialCategory.Count)
        {
            currentMaterialCategory[playerIndex] = new NetworkedMaterialCategory { Value = category };
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            currentMaterialCategory.Add(new NetworkedMaterialCategory
            {
                Value = MaterialCategory.Hair
            });
        }
    }

    public void StartHost() {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        for(int i=0; i<playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId)
            {
                playerDataNetworkList.RemoveAt(i);
                currentMaterialCategory.RemoveAt(i);
            }
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log("Client joined with id " + clientId);
        playerDataNetworkList.Add(new PlayerData {
            clientId = clientId,
            hairColorId = 1,
            skinColorId = 10,
            pantColorId = 12,
            eyesColorId = 11
        });
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
        SetPlayerNameServerRpc(GetPlayerName());

        if (IsServer)
        {
            currentMaterialCategory.Add(new NetworkedMaterialCategory
            {
                Value = MaterialCategory.Hair
            });
        }
    }

    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvvent)
    {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MaterialCategory_OnListChanged(NetworkListEvent<NetworkedMaterialCategory> changeEvent)
    {
        OnMaterialCategoryNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        if (SceneManager.GetActiveScene().name == Loader.Scene.CharacterScene.ToString())
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game has already started";
            return;
        }

        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= PLAYER_COUNT)
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game is full";
            return;
        }

        connectionApprovalResponse.Approved = false;
    }

    public void StartClient() {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId) 
    {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId)
    {
        SetPlayerNameServerRpc(GetPlayerName());
    }

    [ServerRpc(RequireOwnership=false)]
    private void SetPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams=default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerName = playerName;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    public bool IsPlayerIndexConnected(int playerIndex)
    {
        return playerIndex < playerDataNetworkList.Count;
    }

    public PlayerData GetPlayerDataFromClientId(ulong clientId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.clientId == clientId)
            {
                return playerData;
            }
        }
        return default;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i=0; i<playerDataNetworkList.Count; i++)
        {
            if (playerDataNetworkList[i].clientId == clientId){
                return i;
            }
        }
        return -1;
    }

    public PlayerData GetPlayerData()
    {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex)
    {
        return playerDataNetworkList[playerIndex];
    }

    public Color GetPlayerColor(int colorId)
    {
        return playerColors[colorId];
    }

    public void ChangePlayerColor(int colorId)
    {
        ChangePlayerColorServerRpc(colorId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerColorServerRpc(int colorId, ServerRpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];
        switch (currentMaterialCategory[playerDataIndex].Value)
        {
            case MaterialCategory.Hair:
                playerData.hairColorId = colorId;
                break;
            case MaterialCategory.Skin:
                playerData.skinColorId = colorId;
                break;
            case MaterialCategory.Pant:
                playerData.pantColorId = colorId;
                break;
            case MaterialCategory.Eyes:
                playerData.eyesColorId = colorId;
                break;
        }
        // playerData.colorId = colorId;
        playerDataNetworkList[playerDataIndex] = playerData;
    }

    public MaterialCategory GetCurrentMaterialCategory(ulong clientId)
    {
        int playerIndex = GetPlayerDataIndexFromClientId(clientId);
        if (playerIndex >= 0 && playerIndex < currentMaterialCategory.Count)
        {
            return currentMaterialCategory[playerIndex];
        }
        return MaterialCategory.Hair;
    }

    public void KickPlayer(ulong clientId)
    {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }

    public int GetPlayerCount()
    {
        return playerDataNetworkList.Count;
    }

    public ulong GetPlayerClientId()
    {
        return NetworkManager.Singleton.LocalClientId;
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_NAME_MULTIPLAYER, playerName);
    }

    // public override void OnDestroy()
    // {
    //     Dispose();
    //     base.OnDestroy();
    // }

    private void OnApplicationQuit()
    {
        // Dispose();
    }

    // private void Dispose()
    // {
    //     if (playerDataNetworkList != null)
    //     {
    //         playerDataNetworkList.Dispose();
    //         playerDataNetworkList = null;
    //     }

    //     if (currentMaterialCategory != null)
    //     {
    //         currentMaterialCategory.Dispose();
    //         currentMaterialCategory = null;
    //     }
    // }
}
