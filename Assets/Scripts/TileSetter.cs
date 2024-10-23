using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;
using Unity.Netcode;

public class TileSetter : NetworkBehaviour
{
    [System.Serializable]
    public class TileGroup
    {
        public string name;
        public List<GameObject> tiles;
    }
    public List<TileGroup> environmentTileGroups;

    public GameObject[] tileParents = new GameObject[13];
    [SerializeField] private GameObject networkPrefab;
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
    [SerializeField] private GameObject tilesParent;
    private GameObject initParent;
    // private int[][] tiles;
    public string anthropicResponse;
    public string openAIResponse;
    public string grocResponse;

    private bool isSetTilesParent = false;
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

        // tilesParent = new GameObject("TilesParent");
        initParent = new GameObject("InitParent");
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
        // Init ground with {"tiles":[[0,0,0,1,0,0,0,0,1,0],[0,1,0,0,0,0,1,0,0,0],[0,0,0,0,1,0,0,0,0,1],[1,0,0,0,0,0,0,1,0,0],[0,0,1,0,0,1,0,0,0,0],[0,0,0,0,0,0,0,0,1,0],[0,1,0,0,1,0,0,0,0,0],[1,0,0,0,0,0,1,0,0,1],[0,0,1,0,0,0,0,0,0,0],[0,0,0,0,1,0,0,1,0,0]]}
        float width = environmentTileGroups[0].tiles[0].GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
        string response = @"{""tiles"":[[0,0,0,1,0,0,0,0,1,0],[0,1,0,0,0,0,1,0,0,0],[0,0,0,0,1,0,0,0,0,1],[1,0,0,0,0,0,0,1,0,0],[0,0,1,0,0,1,0,0,0,0],[0,0,0,0,0,0,0,0,1,0],[0,1,0,0,1,0,0,0,0,0],[1,0,0,0,0,0,1,0,0,1],[0,0,1,0,0,0,0,0,0,0],[0,0,0,0,1,0,0,1,0,0]]}";
        int[][] initTiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
        // StartCoroutine(SetTiles(initTiles, initParent, 0, 0 - width * 9));
    }

    // Update is called once per frame
    void Update()
    {
        // if (isLoading) return;
        if (isTilesSet && !isIce && Input.GetKeyDown(KeyCode.Space))
        {
            // StartCoroutine(SetIce());
        }

        if (Input.GetKeyDown(KeyCode.P)) {
            SpawnNetworkObjectServerRpc(5, 0, 10, -1, 10, sizeMultiplier);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnNetworkObjectServerRpc(int value, int index, float x, float y, float z, float sizeMultiplier)
    {
        if (value < 0 || value >= environmentTileGroups.Count)
        {
            Debug.LogError("Invalid value: " + value);
            return;
        }
        if (index < 0 || index >= environmentTileGroups[value].tiles.Count)
        {
            Debug.LogError("Invalid index: " + index + " for value: " + value);
            return;
        }

        NetworkObject tilesParentNetworkObject = null;
        if (!isSetTilesParent) {
            isSetTilesParent = true;
            GameObject tilesParentGO = Instantiate(tilesParent, new Vector3(0, 0, 0), Quaternion.identity);
            tilesParentNetworkObject = tilesParentGO.GetComponent<NetworkObject>();
            if (tilesParentNetworkObject != null)
            {
                tilesParentNetworkObject.Spawn();
                tilesParentNetworkObject.gameObject.name = "TilesParent";
            }
        }

        GameObject childPrefabReference = environmentTileGroups[value].tiles[index];
        GameObject spawnedObject = Instantiate(childPrefabReference, new Vector3(x, y, z), Quaternion.identity);
        NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
        spawnedObject.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, sizeMultiplier);
        spawnedObject.gameObject.name = environmentTileGroups[value].name + "_" + index;
        if (networkObject != null)
        {
            networkObject.Spawn();
            networkObject.transform.parent = GameObject.Find("TilesParent").transform;
        }
        // if (networkPrefab == null)
        // {
        //     Debug.LogError("Network prefab reference is null!");
        //     return;
        // }

        // GameObject spawnedObject = Instantiate(networkPrefab, new Vector3(x, y, z), Quaternion.identity);
        // NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
        // if (networkObject != null)
        // {
        //     networkObject.Spawn();
        // }
    }

    void HandleResponse(bool success, string response)
    {
        // if (!IsServer) return;
        if (success)
        {
            try
            {
                // response = "{\"tiles\":[[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5]]}";
                int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
                // int[][] tiles = JsonConvert.DeserializeObject<int[][]>(response);
                // StartCoroutine(SetTiles(tiles, tilesParent, 0, 0));
                // SetTilesServerRpc(tiles, 0, 0);
                StartCoroutine(SetNetworkTiles(tiles, 0, 0));
            }
            catch (System.Exception e)
            {
                const string fallbackResponse = "{\"tiles\":[[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5]]}";
                int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(fallbackResponse).tiles;
                StartCoroutine(SetTiles(tiles, tilesParent, 0, 0));
                Debug.LogError("Error while parsing response: " + e);
            }
        }
        else
        {
            const string fallbackResponse = "{\"tiles\":[[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5]]}";
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(fallbackResponse).tiles;
            StartCoroutine(SetTiles(tiles, tilesParent, 0, 0));
            Debug.LogError("Error while requesting model: " + response);
        }
    }

    public async Task CallAnthropic(string setting)
    {
        try
        {
            string response = await apiManager.SendRequestToAnthropic(setting, tileParents.Length);
            Debug.Log("Response from Anthropic: " + response);
        }
        catch (Exception e)
        {
            Debug.LogError("Error while calling Anthropic: " + e);
        }
    }

    public async Task CallOpenAI(string setting)
    {
        try
        {
            string response = await apiManager.SendRequestToOpenAI(setting, tileParents.Length);
            Debug.Log("Response from OpenAI: " + response);
        }
        catch (Exception e)
        {
            Debug.LogError("Error while calling OpenAI: " + e);
        }
    }

    private IEnumerator CallAnthropicCoroutine(string setting)
    {
        if (setting == "" || setting == null) 
        {
            setting = "A circular pond surrounded by trees.";
            string response = @"{""tiles"":[[0,0,8,8,8,8,8,8,0,0],[0,8,8,9,9,9,9,8,8,0],[8,8,9,9,9,9,9,9,8,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,8,9,9,9,9,9,9,8,8],[0,8,8,9,9,9,9,8,8,0],[0,0,8,8,8,8,8,8,0,0]]}";
            // string response = @"{""tiles"":[[2,2,2,2,2,2,2,2,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,0,0,0,0,2,0,0,0,2],[2,0,0,0,0,2,0,0,0,2],[2,0,0,0,0,2,0,0,0,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,2,2,2,2,2,2,2]]}";
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
            yield return null;
            StartCoroutine(SetTiles(tiles, tilesParent, 0, 0));
        }
        else
        {
            Task task = CallAnthropic(setting);
            yield return new WaitUntil(() => task.IsCompleted);
            Debug.Log(anthropicResponse);
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(anthropicResponse).tiles;
            StartCoroutine(SetTiles(tiles, tilesParent, 0, 0));
        }
    }

    private IEnumerator CallOpenAICoroutine(string setting)
    {
        if (setting == "" || setting == null) 
        {
            setting = "A circular pond surrounded by trees.";
            string response = @"{""tiles"":[[0,0,8,8,8,8,8,8,0,0],[0,8,8,9,9,9,9,8,8,0],[8,8,9,9,9,9,9,9,8,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,8,9,9,9,9,9,9,8,8],[0,8,8,9,9,9,9,8,8,0],[0,0,8,8,8,8,8,8,0,0]]}";
            // string response = @"{""tiles"":[[2,2,2,2,2,2,2,2,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,0,0,0,0,2,0,0,0,2],[2,0,0,0,0,2,0,0,0,2],[2,0,0,0,0,2,0,0,0,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,2,2,2,2,2,2,2]]}";
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
            yield return null;
            StartCoroutine(SetTiles(tiles, tilesParent, 0, 0));
        }
        else
        {
            Task task = CallOpenAI(setting);
            yield return new WaitUntil(() => task.IsCompleted);
            Debug.Log(openAIResponse);
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(openAIResponse).tiles;
            StartCoroutine(SetTiles(tiles, tilesParent, 0, 0));
        }
    }

    private IEnumerator SetTiles(int[][] tiles, GameObject parent, float x, float z)
    {
        curX = x;
        curZ = z;
        float width = environmentTileGroups[0].tiles[0].GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
        const float demultiplier = 0.91f;
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
                    tile.transform.parent = parent.transform;
                }
                curX += width * demultiplier;
            }
            curX = 0;
            curZ += width * demultiplier;
        }
        GameObject waterGO = Instantiate(WaterPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        waterGO.SetActive(true);
        waterGO.transform.parent = parent.transform;
        waterGO.transform.GetChild(0).transform.localScale = new Vector3(sizeMultiplier * 2f, waterGO.transform.localScale.y, sizeMultiplier * 2f);
        waterGO.transform.position = new Vector3((tiles.Length - 1) * demultiplier * (width / 2f), waterGO.transform.position.y, (tiles.Length - 1) * demultiplier * (width / 2f));
        isIce = false;
        isTilesSet = true;
        // isLoading = false;
    }

    private IEnumerator SetNetworkTiles(int[][] tiles, float x, float z)
    {
        Transform parent = tilesParent.transform;
        curX = x;
        curZ = z;
        float width = environmentTileGroups[0].tiles[0].GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
        const float demultiplier = 0.91f;
        for (int i = 0; i < tiles.Length; i++)
        {
            for (int j = 0; j < tiles[i].Length; j++)
            {
                yield return null;
                int value = tiles[i][j];
                if (value != 9)
                {
                    // int count = tileParents[value].transform.childCount;
                    int count = environmentTileGroups[value].tiles.Count;
                    int index = UnityEngine.Random.Range(0, count-1);
                    // Debug.Log("" "Index: " + index);
                    // SetGOServerRpc(value, index, curX, curY, curZ);
                    SpawnNetworkObjectServerRpc(value, index, curX, curY, curZ, sizeMultiplier);
                    // GameObject tilePrefab = tileParents[value].transform.GetChild(index).gameObject;
                    // GameObject tile = Instantiate(tilePrefab, new Vector3(curX, curY, curZ), Quaternion.identity);
                    // tile.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, sizeMultiplier);
                    // tile.transform.parent = parent.transform;
                }
                curX += width * demultiplier;
            }
            curX = 0;
            curZ += width * demultiplier;
        }
        GameObject waterGO = Instantiate(WaterPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        waterGO.SetActive(true);
        // waterGO.transform.parent = parent.transform;
        waterGO.transform.GetChild(0).transform.localScale = new Vector3(sizeMultiplier * 2f, waterGO.transform.localScale.y, sizeMultiplier * 2f);
        waterGO.transform.position = new Vector3((tiles.Length - 1) * demultiplier * (width / 2f), waterGO.transform.position.y, (tiles.Length - 1) * demultiplier * (width / 2f));
        isIce = false;
        isTilesSet = true;
        // isLoading = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetGOServerRpc(int value, int index, float x, float y, float z)
    {
        SetGOClientRpc(value, index, x, y, z);
    }

    [ClientRpc]
    public void SetGOClientRpc(int value, int index, float x, float y, float z)
    {
        GameObject parent = tilesParent;
        GameObject tilePrefab = tileParents[value].transform.GetChild(index).gameObject;
        GameObject tile = Instantiate(tilePrefab, new Vector3(x, y, z), Quaternion.identity);
        tile.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, sizeMultiplier);
        tile.transform.parent = parent.transform;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTilesServerRpc(int[][] tiles, float x, float z)
    {
        // SetTilesClientRpc(tiles, x, z);
        StartCoroutine(SetNetworkTiles(tiles, x, z));
    }

    [ClientRpc]
    public void SetTilesClientRpc(int[][] tiles, float x, float z)
    {
        StartCoroutine(SetNetworkTiles(tiles, x, z));
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
        apiManager.SendRequestToGroq(inputField.text, tileParents.Length, HandleResponse);
        // StartCoroutine(CallGrocCoroutine(inputField.text));
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

        Transform waterGO = tilesParent.transform.Find("WaterGO(Clone)");
        if (waterGO == null)
        {
            Debug.LogError("Water not found");
            RestoreOriginalWaterValues();
            yield break;
        }
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