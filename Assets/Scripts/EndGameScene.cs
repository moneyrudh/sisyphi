using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EndGameScene : NetworkBehaviour
{
    public static EndGameScene Instance { get; private set; }

    private NetworkVariable<int> animationIndex = new NetworkVariable<int>(0);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    void Start()
    {
        SetSkybox();

        if (NetworkManager.Singleton.IsServer) animationIndex.Value = UnityEngine.Random.Range(0, 4);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetSkyboxServerRpc()
    {
        SetSkyboxClientRpc();
    }

    [ClientRpc]
    public void SetSkyboxClientRpc()
    {
        SetSkybox();
    }

    private void SetSkybox()
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
        Debug.Log("Winner client id: " + SisyphiGameMultiplayer.Instance.GetWinnerClientId());
        if (SisyphiGameMultiplayer.Instance.IsLocalPlayerWinner())
        {
            Debug.Log("Local player is the WINNER");
            SoundManager.Instance.Play("Victory");
        }
        else
        {
            Debug.Log("Local player is NOT the WINNER");
            SoundManager.Instance.Play("Loss");
        }
    }

    public int GetAnimationIndex()
    {
        return animationIndex.Value;
    }
}
