using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class EndGame : MonoBehaviour
{
    [SerializeField] private GameObject EndGameUI;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenu);
        });
        Hide();

        SisyphiGameManager.Instance.GameFinishedEvent += EndGame_OnGameFinished;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("Boulder"))
        {
            switch (collider.gameObject.name)
            {
                case "Boulder_0":
                    if (SisyphiGameManager.Instance.IsGameOver()) return;
                    Show("SISYPHUS 1 TAKES THE DUB");
                    break;
                case "Boulder_1":
                    if (SisyphiGameManager.Instance.IsGameOver()) return;
                    Show("SISYPHUS 2 TAKES THE DUB");
                    break;
            }
        }
    }

    private void EndGame_OnGameFinished(object sender, System.EventArgs e)
    {
        Show("TIME'S UP, NOBODY WINS");
    }

    private void Show(string message)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SisyphiGameManager.Instance.SetGameFinishedServerRpc();
        EndGameUI.SetActive(true);
        winText.text = message;
    }

    private void Hide()
    {
        EndGameUI.SetActive(false);
    }
}
