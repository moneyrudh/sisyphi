using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EndSceneUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Button mainMenuButton;
    void Awake()
    {
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenu);
            SoundManager.Instance.StopAll();
            SoundManager.Instance.Play("Theme");
        });

        mainMenuButton.gameObject.SetActive(false);
        StartCoroutine(PlayEndSequence());
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private IEnumerator PlayEndSequence()
    {
        float elapsedTime = 0;
        float fadeDuration = 2f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            background.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        yield return new WaitForSeconds(7f);

        mainMenuButton.gameObject.SetActive(true);
    }
}
