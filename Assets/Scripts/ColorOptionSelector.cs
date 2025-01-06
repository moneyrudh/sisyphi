using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorOptionSelector : MonoBehaviour
{
    [SerializeField] private GameObject PlayerMaterialCategoriesGO;
    [SerializeField] private GameObject PlayerMaterialOptionsGO;
    [SerializeField] private GameObject BoulderMaterialCategoriesGO;
    [SerializeField] private Button playerButton;
    [SerializeField] private Button boulderButton;

    private void Awake()
    {
        playerButton.onClick.AddListener(() => {
            SetPlayerOptions(true);
            SetBoulderOptions(false);
        });

        boulderButton.onClick.AddListener(() => {
            SetPlayerOptions(false);
            SetBoulderOptions(true);
        });

        SetPlayerOptions(true);
        SetBoulderOptions(false);
    }

    private void SetPlayerOptions(bool set)
    {
        playerButton.interactable = !set;
        PlayerMaterialCategoriesGO.SetActive(set);
        PlayerMaterialOptionsGO.SetActive(set);
    }

    private void SetBoulderOptions(bool set)
    {
        boulderButton.interactable = !set;
        BoulderMaterialCategoriesGO.SetActive(set);
    }
}
