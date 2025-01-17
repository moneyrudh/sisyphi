using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class EndGame : NetworkBehaviour
{
    [SerializeField] private GameObject EndGameUI;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private TMP_Text winTextBackground;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Image background;
    private NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);

    private void Start()
    {
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenu);
        });
        mainMenuButton.gameObject.SetActive(false);
        Hide();
        if (background != null) background.gameObject.SetActive(false);
        SisyphiGameManager.Instance.GameFinishedEvent += EndGame_OnGameFinished;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (isGameOver.Value) return;
        if (collider.gameObject.CompareTag("Boulder"))
        {
            switch (collider.gameObject.name)
            {
                case "Boulder_0":
                    {
                        if (SisyphiGameManager.Instance.IsGameOver()) return;
                        PlayerData playerData = SisyphiGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(0);
                        SetGameOverServerRpc();
                        ShowServerRpc($"{playerData.playerName.ToString()} TAKES THE DUB");
                    }
                    break;
                case "Boulder_1":
                    {
                        if (SisyphiGameManager.Instance.IsGameOver()) return;
                        PlayerData playerData = SisyphiGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(1);
                        SetGameOverServerRpc();
                        ShowServerRpc($"{playerData.playerName.ToString()} TAKES THE DUB");
                    }
                    break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetGameOverServerRpc()
    {
        isGameOver.Value = true;
    }

    private void EndGame_OnGameFinished(object sender, System.EventArgs e)
    {
        if (isGameOver.Value) return;
        SetGameOverServerRpc();
        mainMenuButton.gameObject.SetActive(true);
        Show("Time's up, nobody wins. Better luck next time.\nAlso, you missed out on the victory scene.", false);
    }

    [ServerRpc]
    private void ShowServerRpc(string message)
    {
        ShowClientRpc(message);
    }

    [ClientRpc]
    private void ShowClientRpc(string message)
    {
        Show(message, true);
        SoundManager.Instance.PlayOneShot("Bell");
    }

    private void Show(string message, bool changeScene)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SisyphiGameManager.Instance.SetGameFinishedServerRpc();
        EndGameUI.SetActive(true);
        winText.text = message;
        winTextBackground.text = message;

        if (changeScene) StartCoroutine(ChangeScene());
    }

    private IEnumerator ChangeScene()
    {
        yield return new WaitForSeconds(3f);

        background.gameObject.SetActive(true);
        float elapsedTime = 0;
        float fadeDuration = 5f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            background.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            CleanupNetworkObjectsServerRpc();
            SisyphiGameMultiplayer.Instance.LoadEndSceneServerRpc();
        }
        // Loader.Load(Loader.Scene.EndScene);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CleanupNetworkObjectsServerRpc()
    {
        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        foreach (NetworkObject netObj in networkObjects)
        {
            if (netObj.gameObject.GetComponent<NetworkManager>() != null ||
                netObj.gameObject.GetComponent<SisyphiGameMultiplayer>() != null ||
                netObj.gameObject.GetComponent<NetworkedSoundManager>() != null)
            {
                continue;
            }

            netObj.Despawn();
        }
    }

    private void Hide()
    {
        EndGameUI.SetActive(false);
    }
}
