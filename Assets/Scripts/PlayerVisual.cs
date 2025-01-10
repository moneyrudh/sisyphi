using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerVisual : MonoBehaviour
{
    [Header("Renderer References")]
    [SerializeField] public List<SkinnedMeshRenderer> hairMeshRenderers;
    [SerializeField] public List<SkinnedMeshRenderer> skinMeshRenderers;
    [SerializeField] public SkinnedMeshRenderer pantMeshRenderer;
    [SerializeField] public SkinnedMeshRenderer eyesMeshRenderer;

    [Header("Material References")]
    [SerializeField] public Material hairMaterial;
    [SerializeField] public Material skinMaterial;
    [SerializeField] public Material pantMaterial;
    [SerializeField] public Material eyesMaterial;

    [Header("Material Category Button References")]
    [SerializeField] private Button hairCategoryButton;
    [SerializeField] private Button skinCategoryButton;
    [SerializeField] private Button pantCategoryButton;
    [SerializeField] private Button eyesCategoryButton;

    // [Header("Selected Material Category")]
    private Material _hairMaterial;
    private Material _skinMaterial;
    private Material _pantMaterial;
    private Material _eyesMaterial;

    private void Awake()
    {
        _hairMaterial = new Material(hairMeshRenderers[0].material);
        _skinMaterial = new Material(skinMeshRenderers[0].material);
        _pantMaterial = new Material(pantMeshRenderer.material);
        _eyesMaterial = new Material(eyesMeshRenderer.material);

        if (SceneManager.GetActiveScene().name == Loader.Scene.CharacterScene.ToString())
        {
            hairCategoryButton.onClick.AddListener(() => {
                ClearButtonInteractions();
                SisyphiGameMultiplayer.Instance.ChangeCurrentMaterialCategory(MaterialCategory.Hair);
                hairCategoryButton.interactable = false;
            });
            skinCategoryButton.onClick.AddListener(() => {
                ClearButtonInteractions();
                SisyphiGameMultiplayer.Instance.ChangeCurrentMaterialCategory(MaterialCategory.Skin);
                skinCategoryButton.interactable = false;
            });
            pantCategoryButton.onClick.AddListener(() => {
                ClearButtonInteractions();
                SisyphiGameMultiplayer.Instance.ChangeCurrentMaterialCategory(MaterialCategory.Pant);
                pantCategoryButton.interactable = false;
            });
            eyesCategoryButton.onClick.AddListener(() => {
                ClearButtonInteractions();
                SisyphiGameMultiplayer.Instance.ChangeCurrentMaterialCategory(MaterialCategory.Eyes);
                eyesCategoryButton.interactable = false;
            });
            hairCategoryButton.interactable = false;
        }

        foreach (SkinnedMeshRenderer skinnedMeshRenderer in hairMeshRenderers)
        {
            skinnedMeshRenderer.material = _hairMaterial;
        }

        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinMeshRenderers)
        {
            skinnedMeshRenderer.material = _skinMaterial;
        }

        pantMeshRenderer.material = _pantMaterial;
        eyesMeshRenderer.material = _eyesMaterial;
    }

    public void UpdateMaterials(MaterialCategory category, PlayerData playerData)
    {
        switch (category)
        {
            case MaterialCategory.Hair:
                _hairMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.hairColorId);
                break;
            case MaterialCategory.Skin:
                _skinMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.skinColorId);
                break;
            case MaterialCategory.Pant:
                _pantMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.pantColorId);
                break;
            case MaterialCategory.Eyes:
                _eyesMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.eyesColorId);
                break;
        }
    }

    private void ClearButtonInteractions()
    {
        hairCategoryButton.interactable = true;
        skinCategoryButton.interactable = true;
        pantCategoryButton.interactable = true;
        eyesCategoryButton.interactable = true;
    }

    public void SetPlayerColor(MaterialCategory category, Color color)
    {
        switch (category)
        {
            case MaterialCategory.Hair:
                _hairMaterial.color = color;
                break;
            case MaterialCategory.Skin:
                _skinMaterial.color = color;
                break;
            case MaterialCategory.Pant:
                _pantMaterial.color = color;
                break;
            case MaterialCategory.Eyes:
                _eyesMaterial.color = color;
                break;
        }
    }   
}
