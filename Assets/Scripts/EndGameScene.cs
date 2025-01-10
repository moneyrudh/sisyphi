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
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetSkyboxServerRpc()
    {
        animationIndex.Value = UnityEngine.Random.Range(0, 3);
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
    }

    public int GetAnimationIndex()
    {
        return animationIndex.Value;
    }
}
