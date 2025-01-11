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

    private void Start()
    {
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        buttonImage.sprite = pressedSprite;
        buttonText.alignment = TextAlignmentOptions.Bottom;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        buttonImage.sprite = normalSprite;
        buttonText.alignment = TextAlignmentOptions.Center;
    }
}
