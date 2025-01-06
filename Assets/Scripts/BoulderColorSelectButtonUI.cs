using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoulderColorSelectButtonUI : MonoBehaviour
{
    [SerializeField] private int materialId;
    [SerializeField] private Image image;
    [SerializeField] private GameObject selectedGameObject;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => {
            SisyphiGameMultiplayer.Instance.ChangeBoulderMaterial(materialId);
        });
    }

    private void Start()
    {
        if (materialId != 0) selectedGameObject.SetActive(false);
        SisyphiGameMultiplayer.Instance.OnPlayerDataNetworkListChanged += SisyphiGameMultiplayer_OnPlayerNetworkListChanged;
    }

    private void SisyphiGameMultiplayer_OnPlayerNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdateIsSelected();
    }

    private void UpdateIsSelected()
    {
        PlayerData playerData = SisyphiGameMultiplayer.Instance.GetPlayerData();
        int materialId = playerData.boulderMaterialId;
        if (this.materialId == materialId)
        {
            selectedGameObject.SetActive(true);
        }
        else
        {
            selectedGameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        SisyphiGameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= SisyphiGameMultiplayer_OnPlayerNetworkListChanged;
    }
}
