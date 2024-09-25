using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSetter : MonoBehaviour
{
    // public GameObject tilePrefab;
    private APIManager apiManager;
    private float curX; 
    private float curY;
    private float curZ;
    // Start is called before the first frame update

    void Start()
    {
        curX = 0;
        curY = 0;
        curZ = 0;
        apiManager = GetComponent<APIManager>();
        if (apiManager == null) {
            Debug.LogError("APIManager not found");
            return;
        }

        apiManager.SendRequest("A desert Oasis", HandleResponse);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void HandleResponse(bool success, string response)
    {
        if (success) {
            Debug.Log("Response from API Manager: " + response);
            response = "{\"tiles\":" + response + "}";
            ResponseBody data = JsonUtility.FromJson<ResponseBody>(response);
            Debug.Log(data.tiles.Length);
        }
        else
        {
            Debug.LogError("Error while requesting model: " + response);
        }
    }
}

[System.Serializable]
public class ResponseBody
{
    public int[] tiles;
}