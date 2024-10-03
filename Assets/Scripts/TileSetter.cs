using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;

public class TileSetter : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject[] tileParents = new GameObject[13];
    public TMP_InputField inputField;
    public float sizeMultiplier;
    private APIManager apiManager;
    private float curX;
    private float curY;
    private float curZ;
    // private bool isLoading = true;
    private GameObject tilesParent;
    // private int[][] tiles;
    public string anthropicResponse;
    // Start is called before the first frame update

    void Start()
    {
        curX = 0;
        curY = 0;
        curZ = 0;
        apiManager = GetComponent<APIManager>();
        if (apiManager == null)
        {
            Debug.LogError("APIManager not found");
            return;
        }

        tilesParent = new GameObject("TilesParent");
        // apiManager.SendRequest("Ocean", HandleResponse);
        // StartCoroutine(CallAnthropicCoroutine(""));

        // float width = tilePrefab.GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
        // for (int i = 0; i < 10; i++) {
        //     for (int j = 0; j < 10; j++) {
        //         GameObject tile = Instantiate(tilePrefab, new Vector3(curX, curY, curZ), Quaternion.identity);
        //         tile.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, sizeMultiplier);
        //         curX += width * 0.9f;
        //     }
        //     curX = 0;
        //     curZ += width * 0.9f;
        // }
    }

    // Update is called once per frame
    void Update()
    {
        // if (isLoading) return;
    }

    void HandleResponse(bool success, string response)
    {
        if (success)
        {
            try
            {
                response = "{\"tiles\":[[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5]]}";
                int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
                // int[][] tiles = JsonConvert.DeserializeObject<int[][]>(response);
                StartCoroutine(SetTiles(tiles));
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error while parsing response: " + e);
            }
        }
        else
        {
            Debug.LogError("Error while requesting model: " + response);
        }
    }

    public async Task CallAnthropic(string setting)
    {
        if (setting == "" || setting == null) {
            setting = "A circular pond surrounded by trees.";
        }
        try
        {
            string response = await apiManager.SendRequestToAnthropic(setting);
            Debug.Log("Response from Anthropic: " + response);
        }
        catch (Exception e)
        {
            Debug.LogError("Error while calling Anthropic: " + e);
        }
    }

    private IEnumerator CallAnthropicCoroutine(string setting)
    {
        Task task = CallAnthropic(setting);
        yield return new WaitUntil(() => task.IsCompleted);
        Debug.Log(anthropicResponse);
        int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(anthropicResponse).tiles;
        StartCoroutine(SetTiles(tiles));
    }

    private IEnumerator SetTiles(int[][] tiles)
    {
        float width = tilePrefab.GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
        for (int i = 0; i < tiles.Length; i++)
        {
            for (int j = 0; j < tiles[i].Length; j++)
            {
                yield return null;
                int value = tiles[i][j];
                int count = tileParents[value].transform.childCount;
                int index = UnityEngine.Random.Range(0, count);
                GameObject tilePrefab = tileParents[value].transform.GetChild(index).gameObject;
                GameObject tile = Instantiate(tilePrefab, new Vector3(curX, curY, curZ), Quaternion.identity);
                tile.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, sizeMultiplier);
                tile.transform.parent = tilesParent.transform;
                curX += width * 0.91f;
            }
            curX = 0;
            curZ += width * 0.91f;
        }
        // isLoading = false;
    }

    public void RestartScene() {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void GenerateMap() {
        curX = 0;
        curY = 0;
        curZ = 0;
        if (inputField.text == "") {
            Debug.LogError("Input field is empty");
            return;
        }
        // Delete tilesParent's children
        if (tilesParent != null) {
            foreach (Transform child in tilesParent.transform) {
                Destroy(child.gameObject);
            }
        }
        Debug.Log("Generating map with text: " + inputField.text);
        // apiManager.SendRequest(inputField.text, HandleResponse);
        StartCoroutine(CallAnthropicCoroutine(inputField.text));
    }
}

[System.Serializable]
public class ResponseBody
{
    public int[][] tiles;
}