using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestingLobbyUI : MonoBehaviour
{
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button joinGameButton;
    // Start is called before the first frame update

    private void Awake()
    {
        createGameButton.onClick.AddListener(() => {
            SisyphiGameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterScene);
        });
        joinGameButton.onClick.AddListener(() => {
            SisyphiGameMultiplayer.Instance.StartClient();
        });
    }
    void Start()
    {
        Debug.Log("");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
