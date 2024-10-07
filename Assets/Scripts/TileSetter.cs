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
    public GameObject WaterPrefab;
    public Material WaterMaterial;
    public GameObject IcePrefab;
    public TMP_InputField inputField;
    public float sizeMultiplier;
    private APIManager apiManager;
    private float curX;
    private float curY;
    private float curZ;
    private bool isIce = false;
    private bool isTilesSet = false;
    private Dictionary<string, float> originalWaterValues = new Dictionary<string, float>();
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
        SetOriginalWaterValues();
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
        if (isTilesSet && !isIce && Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(SetIce());
        }
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
        if (setting == "" || setting == null) 
        {
            setting = "A circular pond surrounded by trees.";
            string response = @"{""tiles"":[[0,0,8,8,8,8,8,8,0,0],[0,8,8,9,9,9,9,8,8,0],[8,8,9,9,9,9,9,9,8,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,8,9,9,9,9,9,9,8,8],[0,8,8,9,9,9,9,8,8,0],[0,0,8,8,8,8,8,8,0,0]]}";
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
            yield return null;
            StartCoroutine(SetTiles(tiles));
        }
        else
        {
            Task task = CallAnthropic(setting);
            yield return new WaitUntil(() => task.IsCompleted);
            Debug.Log(anthropicResponse);
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(anthropicResponse).tiles;
            StartCoroutine(SetTiles(tiles));
        }
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
                if (value != 9)
                {
                    int count = tileParents[value].transform.childCount;
                    int index = UnityEngine.Random.Range(0, count);
                    GameObject tilePrefab = tileParents[value].transform.GetChild(index).gameObject;
                    GameObject tile = Instantiate(tilePrefab, new Vector3(curX, curY, curZ), Quaternion.identity);
                    tile.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, sizeMultiplier);
                    tile.transform.parent = tilesParent.transform;
                }
                curX += width * 0.91f;
            }
            curX = 0;
            curZ += width * 0.91f;
        }
        GameObject waterGO = Instantiate(WaterPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        waterGO.SetActive(true);
        waterGO.transform.parent = tilesParent.transform;
        isIce = false;
        isTilesSet = true;
        // isLoading = false;
    }

    public void RestartScene() {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void GenerateMap() {
        curX = 0;
        curY = 0;
        curZ = 0;
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

    private IEnumerator SetIce()
    {
        isIce = true;
        float transitionDuration = 4.0f; // Duration of the transition in seconds
        float iceDuration = 5.0f; // Duration of the ice state

        // Store original values
        float originalSmoothness = WaterMaterial.GetFloat("_WaterSmoothness");
        float originalDistortion = WaterMaterial.GetFloat("_Distortion");
        float originalWavesAmplitude = WaterMaterial.GetFloat("_WavesAmplitude");
        float originalWavesAmount = WaterMaterial.GetFloat("_WavesAmount");

        // Define ice state values
        float iceSmoothness = 3.25f;
        float iceDistortion = 0.0f;
        float iceWavesAmplitude = 0.0f;
        float iceWavesAmount = 0.0f;

        // Transition to ice
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            WaterMaterial.SetFloat("_WaterSmoothness", Mathf.Lerp(originalSmoothness, iceSmoothness, t));
            WaterMaterial.SetFloat("_Distortion", Mathf.Lerp(originalDistortion, iceDistortion, t));
            WaterMaterial.SetFloat("_WavesAmplitude", Mathf.Lerp(originalWavesAmplitude, iceWavesAmplitude, t));
            WaterMaterial.SetFloat("_WavesAmount", Mathf.Lerp(originalWavesAmount, iceWavesAmount, t));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Confirm ice state values
        WaterMaterial.SetFloat("_WaterSmoothness", iceSmoothness);
        WaterMaterial.SetFloat("_Distortion", iceDistortion);
        WaterMaterial.SetFloat("_WavesAmplitude", iceWavesAmplitude);
        WaterMaterial.SetFloat("_WavesAmount", iceWavesAmount);

        GameObject water = tilesParent.transform.Find("WaterGO(Clone)").GetChild(0).gameObject;
        if (water == null)
        {
            Debug.LogError("Water not found");
            RestoreOriginalWaterValues();
            yield break;
        }
        water.GetComponent<Collider>().enabled = true;

        // Wait for the ice duration
        yield return new WaitForSeconds(iceDuration);

        // Transition back to water
        elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            WaterMaterial.SetFloat("_WaterSmoothness", Mathf.Lerp(iceSmoothness, originalSmoothness, t));
            WaterMaterial.SetFloat("_Distortion", Mathf.Lerp(iceDistortion, originalDistortion, t));
            WaterMaterial.SetFloat("_WavesAmplitude", Mathf.Lerp(iceWavesAmplitude, originalWavesAmplitude, t));
            WaterMaterial.SetFloat("_WavesAmount", Mathf.Lerp(iceWavesAmount, originalWavesAmount, t));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        water.GetComponent<Collider>().enabled = false;

        // Confirm original values
        WaterMaterial.SetFloat("_WaterSmoothness", originalSmoothness);
        WaterMaterial.SetFloat("_Distortion", originalDistortion);
        WaterMaterial.SetFloat("_WavesAmplitude", originalWavesAmplitude);
        WaterMaterial.SetFloat("_WavesAmount", originalWavesAmount);

        isIce = false;
    }

    private void SetOriginalWaterValues()
    {
        string[] properties = { "_WaterSmoothness", "_Distortion", "_WavesAmplitude", "_WavesAmount" };
        foreach (string prop in properties)
        {
            originalWaterValues[prop] = WaterMaterial.GetFloat(prop);
        }
    }

    private void RestoreOriginalWaterValues()
    {
        foreach (KeyValuePair<string, float> entry in originalWaterValues)
        {
            WaterMaterial.SetFloat(entry.Key, entry.Value);
        }
    }

    private void OnDisable()
    {
        RestoreOriginalWaterValues();
    }

    private void OnApplicationQuit()
    {
        RestoreOriginalWaterValues();
    }

    private void OnDestroy()
    {
        RestoreOriginalWaterValues();
    }
}

[System.Serializable]
public class ResponseBody
{
    public int[][] tiles;
}