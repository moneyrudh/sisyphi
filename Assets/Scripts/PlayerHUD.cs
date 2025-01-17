using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    [SerializeField] private GameObject playerHUD;

    [Header("Toggle Colors")]
    [SerializeField] private Image boatButton;
    [SerializeField] private Image buildButton;

    [Header("HUD References")]
    [SerializeField] private Image BoatHUD;
    [SerializeField] private Image RampHUD;
    [SerializeField] private Image ConnectorHUD;
    [SerializeField] private Image PlatformHUD;

    [Header("Skill Reference")]
    [SerializeField] private Image shrinkSkill;
    [SerializeField] private Image growSkill;

    [Header("Wood")]
    [SerializeField] private TMP_Text woodText;

    [Header("Color References")]
    [SerializeField] private Color hudPreviewColor;
    [SerializeField] private Color buildableHudPreviewColor;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color buildableNormalColor;

    [Header("Gameplay Timer")]
    [SerializeField] private TMP_Text gameTimer;
    [SerializeField] private Color gameTimerLateColor;
    private bool gameTimerColorSet = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        SisyphiGameManager.Instance.OnStateChanged += PlayerHUD_OnStateChanged;
        HandleBoatToggle(false);
        HandleBoatHUD(false);
        HandleBuildToggle(false, 0);
        Hide();
    }

    private void Update()
    {
        float seconds = SisyphiGameManager.Instance.GetGameplayTimer();
        seconds = Mathf.Max(seconds, 0);
        float minutes = Mathf.Floor(seconds / 60f);
        int secondsRounded = (int) Mathf.Ceil(seconds);
        int minutesRounded = (int) minutes;
        gameTimer.text = $"{minutesRounded}:{secondsRounded}";
        if (!gameTimerColorSet && minutes == 0)
        {
            gameTimer.color = gameTimerLateColor;
            gameTimerColorSet = true;
        }        
    }

    private void PlayerHUD_OnStateChanged(object sender, System.EventArgs e)
    {
        if (SisyphiGameManager.Instance.IsGamePlaying())
        {
            Show();
        }
    }

    public void HandleBoatToggle(bool active)
    {
        if (active)
        {
            boatButton.color = normalColor;
        }
        else
        {
            boatButton.color = hudPreviewColor;
        }
        buildButton.color = hudPreviewColor;
        BoatHUD.gameObject.SetActive(active);
    }

    public void HandleBoatHUD(bool placed)
    {
        if (placed)
        {
            BoatHUD.color = hudPreviewColor;
        }
        else
        {
            BoatHUD.color = normalColor;
        }
    }

    public void HandleBuildToggle(bool active, int currentBuild)
    {
        if (active)
        {
            RampHUD.gameObject.SetActive(true);
            ConnectorHUD.gameObject.SetActive(true);
            PlatformHUD.gameObject.SetActive(true);
            HandleBuildableToggle(currentBuild);
            buildButton.color = buildableNormalColor;
        }
        else
        {
            RampHUD.gameObject.SetActive(false);
            ConnectorHUD.gameObject.SetActive(false);
            PlatformHUD.gameObject.SetActive(false);
            buildButton.color = buildableHudPreviewColor;
        }
        boatButton.color = hudPreviewColor;
    }

    public void HandleBuildableToggle(int currentBuild)
    {
        if (!RampHUD.gameObject.activeSelf ||
            !ConnectorHUD.gameObject.activeSelf ||
            !PlatformHUD.gameObject.activeSelf) return;

        switch (currentBuild)
        {
            case 0:
                RampHUD.color = buildableNormalColor;
                ConnectorHUD.color = buildableHudPreviewColor;
                PlatformHUD.color = buildableHudPreviewColor;
                break;
            case 1:
                RampHUD.color = buildableHudPreviewColor;
                ConnectorHUD.color = buildableNormalColor;
                PlatformHUD.color = buildableHudPreviewColor;
                break;
            case 2:
                RampHUD.color = buildableHudPreviewColor;
                ConnectorHUD.color = buildableHudPreviewColor;
                PlatformHUD.color = buildableNormalColor;
                break;
            default:
                break;
        }
    }

    public void HandleUsedSkill(bool used)
    {
        if (used)
        {
            shrinkSkill.color = hudPreviewColor;
            growSkill.color = hudPreviewColor;
        }
        else
        {
            shrinkSkill.color = normalColor;
            growSkill.color = normalColor;
        }
    }

    public void SetWood(int wood)
    {
        Debug.Log("Setting wood: " + wood);
        woodText.text = wood.ToString();
    }

    private void Show()
    {
        playerHUD.SetActive(true);
    }

    private void Hide()
    {
        playerHUD.SetActive(false);
    }
}
