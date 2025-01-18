using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CountdownUI : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text generatingPromptText;

    private void Start()
    {
        SisyphiGameManager.Instance.OnStateChanged += CountdownUI_UpdateState;

        countdownText.gameObject.SetActive(false);
        Hide();
        // generatingPromptText.gameObject.SetActive(false);
    }

    private void CountdownUI_UpdateState(object sender, System.EventArgs e)
    {
        if (SisyphiGameManager.Instance.IsPromptGenerationState())
        {
            Show();
            generatingPromptText.gameObject.SetActive(true);
        }
        if (SisyphiGameManager.Instance.IsCinematicState())
        {
            backgroundImage.gameObject.SetActive(false);
            generatingPromptText.gameObject.SetActive(false);
        }
        if (SisyphiGameManager.Instance.IsCountdown())
        {
            countdownText.gameObject.SetActive(true);
        }
        if (SisyphiGameManager.Instance.IsGamePlaying())
        {
            SoundManager.Instance.Stop("Prompt");
            SoundManager.Instance.Play("Sisyphi");
            countdownText.gameObject.SetActive(false);
            Hide();
        }
    }

    private void Update()
    {
        if (SisyphiGameManager.Instance.IsCountdown())
        {
            float seconds = SisyphiGameManager.Instance.GetCountdownTimer();
            seconds = Mathf.Max(seconds, 0);
            int rounded = (int) Mathf.Ceil(seconds);
            countdownText.text = rounded.ToString();
        }
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
