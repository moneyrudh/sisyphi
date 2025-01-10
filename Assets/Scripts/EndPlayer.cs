using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EndPlayer : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] public PlayerVisual playerVisual;
    [SerializeField] private TMP_Text playerNameText;
    private int finalPlayerIndex;

    private Material _hairMaterial;
    private Material _skinMaterial;
    private Material _pantMaterial;
    private Material _eyesMaterial;

    void Start()
    {
        SetPlayerColor();
        finalPlayerIndex = SisyphiGameMultiplayer.Instance.GetWinnerIndexFromPlayerIndex(playerIndex);
    }

    public void SetPlayerColor()
    {
        _hairMaterial = new Material(playerVisual.hairMeshRenderers[0].material);
        _skinMaterial = new Material(playerVisual.skinMeshRenderers[0].material);
        _pantMaterial = new Material(playerVisual.pantMeshRenderer.material);
        _eyesMaterial = new Material(playerVisual.eyesMeshRenderer.material);

        foreach (SkinnedMeshRenderer renderer in playerVisual.hairMeshRenderers)
        {
            renderer.material = _hairMaterial;
        }
        foreach (SkinnedMeshRenderer renderer in playerVisual.skinMeshRenderers)
        {
            renderer.material = _skinMaterial;
        }
        playerVisual.pantMeshRenderer.material = _pantMaterial;
        playerVisual.eyesMeshRenderer.material = _eyesMaterial;

        PlayerData playerData = SisyphiGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(finalPlayerIndex);
        _hairMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.hairColorId);
        _skinMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.skinColorId);
        _pantMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.pantColorId);
        _eyesMaterial.color = SisyphiGameMultiplayer.Instance.GetPlayerColor(playerData.eyesColorId);
        playerNameText.text = playerData.playerName.ToString();

        Animator animator = GetComponentInChildren<Animator>();
        if (playerIndex != 0)
        {
            animator.SetTrigger("sad");
        }
        else
        {
            switch (EndGameScene.Instance.GetAnimationIndex())
            {
                case 0:
                    animator.SetTrigger("chicken");
                    break;
                case 1:
                    animator.SetTrigger("dance0");
                    break;
                case 2:
                    animator.SetTrigger("dance1");
                    break;
                default:
                    animator.SetTrigger("dance2");
                    break;
            }
        }
    }
}
