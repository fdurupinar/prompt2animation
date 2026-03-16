using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Collections;
using System;

public class StoryNetClient : MonoBehaviour
{
    public string model = "gpt-3.5-turbo-instruct";
    public float temperature = 1.5F;
    public int max_tokens = 1500;
    public float top_p = 0.3F;
    public float frequency_penalty=0.5F;
    public float presence_penalty=0.3F;

    public delegate void ResponseCb(string request, string response);
    public delegate void CompareCb(string phrase1, string phrase2, float cosineSimilarity);

    private const string completionUrl = "https://api.openai.com/v1/completions";
    private const string embeddingUrl = "https://api.openai.com/v1/embeddings";
    private int requestId = 1;

    public static string SentenceTrim(string message, int num)
    {
        // Remove newlines and ignore all senetences after num
        message = message.Trim();
        message = message.Replace("\n", "");
        message = message.Replace("\t", "");

        int index = -1;
        index = message.IndexOf('"');

        // todo: check for ? and !
        message = message.Substring(index + 1);
        for (int i = 0; i < num; i++)
        {
            index = message.IndexOf('.');
            if (index > 0) index = message.IndexOf('.', index + 1);
        }
        // remove any trailing quotes
        message = message.Replace("\"", "");
        return message;
    }

    public void Prompt(string message, ResponseCb cb)
    {
        StartCoroutine(MakeCompletionRequest(message, cb));
    }

    public void PromptCompare(string phrase1, string phrase2, CompareCb cb)
    {
        StartCoroutine(MakeEmbeddingRequest(phrase1, phrase2, cb));
    }

    private void Start()
    {
        // for testing
        //string prompt = "Do you like cats?";
        //Prompt(prompt, TestResponseCb);

        //string prompt = "Do you like cats?";
        //PromptCompare(prompt, "Likes cats", TestCompareCb);
    }

    // for testing
    private void TestCompareCb(float response)
    {
        Debug.Log(response);
    }

    private void TestResponseCb(string response)
    {
        Debug.Log(response);
    }

    private string Key()
    {
        TextAsset obscure = Resources.Load("api-key") as TextAsset;
        return obscure.text.Trim();
    }

    private void Log(string message) 
    {
       
    }

    private IEnumerator MakeCompletionRequest(string prompt, ResponseCb cb)
    {
        StoryCompletionRequestData requestData = new StoryCompletionRequestData
        {
            prompt = prompt,
            model = model,
            temperature = temperature,
            max_tokens = max_tokens,
            top_p = top_p,
            frequency_penalty = frequency_penalty,
            presence_penalty = presence_penalty
        };

        int id = requestId++;
        string requestDataJson = JsonUtility.ToJson(requestData);
        Log("CompletionRequestParams,"+id+
            ",model:"+model+
            ",temperature:"+temperature+
            ",max_tokens:"+max_tokens+
            ",top_p:"+top_p+
            ",frequency_penalty:"+frequency_penalty+
            ",presence_penalty:"+presence_penalty);

        // Create the UnityWebRequest object
        using (UnityWebRequest request = new UnityWebRequest(completionUrl, "POST"))
        { 
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestDataJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.disposeCertificateHandlerOnDispose = true;

            // Set the authorization header with your API key
            request.SetRequestHeader("Authorization", "Bearer " + Key());
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Check for errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("API request error: " + request.error);
            }
            else
            {
                // Request successful, process the response
                string responseRaw = request.downloadHandler.text;
                //Debug.Log("API response: " + responseRaw);

                StoryCompletionResponse response = JsonUtility.FromJson<StoryCompletionResponse>(responseRaw);
                char[] delimeters = { '\t', '\r', '\n', ':', ' '};
                string message = response.choices[0].text.Trim(delimeters);
                Log("CompletionPrompt,"+id+","+prompt);
                Log("CompletionResponse,"+id+","+message);
                cb(prompt, message);
            }
        }
    }

    private IEnumerator MakeEmbeddingRequest(string phrase1, string phrase2, CompareCb cb)
    {
        List<string> prompts = new List<string>();
        prompts.Add(phrase1);
        prompts.Add(phrase2);

        StoryEmbeddingRequestData requestData = new StoryEmbeddingRequestData
        {
            input = prompts,
            model = "text-embedding-ada-002"
        };

        int id = requestId++;
        string requestDataJson = JsonUtility.ToJson(requestData);
        Log("EmbeddingRequestParams,"+id+","+model);

        // Create the UnityWebRequest object
        using (UnityWebRequest request = new UnityWebRequest(embeddingUrl, "POST"))
        { 
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestDataJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.disposeCertificateHandlerOnDispose = true;

            // Set the authorization header with your API key
            request.SetRequestHeader("Authorization", "Bearer " + Key());
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Check for errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("API request error: " + request.error);
            }
           
        }
    }
}

[System.Serializable]
public class StoryEmbeddingRequestData
{
    public List<string> input;
    public string model;
}

[System.Serializable]
public class StoryEmbeddingResponseData
{
    public string model;
    public List<StoryEmbeddingVector> data;
    public StoryEmbeddingUsage usage;
}

[System.Serializable]
public class StoryEmbeddingVector
{
    public string Object;
    public List<float> embedding;
    public int index;
}

[System.Serializable]
public class StoryEmbeddingUsage
{
    public int prompt_tokens;
    public int total_tokens;
}

[System.Serializable]
public class StoryCompletionRequestData
{
    public string prompt;
    public string model;
    public float temperature ;
    public int max_tokens;
    public float top_p;
    public float frequency_penalty;
    public float presence_penalty;
}

[System.Serializable]
public class StoryCompletionChoice
{
    public string text;
    public int index ;
    public float logprobs;
    public string finish_reason ;
}

[System.Serializable]
public class StoryCompletionUsage
{
    public int prompt_tokens ;
    public int completion_tokens ;
    public int total_tokens ;
}

[System.Serializable]
public class StoryCompletionResponse
{
    public string id;
    public string Object;
    public long created;
    public string model;
    public List<StoryCompletionChoice> choices;
    public StoryCompletionUsage usage ;
}

