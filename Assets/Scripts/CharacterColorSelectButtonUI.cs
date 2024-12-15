using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterColorSelectButtonUI : MonoBehaviour
{
    [SerializeField] private int colorId;
    [SerializeField] private Image image;
    [SerializeField] private GameObject selectedGameObject;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => {
            SisyphiGameMultiplayer.Instance.ChangePlayerColor(colorId);
        });
    }

    private void Start()
    {
        SisyphiGameMultiplayer.Instance.OnPlayerDataNetworkListChanged += SisyphiGameMultiplayer_OnPlayerNetworkListChanged;
        SisyphiGameMultiplayer.Instance.OnMaterialCategoryNetworkListChanged += SisyphiGameMultiplayer_OnPlayerNetworkListChanged;
        image.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(colorId);
        UpdateIsSelected();
    }

    private void SisyphiGameMultiplayer_OnPlayerNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdateIsSelected();
    }

    private void UpdateIsSelected()
    {
        PlayerData playerData = SisyphiGameMultiplayer.Instance.GetPlayerData();
        MaterialCategory materialCategory = SisyphiGameMultiplayer.Instance.GetCurrentMaterialCategory(playerData.clientId);
        Color color = Color.black;
        switch (materialCategory)
        {
            case MaterialCategory.Hair:
                color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.hairColorId);
                break;
            case MaterialCategory.Skin:
                color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.skinColorId);
                break;
            case MaterialCategory.Pant:
                color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.pantColorId);
                break;
            case MaterialCategory.Eyes:
                color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.eyesColorId);
                break;
        }
            
        if (
            (materialCategory == MaterialCategory.Hair && SisyphiGameMultiplayer.Instance.GetPlayerData().hairColorId == colorId) ||
            (materialCategory == MaterialCategory.Skin && SisyphiGameMultiplayer.Instance.GetPlayerData().skinColorId == colorId) ||
            (materialCategory == MaterialCategory.Pant && SisyphiGameMultiplayer.Instance.GetPlayerData().pantColorId == colorId) ||
            (materialCategory == MaterialCategory.Eyes && SisyphiGameMultiplayer.Instance.GetPlayerData().eyesColorId == colorId)
        )
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
        SisyphiGameMultiplayer.Instance.OnMaterialCategoryNetworkListChanged -= SisyphiGameMultiplayer_OnPlayerNetworkListChanged;
    }
}
