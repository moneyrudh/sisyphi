using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public class CharacterSelectReady : NetworkBehaviour
{
    public static CharacterSelectReady Instance { get; private set; }
    public event EventHandler OnReadyPlayer;
    private Dictionary<ulong, bool> playerReadyDictionary;

    private void Awake()
    {
        Instance = this;

        playerReadyDictionary = new Dictionary<ulong, bool>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
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
        string log = @"Inside SetSkyboxClientRpc with skybox settings:
            Skybox: " + skyboxMaterial.material + @"
            Fog Color: " + skyboxMaterial.fogColor + @"
            Fog Mode: " + skyboxMaterial.fogMode.ToString();
        Debug.Log(log);
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

        DynamicGI.UpdateEnvironment();
    }

    public void SetPlayerReady(bool ready)
    {
        SetPlayerReadyServerRpc(ready);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(bool ready, ServerRpcParams serverRpcParams = default)
    {
        SetPlayerReadyClientRpc(serverRpcParams.Receive.SenderClientId, ready);

        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = ready;

        bool allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                allClientsReady = false;
                break;
            }
        }

        if (NetworkManager.Singleton.ConnectedClientsIds.Count < 2)
        {
            return;
        }

        if (allClientsReady)
        {
            SisyphiGameLobby.Instance.DeleteLobby();
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }

    [ClientRpc]
    private void SetPlayerReadyClientRpc(ulong clientId, bool ready)
    {
        playerReadyDictionary[clientId] = ready;

        OnReadyPlayer?.Invoke(this, EventArgs.Empty);
    }

    public bool IsPlayerReady(ulong clientId)
    {
        return playerReadyDictionary.ContainsKey(clientId) && playerReadyDictionary[clientId];
    }
}
