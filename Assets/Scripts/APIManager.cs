using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class APIManager : MonoBehaviour
{
    private const string URL = "https://aigame.engineering.nyu.edu/generate";
    public delegate void RequestCallback(bool success, string response);
    
    public void SendRequest(string prompt, RequestCallback callback)
    {
        StartCoroutine(GetModelResponse(prompt, callback));
    }

    private IEnumerator GetModelResponse(string prompt, RequestCallback callback)
    {
        string requestBody = JsonUtility.ToJson(new RequestBody { prompt = prompt });
        byte[] requestBodyRaw = Encoding.UTF8.GetBytes(requestBody);

        using (UnityWebRequest webRequest = new UnityWebRequest(URL, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(requestBodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.certificateHandler = new BypassCertificate();

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error while requesting model: " + webRequest.error);
                callback(false, webRequest.error);
            }
            else
            {
                Debug.Log("Response: " + webRequest.downloadHandler.text);
                callback(true, webRequest.downloadHandler.text);
            }
        }
    }
}

[System.Serializable]
public class RequestBody
{
    public string prompt;
}

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}