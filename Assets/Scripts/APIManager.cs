using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using Claudia;
using com.studios.taprobana;

public class APIManager : MonoBehaviour
{
    private const string URL = "https://aigame.engineering.nyu.edu/generate";
    public delegate void RequestCallback(bool success, string response);
    private ChatCompletionsApi chatCompletionsApi;
    private readonly string apiKey = "KEY";

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
                Debug.Log("Response from model: " + webRequest.downloadHandler.text);
                callback(true, webRequest.downloadHandler.text);
            }
        }
    }

    public async Task<string> SendRequestToOpenAI(string setting, int size)
    {
        string prompt = @"
            I want you to generate a 2D 10x10 matrix populated with values 0-" + size.ToString() + @". The values represent an element of an environment, like so:

            0: Grass
            1: Flowers
            2: Sand
            3: Rocks for a grassy terrain
            4: Rocks for a sandy terrain
            5: Trees for a grassy terrain
            6: Trees for a sandy terrain
            7: Logs for a grassy terrain
            8: Logs for a sandy terrain
            9: Water
            10: Bomb

            Consider that this 10x10 grid will be used in Unity3D for a game. Each element of this 2D matrix represents a tile that will be replaced in-game based on its number.
            Now, for the prompt """ + setting + @""", generate a 2D matrix with these values 0-" + size.ToString() + @". Regardless of what the prompt asks for, make sure the values of the matrix are between 0-" + size.ToString() + @" ONLY.
            If additional information is not specified about some remaining elements of the matrix, fill it by yourself by correlating it to the prompt. Your response should be only the 10x10 matrix and nothing else. 
            Give it in a JSON string format without indentation under the key ""tiles"" with the value being the 2D array. DO NOT response with any other text.
        ";

        try
        {
            chatCompletionsApi = new(apiKey);
            ChatCompletionsRequest chatCompletionsRequest = new ChatCompletionsRequest();
            chatCompletionsRequest.Model = "gpt-3.5-turbo";
            chatCompletionsRequest.MaxTokens = 600;
            com.studios.taprobana.Message message = new(com.studios.taprobana.Roles.USER, prompt);

            chatCompletionsRequest.AddMessage(message);

            ChatCompletionsResponse res = await chatCompletionsApi.CreateChatCompletionsRequest(chatCompletionsRequest);
            string response = res.GetResponseMessage();
            Debug.Log(response);
            GetComponent<TileSetter>().openAIResponse = response;
            return response;
        }
        catch (OpenAiRequestException exception)
        {
            Debug.LogError(exception);
            const string fallbackResponse = "{\"tiles\":[[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5]]}";
            GetComponent<TileSetter>().openAIResponse = fallbackResponse;
            return null;
        }
    }

    public async Task<string> SendRequestToAnthropic(string setting, int size)
    {
        string prompt = @"
            I want you to generate a 2D 10x10 matrix populated with values 0-" + size.ToString() + @". The values represent an element of an environment, like so:

            0: Grass
            1: Flowers
            2: Sand
            3: Rocks for a grassy terrain
            4: Rocks for a sandy terrain
            5: Trees for a grassy terrain
            6: Trees for a sandy terrain
            7: Logs for a grassy terrain
            8: Logs for a sandy terrain
            9: Water
            10: Bomb

            Consider that this 10x10 grid will be used in Unity3D for a game. Each element of this 2D matrix represents a tile that will be replaced in-game based on its number.
            Now, for the prompt """ + setting + @""", generate a 2D matrix with these values 0-" + size.ToString() + @". Regardless of what the prompt asks for, make sure the values of the matrix are between 0-" + size.ToString() + @" ONLY.
            If additional information is not specified about some remaining elements of the matrix, fill it by yourself by correlating it to the prompt. Your response should be only the 10x10 matrix and nothing else. 
            Give it in a JSON string format without indentation under the key ""tiles"" with the value being the 2D array. DO NOT response with any other text.
        ";
        Debug.Log(prompt);

        string promptWithPath = @"
            I want you to generate a 2D 10x10 matrix populated with values 0-12. The values represent an element of an environment, like so:

            0: Grass
            1: Flowers
            2: Bushes
            3: Barren grass patch
            4: Sand
            5: Rocks for a grassy terrain
            6: Rocks for a sandy terrain
            7: Tower
            8: Fence
            9: Tree
            10: Water
            11: House
            12: Bomb

            Consider that this 10x10 grid will be used in Unity3D for a game. Each element of this 2D matrix represents a tile that will be replaced in-game based on its number.
            Now, for the prompt """ + setting + "with a contiguous traversible path from row 0 to row 10 NOT obstructed by water, trees, towers, houses, or bombs along the path" + @""", generate a 2D matrix with these values 0-12. Regardless of what the prompt asks for, make sure the values of the matrix are between 0-12 ONLY.
            Keep in mind that the movement of the player will be along the direction row 0 to row 10, so while generating this matrix, try to make sure there is at least one traversable path from row 0 to row 10.
            A traversable path is a path that DOES NOT have any obstacles in it. Values 5, 6, 7, 8, 9, 10, 11, and 12 are considered obstacles.
            This means that you will ensure there exists at least one path with values 0, 1, 2, 3, and/or 4 (depending on the environment of the setting) all the way from row 0 to row 10, which has a path width of 1 or 2 tiles.
            If additional information is not specified about some remaining elements of the matrix, fill it by yourself by correlating it to the prompt. Your response should be only the 10x10 matrix and nothing else. 
            Give it in a JSON string format without indentation under the key ""tiles"" with the value being the 2D array. DO NOT response with any other text.
        ";
        
        try {
            var anthropic = new Anthropic()
            {
                ApiKey = "KEY"
            };

            Debug.Log("Sending request to Anthropic");

            var message = await anthropic.Messages.CreateAsync(new()
            {
                Model = Models.Claude3_5Sonnet,
                MaxTokens = 300,
                Messages = new Claudia.Message[] { new() { Role = "user", Content = prompt } }
            });

            // Debug.Log("Received response from Anthropic: " + message);
            string response = message.Content[0].Text;
            Debug.Log("Response from Anthropic: " + response);
            GetComponent<TileSetter>().anthropicResponse = response;
            return response;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error while sending request to Anthropic: " + e.Message);
            const string fallbackResponse = "{\"tiles\":[[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,4,4,3,2,5,5],[5,5,2,3,3,3,3,2,5,5],[5,5,2,2,2,2,2,2,5,5],[5,5,5,5,5,5,5,5,5,5],[5,5,5,5,5,5,5,5,5,5]]}";
            GetComponent<TileSetter>().anthropicResponse = fallbackResponse;
            return null;
        }
    }

    private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";
    private readonly string groqApiKey = "KEY"; // Replace with your actual Groq API key

    [System.Serializable]
    private class GroqRequestBody
    {
        public List<GroqMessage> messages;
        public string model;
        // public ResponseFormat response_format;
    }

    [System.Serializable]
    private class GroqMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    private class ResponseFormat
    {
        public string type;
    }

    [System.Serializable]
    private class GroqResponse
    {
        public List<GroqChoice> choices;
    }

    [System.Serializable]
    private class GroqChoice
    {
        public GroqMessage message;
    }

    public void SendRequestToGroq(string setting, int size, RequestCallback callback)
    {
        StartCoroutine(GetGroqResponse(setting, size, callback));
    }

    private IEnumerator GetGroqResponse(string setting, int size, RequestCallback callback)
    {
        string prompt = @"
            I want you to generate a 2D 10x10 matrix populated with values 0-" + size.ToString() + @". The values represent an element of an environment, like so:

            0: Grass
            1: Flowers
            2: Sand
            3: Rocks for a grassy terrain
            4: Rocks for a sandy terrain
            5: Trees for a grassy terrain
            6: Trees for a sandy terrain
            7: Logs for a grassy terrain
            8: Logs for a sandy terrain
            9: Water
            10: Bomb

            Consider that this 10x10 grid will be used in Unity3D for a game. Each element of this 2D matrix represents a tile that will be replaced in-game based on its number.
            Now, for the prompt """ + setting + @""", generate a 2D matrix with these values 0-" + size.ToString() + @". Regardless of what the prompt asks for, make sure the values of the matrix are between 0-" + size.ToString() + @" ONLY.
            If additional information is not specified about some remaining elements of the matrix, fill it by yourself by correlating it to the prompt. Your response should be only the 10x10 matrix and nothing else. 
            Give it in a JSON format without indentation under the key ""tiles"" with the value being the 2D array. DO NOT response with any other text.
        ";
        var requestBody = new GroqRequestBody
        {
            messages = new List<GroqMessage>
            {
                new GroqMessage { role = "user", content = prompt }
            },
            model = "llama-3.2-90b-vision-preview"
            // response_format = new ResponseFormat { type = "json_object" }
        };

        string jsonBody = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(GROQ_API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + groqApiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error while requesting Groq API: " + request.error);
                // return null;
            }
            else
            {
                string responseJson = request.downloadHandler.text;
                GroqResponse response = JsonUtility.FromJson<GroqResponse>(responseJson);
                if (response.choices != null && response.choices.Count > 0)
                {
                    string groqResponse = response.choices[0].message.content;
                    int startIndex = groqResponse.IndexOf("{");
                    int endIndex = groqResponse.LastIndexOf("}");
                    groqResponse = groqResponse.Substring(startIndex, endIndex - startIndex + 1);
                    Debug.Log("Response from Groq: " + groqResponse);
                    // GetComponent<TileSetter>().groqResponse = groqResponse;
                    callback(true, groqResponse);
                    // return groqResponse;
                }
                else
                {
                    Debug.LogError("Unexpected response format from Groq API");
                    callback(false, "Unexpected response format from Groq API");
                    // return null;
                }
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