using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CustomButtonBehaviour : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;
    private Image buttonImage;
    private TextMeshProUGUI buttonText;
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;
        buttonImage.sprite = pressedSprite;
        buttonText.alignment = TextAlignmentOptions.Bottom;
        SoundManager.Instance.PlayOneShot("Hover");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable) return;
        buttonImage.sprite = normalSprite;
        buttonText.alignment = TextAlignmentOptions.Center;
        SoundManager.Instance.PlayOneShot("TurnOffSystem");
    }
}
