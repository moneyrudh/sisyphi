using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;
using Unity.Netcode;

public enum APIProvider {
    OpenAI,
    Anthropic,
    Groq
}

[System.Serializable]
public class TileGroup
{
    public string name;
    public List<GameObject> tiles;
}

[System.Serializable]
public class Logs
{
    public int wood;
    public GameObject log;
}

public class TileSpawnJob
{
    public enum SpawnType 
    {
        Normal,
        Checkpoint
    }

    public int[][] Tiles { get; set; }
    public float X { get; set; }
    public float Z { get; set; }
    public int ParentIndex { get; set; }
    public SpawnType Type { get; set; }

    public TileSpawnJob(int[][] tiles, float x, float z, int parentIndex, SpawnType type=SpawnType.Normal)
    {
        Tiles = tiles;
        X = x;
        Z = z;
        ParentIndex = parentIndex;
        Type = type;
    }
}

public class TileSetter : NetworkBehaviour
{
    public List<TileGroup> environmentTileGroups;

    [SerializeField] public List<Logs> logs;

    [SerializeField] private GameObject[] tileParents = new GameObject[4];
    [SerializeField] private GameObject networkPrefab;
    public GameObject WaterPrefab;
    public Material WaterMaterial;
    public GameObject IcePrefab;
    public GameObject EndTile;
    public TMP_InputField inputField;
    public float sizeMultiplier;
    private APIManager apiManager;
    private float curX;
    private float curY;
    private float curZ;
    private bool isIce = false;
    private bool isTilesSet = false;
    private float iceCooldown = 10f;
    private Dictionary<string, float> originalWaterValues = new Dictionary<string, float>();
    // private bool isLoading = true;
    [SerializeField] private GameObject initParent;
    public string anthropicResponse;
    public string openAIResponse;
    public string grocResponse;

    private bool isSetTilesParent = false;
    // Start is called before the first frame update

    private Queue<TileSpawnJob> spawnQueue = new Queue<TileSpawnJob>();
    private bool isProcessingQueue = false;


    public APIProvider apiProvider = APIProvider.Groq;

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

        initParent = new GameObject("InitParent");
        SetOriginalWaterValues();

        if (NetworkManager.Singleton.IsServer) SisyphiGameManager.Instance.OnStateChanged += TileSetter_PromptGeneration;
    }

    private async void TileSetter_PromptGeneration(object sender, System.EventArgs e)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (SisyphiGameManager.Instance.IsPromptGenerationState())
        {
            // float width = environmentTileGroups[0].tiles[0].GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
            // const float demultiplier = 0.91f;
            // int count = 0;
            // int checkPointOffset = 2;
            // float x = 0, z = 0;
        
            // SisyphiGameManager.Instance.PrintPrompts();
            // List<string> prompts = SisyphiGameManager.Instance.GetPrompts();
            // foreach (string s in prompts)
            // {
            //     count += 1;
            //     await CallServer(s, x, z, count);
            //     z += width * demultiplier * 10;
            //     z += checkPointOffset * width * demultiplier;
            // }
            // SisyphiGameManager.Instance.SetCountdownState();
            SisyphiGameManager.Instance.StartCinematicServerRpc();
            // SisyphiGameManager.Instance.CinematicCompleteServerRpc();
        }
    }

    void Update()
    {
        // if (isLoading) return;
        // if (IsHost && isTilesSet && !isIce) 
        // {
        //     StartCoroutine(SetIce());
        // }

        // if (Input.GetKeyDown(KeyCode.P)) {
        //     SpawnTileServerRpc(0, 0, 10, -1, 10, sizeMultiplier);
        // }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            GenerateMap();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnTileServerRpc(int value, int index, float x, float y, float z, float sizeMultiplier, int parentIndex)
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

        GameObject childPrefabReference = environmentTileGroups[value].tiles[index];
        GameObject spawnedObject = Instantiate(childPrefabReference, new Vector3(x, y, z), Quaternion.identity);
        NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
        spawnedObject.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, sizeMultiplier);
        spawnedObject.gameObject.name = environmentTileGroups[value].name + "_" + index;
        if (networkObject != null)
        {
            networkObject.Spawn();
            networkObject.transform.parent = GameObject.Find($"TilesParent_{parentIndex}").transform;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnEndTilesServerRpc(float x, float y, float z, int parentIndex)
    {
        GameObject gameObject = Instantiate(EndTile, new Vector3(x, y, z), Quaternion.identity);
        NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();
        networkObject.Spawn();
        networkObject.transform.parent = GameObject.Find($"TilesParent_{parentIndex}").transform;
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
                // StartCoroutine(SetTiles(tiles, initParent, 0, 0));
                StartCoroutine(SetNetworkTiles(tiles, 0, 0, -1));
            }
            catch (System.Exception e)
            {
                const string fallbackResponse = @"{""tiles"":[[0,0,0,5,5,5,5,0,0,0],[0,5,5,5,9,9,5,5,0,0],[0,5,9,9,9,9,9,5,5,0],[5,5,9,9,9,9,9,9,5,0],[5,9,9,9,9,9,9,9,5,0],[5,9,9,9,9,9,9,9,5,0],[5,5,9,9,9,9,9,9,5,0],[0,5,5,9,9,9,9,5,5,0],[0,0,5,5,9,9,5,5,0,0],[0,0,0,5,5,5,5,0,0,0]]}";
                int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(fallbackResponse).tiles;
                StartCoroutine(SetTiles(tiles, initParent, 0, 0));
                Debug.LogError("Error while parsing response: " + e);
            }
        }
        else
        {
            const string fallbackResponse = @"{""tiles"":[[0,0,0,5,5,5,5,0,0,0],[0,5,5,5,9,9,5,5,0,0],[0,5,9,9,9,9,9,5,5,0],[5,5,9,9,9,9,9,9,5,0],[5,9,9,9,9,9,9,9,5,0],[5,9,9,9,9,9,9,9,5,0],[5,5,9,9,9,9,9,9,5,0],[0,5,5,9,9,9,9,5,5,0],[0,0,5,5,9,9,5,5,0,0],[0,0,0,5,5,5,5,0,0,0]]}";
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(fallbackResponse).tiles;
            StartCoroutine(SetTiles(tiles, initParent, 0, 0));
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
            string response = @"{""tiles"":[[0,0,0,5,5,5,5,0,0,0],[0,5,5,5,9,9,5,5,0,0],[0,5,9,9,9,9,9,5,5,0],[5,5,9,9,9,9,9,9,5,0],[5,9,9,9,9,9,9,9,5,0],[5,9,9,9,9,9,9,9,5,0],[5,5,9,9,9,9,9,9,5,0],[0,5,5,9,9,9,9,5,5,0],[0,0,5,5,9,9,5,5,0,0],[0,0,0,5,5,5,5,0,0,0]]}";
            // string response = @"{""tiles"":[[2,2,2,2,2,2,2,2,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,0,0,0,0,2,0,0,0,2],[2,0,0,0,0,2,0,0,0,2],[2,0,0,0,0,2,0,0,0,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,2,2,2,2,2,2,2]]}";
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
            yield return null;
            // StartCoroutine(SetTiles(tiles, initParent, 0, 0));
            StartCoroutine(SetNetworkTiles(tiles, 0, 0, -1));
        }
        else
        {
            Task task = CallAnthropic(setting);
            yield return new WaitUntil(() => task.IsCompleted);
            Debug.Log(anthropicResponse);
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(anthropicResponse).tiles;
            // StartCoroutine(SetTiles(tiles, initParent, 0, 0));
            StartCoroutine(SetNetworkTiles(tiles, 0, 0, -1));
        }
    }

    private IEnumerator CallOpenAICoroutine(string setting)
    {
        if (setting == "" || setting == null) 
        {
            setting = "A circular pond surrounded by trees.";
            string response = @"{""tiles"":[[0,0,0,5,5,5,5,0,0,0],[0,5,5,5,9,9,5,5,0,0],[0,5,9,9,9,9,9,5,5,0],[5,5,9,9,9,9,9,9,5,0],[5,9,9,9,9,9,9,9,5,0],[5,9,9,9,9,9,9,9,5,0],[5,5,9,9,9,9,9,9,5,0],[0,5,5,9,9,9,9,5,5,0],[0,0,5,5,9,9,5,5,0,0],[0,0,0,5,5,5,5,0,0,0]]}";
            // string response = @"{""tiles"":[[2,2,2,2,2,2,2,2,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,0,0,0,0,2,0,0,0,2],[2,0,0,0,0,2,0,0,0,2],[2,0,0,0,0,2,0,0,0,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,0,0,2,0,0,2,2],[2,2,2,2,2,2,2,2,2,2]]}";
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
            yield return null;
            // StartCoroutine(SetTiles(tiles, initParent, 0, 0));
            StartCoroutine(SetNetworkTiles(tiles, 0, 0, -1));
        }
        else
        {
            Task task = CallOpenAI(setting);
            yield return new WaitUntil(() => task.IsCompleted);
            Debug.Log(openAIResponse);
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(openAIResponse).tiles;
            StartCoroutine(SetTiles(tiles, initParent, 0, 0));
        }
    }

    private IEnumerator CallGroqCoroutine(string setting)
    {
        apiManager.SendRequestToGroq(setting, tileParents.Length, HandleResponse);
        yield return null;
    }

    private IEnumerator CallModel(string setting)
    {
        apiManager.SendRequest(setting, HandleResponse);
        yield return null;
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

    private IEnumerator SetNetworkTiles(int[][] tiles, float x, float z, int parentIndex)
    {
        if (parentIndex < 0) yield break;

        Transform parent = tileParents[parentIndex].transform;
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
                    int count = environmentTileGroups[value].tiles.Count;
                    int index = UnityEngine.Random.Range(0, count-1);
                    SpawnTileServerRpc(value, index, curX, curY, curZ, sizeMultiplier, parentIndex);
                }
                curX += width * demultiplier;
            }
            curX = 0;
            curZ += width * demultiplier;
        }
        // GameObject waterGO = Instantiate(WaterPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        // waterGO.SetActive(true);
        // // waterGO.transform.parent = parent.transform;
        // waterGO.transform.GetChild(0).transform.localScale = new Vector3(sizeMultiplier * 2f, waterGO.transform.localScale.y, sizeMultiplier * 2f);
        // waterGO.transform.position = new Vector3((tiles.Length - 1) * demultiplier * (width / 2f), waterGO.transform.position.y, (tiles.Length - 1) * demultiplier * (width / 2f));
        isIce = false;
        isTilesSet = true;
        // isLoading = false;
    }

    public void RestartScene() {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void SetInitialGrid() 
    {
        Debug.Log("Setting initial grid");
        float width = environmentTileGroups[0].tiles[0].GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
        string response = @"{""tiles"":[[0,0,0,1,0,0,0,0,1,0],[0,1,0,0,0,0,1,0,0,0],[0,0,0,0,1,0,0,0,0,1],[1,0,0,0,0,0,0,1,0,0],[0,0,1,0,0,1,0,0,0,0],[0,0,0,5,5,5,5,0,1,0],[0,1,0,0,1,0,0,0,0,0],[1,0,0,0,0,0,1,0,0,1],[0,0,1,0,0,0,0,0,0,0],[0,0,0,0,1,0,0,1,0,0]]}";
        int[][] initTiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
        // StartCoroutine(SetTiles(initTiles, initParent, 0, 0 - width * 9));

        int index=0;
        foreach (GameObject tileParent in tileParents)
        {
            NetworkObject tilesParentNetworkObject = null;
            GameObject tilesParentGO = Instantiate(tileParent, new Vector3(0, 0, 0), Quaternion.identity);
            tilesParentNetworkObject = tilesParentGO.GetComponent<NetworkObject>();
            if (tilesParentNetworkObject != null)
            {
                tilesParentNetworkObject.Spawn();
                tilesParentNetworkObject.gameObject.name = $"TilesParent_{index}";
                index += 1;
            }
        }

        // StartCoroutine(SetNetworkTiles(initTiles, 0, 0 - width * 9, 0));
        // StartCoroutine(SetCheckpointTiles());
        EnqueueTileSpawn(initTiles, 0, -width*9, 0);
        float checkPointStartZ = width * 0.91f * 10;
        EnqueueTileSpawn(null, 0, checkPointStartZ, 0, TileSpawnJob.SpawnType.Checkpoint);
    }

    private IEnumerator SetCheckpointTiles()
    {
        Transform parent = tileParents[0].transform;
        float width = environmentTileGroups[0].tiles[0].GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
        const float demultiplier = 0.91f;
        int checkPointOffset = 2;
        float curX = 0, curY = 0, curZ = width * demultiplier * 10;

        for (int k = 0; k < 3; k++)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    yield return null;
                    int value = 0;
                    int count = environmentTileGroups[value].tiles.Count;
                    int index = UnityEngine.Random.Range(0, count-1);
                    SpawnTileServerRpc(value, index, curX, curY, curZ, sizeMultiplier, 0);
                    curX += width * demultiplier;
                }
                curX = 0;
                curZ += width * demultiplier;
            }
            curZ += width * demultiplier * 8;
            curZ += checkPointOffset * width * demultiplier;
        }
    }

    private void EnqueueTileSpawn(int[][] tiles, float x, float z, int parentIndex, TileSpawnJob.SpawnType type = TileSpawnJob.SpawnType.Normal)
    {
        var job = new TileSpawnJob(tiles, x, z, parentIndex, type);
        spawnQueue.Enqueue(job);

        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessSpawnQueue());
        }
    }

    private IEnumerator ProcessSpawnQueue()
    {
        isProcessingQueue = true;

        while (spawnQueue.Count > 0)
        {
            var job = spawnQueue.Peek();

            if (job.Type == TileSpawnJob.SpawnType.Normal)
            {
                yield return StartCoroutine(SpawnTileSet(job));
            }
            else 
            {
                yield return StartCoroutine(SpawnCheckpointSet(job));
            }

            spawnQueue.Dequeue();
            yield return new WaitForSeconds(0.1f);
        }

        isProcessingQueue = false;
    }

    private IEnumerator SpawnTileSet(TileSpawnJob job)
    {
        if (job.ParentIndex < 0) yield break;

        Transform parent = tileParents[job.ParentIndex].transform;
        curX = job.X;
        curZ = job.Z;
        float width = environmentTileGroups[0].tiles[0].GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
        const float demultiplier = 0.91f;

        for (int i = 0; i < job.Tiles.Length; i++)
        {
            for (int j = 0; j < job.Tiles[i].Length; j++)
            {
                yield return null;
                int value = job.Tiles[i][j];
                if (value != 9)
                {
                    int count = environmentTileGroups[value].tiles.Count;
                    int index = UnityEngine.Random.Range(0, count-1);
                    if (
                        job.ParentIndex == 0 &&
                        i >= 8 && 
                        (j <= 1 || j >= job.Tiles.Length - 2)
                    ) {
                    } else {
                        SpawnTileServerRpc(value, index, curX, curY, curZ, sizeMultiplier, job.ParentIndex);
                    }
                }
                curX += width * demultiplier;
            }
            curX = 0;
            curZ += width * demultiplier;
        }

        isIce = false;
        isTilesSet = true;
    }

    private IEnumerator SpawnCheckpointSet(TileSpawnJob job)
    {
        float width = environmentTileGroups[0].tiles[0].GetComponent<Renderer>().bounds.size.x * sizeMultiplier;
        const float demultiplier = 0.91f;
        int checkPointOffset = 2;
        curX = job.X;
        curZ = job.Z;

        for (int k = 0; k < 4; k++)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    yield return null;
                    int value = 0;
                    int count = environmentTileGroups[value].tiles.Count;
                    int index = UnityEngine.Random.Range(0, count-1);
                    if (
                        job.ParentIndex == 0 &&
                        k < 3 &&
                        (j <= 1 || j >= 8)
                    ) {
                    } else {
                        SpawnTileServerRpc(value, index, curX, curY, curZ, sizeMultiplier, job.ParentIndex);
                    }
                    // SpawnTileServerRpc(value, index, curX, curY, curZ, sizeMultiplier, job.ParentIndex);
                    curX += width * demultiplier;
                }
                curX = 0;
                curZ += width * demultiplier;
            }
            curZ += width * demultiplier * 8;
            curZ += checkPointOffset * width * demultiplier;
        }
        
        // curZ -= checkPointOffset * width * demultiplier;
        // curZ -= width * demultiplier * 8;
        // for (int i = 0; i < 10; i++)
        // {
        //     for (int j = 0; j < 10; j++)
        //     {
        //         yield return null;
        //         SpawnEndTilesServerRpc(curX, curY, curZ, job.ParentIndex);
        //         curX += width * demultiplier;
        //     }
        //     curX = 0;
        //     curZ += width * demultiplier;
        // }
    }

    public void GenerateMap() {
        if (!NetworkManager.Singleton.IsServer) return;
        curX = 0;
        curY = 0;
        curZ = 0;
        if (initParent != null) {
            foreach (Transform child in initParent.transform) {
                // child.NetworkObject.Despawn();
                Destroy(child.gameObject);
            }
        }
        Debug.Log("Generating map with text: " + inputField.text);

        // switch(apiProvider) {
        //     case APIProvider.OpenAI:
        //         StartCoroutine(CallOpenAICoroutine(inputField.text));
        //         break;
        //     case APIProvider.Anthropic:
        //         StartCoroutine(CallAnthropicCoroutine(inputField.text));
        //         break;
        //     case APIProvider.Groq:
        //         StartCoroutine(CallGroqCoroutine(inputField.text));
        //         break;
        //     default:
        //         apiManager.SendRequest(inputField.text, HandleResponse);
        //         break;
        // }

        string setting = string.IsNullOrEmpty(inputField.text)
            ? "A circular pond surrounded by trees."
            : inputField.text;

        _ = CallServer(setting, 0, 0, 1);
    }

    private async Task CallServer(string setting, float x, float z, int parentIndex)
    {
        try
        {
            string response = await GetComponent<APIManager>().SendServerRequest(setting, apiProvider);
            Debug.Log($"Response from server: {response}");

            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(response).tiles;
            // StartCoroutine(SetNetworkTiles(tiles, x, z, parentIndex));
            EnqueueTileSpawn(tiles, x, z, parentIndex);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while calling server: {e}");
            const string fallbackResponse = @"{""tiles"":[[0,0,8,8,8,8,8,8,0,0],[0,8,8,9,9,9,9,8,8,0],[8,8,9,9,9,9,9,9,8,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,9,9,9,9,9,9,9,9,8],[8,8,9,9,9,9,9,9,8,8],[0,8,8,9,9,9,9,8,8,0],[0,0,8,8,8,8,8,8,0,0]]}";
            int[][] tiles = JsonConvert.DeserializeObject<ResponseBody>(fallbackResponse).tiles;
            // StartCoroutine(SetNetworkTiles(tiles, x, z, parentIndex));
            EnqueueTileSpawn(tiles, x, z, parentIndex);
        }
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

        Transform waterGO = GameObject.Find("WaterGO").transform;
        if (waterGO == null)
        {
            Debug.LogError("Water not found");
            RestoreOriginalWaterValues();
            yield break;
        }
        GameObject water = waterGO.GetChild(0).gameObject;
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

        yield return new WaitForSeconds(iceCooldown);
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