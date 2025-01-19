using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SecondPrompt : MonoBehaviour
{
    private int promptIndex;
    [SerializeField] private TMP_InputField promptInputField;
    [SerializeField] private Button doneButton;
    [SerializeField] private TMP_Text countdownTimerText;
    [SerializeField] private TMP_Text displayText;

    private bool setState = false;
    private string prompt = "A circular pond surrounded by grass and trees";

    private void Awake()
    {
        SisyphiGameManager.Instance.OnStateChanged += FirstPrompt_UpdateDisplay;
        doneButton.onClick.AddListener(() => {
            countdownTimerText.gameObject.SetActive(false);
            UpdateState();
        });

        displayText.gameObject.SetActive(false);
        Hide();
    }

    private void Start()
    {
        promptIndex = SisyphiGameManager.Instance.GetPlayerIndex() + 2;
    }

    private void FirstPrompt_UpdateDisplay(object sender, System.EventArgs e)
    {
        if (SisyphiGameManager.Instance.IsSecondPrompt())
        {
            Show();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (SisyphiGameManager.Instance.IsPromptGenerationState() || SisyphiGameManager.Instance.IsGamePlaying())
        {
            Hide();
        }
    }

    private void Update()
    {
        float seconds = SisyphiGameManager.Instance.GetCountdownTimer();
        seconds = Mathf.Max(seconds, 0);
        int rounded = (int) Mathf.Ceil(seconds);
        countdownTimerText.text = rounded.ToString();
        if (!setState && rounded <= 0)
        {
            setState = true;
            UpdateState();
        }
    }

    private void UpdateState()
    {
        prompt = promptInputField.text;
        promptInputField.gameObject.SetActive(false);
        doneButton.gameObject.SetActive(false);
        displayText.gameObject.SetActive(true);
        SisyphiGameManager.Instance.SetPromptServerRpc(promptIndex, prompt);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
