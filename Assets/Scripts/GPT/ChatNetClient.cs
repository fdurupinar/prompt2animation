using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Collections;
using System;
using Newtonsoft.Json;



public class ChatNetClient: MonoBehaviour
{

   //OpenAI models
    string chatCompletionModel = "gpt-4";
    string completionModel = "gpt-3.5-turbo-instruct";
   
    public float temperature = 2.0F;
    public int max_tokens = 1500;//5000;//1500;//10000;
    public float top_p = 0.3F;
    public float frequency_penalty=0.5F;
    public float presence_penalty=0.3F;
    
    
    public string Context;

    public delegate void ResponseCb(string response);
    public delegate void CompareCb(float cosineSimilarity);

    private const string chatCompletionUrl = "https://api.openai.com/v1/chat/completions";
    private string completionUrl = "https://api.openai.com/v1/completions";

    private const string embeddingUrl = "https://api.openai.com/v1/embeddings";

    //private const string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-05-20:generateContent?key=";

    //private const string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=";
    private const string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";


    private float _timeOut = 30f;

    public void SetContext(string context) {
        Context = context;
    }
 
    public void Prompt(bool isGemini, string message, ResponseCb cb)
    {
        Debug.Log(message);
        
        StartCoroutine(MakeChatCompletionRequest(isGemini, message, cb));

    }
  

    private string Key()
    {
        //TextAsset obscure = Resources.Load("api-key") as TextAsset;

        TextAsset obscure = Resources.Load("gemini-api-key") as TextAsset;

        return obscure.text.Trim();
    }

 
    private IEnumerator MakeChatCompletionRequest(bool isGemini, string prompt, ResponseCb cb)
    {
        
        string apiKey = Key(); 
        string url = geminiUrl+ apiKey;

        if(!isGemini) {
            url = completionUrl;
           
        }

        List<ChatMessage> chatHistory = new List<ChatMessage>();

        chatHistory.Add(new ChatMessage { role = "system", content = Context });
        chatHistory.Add(new ChatMessage { role = "user", content = prompt });


        string requestDataJson;
        if(isGemini) {

            GeminiMessage requestData = new GeminiMessage(Context + prompt);
            
            requestDataJson = JsonConvert.SerializeObject(requestData);

            

        }
        else {
            string messages = "";
            foreach(ChatMessage cm in chatHistory) {
                messages += " " + cm.content;
            }
            CompletionRequestData requestData = new CompletionRequestData {
                prompt = messages,                                
                model = completionModel,
                temperature = temperature,
                max_tokens = max_tokens,
                top_p = top_p,
                frequency_penalty = frequency_penalty,
                presence_penalty = presence_penalty
                
            };
            requestDataJson = JsonUtility.ToJson(requestData);
        }

        

        Debug.Log(requestDataJson);
        
        // Create the UnityWebRequest object
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        { 
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestDataJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.disposeCertificateHandlerOnDispose = true;

            // Set the authorization header with your API key
            if(!isGemini)
                request.SetRequestHeader("Authorization", "Bearer " + Key());
            request.SetRequestHeader("Content-Type", "application/json");



            // Send the request
            //yield return request.SendWebRequest();
            request.timeout = (int)_timeOut;
            UnityWebRequestAsyncOperation async = request.SendWebRequest();
            
            float timer = 0;
            while(!async.isDone) {
                
                timer += Time.deltaTime;

                async.allowSceneActivation = false;

                if(timer >= _timeOut) {
                    Debug.Log("Timeout happened");
                    //Break out of the loop
                    yield break;
                }
                //Wait each frame in each loop OR Unity would freeze
                yield return null;
            }


            Debug.Log("yielded "+ request.result);
      
      
            // Check for errors
            if (request.result == UnityWebRequest.Result.ConnectionError) 
            {
                Debug.LogError("API request connection error: " + request.error);
                
            }
            else if(request.result == UnityWebRequest.Result.ProtocolError)
                {
                
                 Debug.LogError("API request protocol error: " + request.error);
                
            }
            else if(string.IsNullOrEmpty(request.error)) {
                
                // Request successful, process the response
                string responseRaw = request.downloadHandler.text;
                Debug.Log("API response: " + responseRaw);

                ChatCompletionResponse response = JsonUtility.FromJson<ChatCompletionResponse>(responseRaw);
                char[] delimiters = { '\t', '\r', '\n', ':', ' ', '`'};
                string message = "";
                if(isGemini) {
                    
                    GeminiResponse msg = JsonConvert.DeserializeObject<GeminiResponse>(responseRaw);

                    message = msg.candidates[0].content.parts[0].text.Trim(delimiters).Replace("json", "");
                        
                    Debug.Log("Gemini response object: " + message);

                }
                else {
                    CompletionResponse responseC = JsonUtility.FromJson<CompletionResponse>(responseRaw);
                    message = responseC.choices[0].text.Trim(delimiters);
                }

                
                cb(message);

            }
        }
    }

 
}
// Gemini data format
[System.Serializable]
public class Part {
    public string text;
    public Part(string msg) {
        text = msg;
    }
}

[System.Serializable]
public class Content {
    public Part [] parts;
    public Content(string msg) {
        parts = new Part[1];
        parts[0] = new Part(msg);
        parts[0].text = msg;
    }
}

[System.Serializable]
public class GeminiMessage {
    public Content[] contents;

    public GeminiMessage(string msg) {
        contents = new Content[1];
        contents[0] = new Content(msg);

    }
}


[System.Serializable]
public class Candidate {
    public Content content;
}

[System.Serializable]
public class GeminiResponse {
    public Candidate[] candidates;

    public GeminiResponse() {
        candidates = new Candidate[1];
        candidates[0] = new Candidate();

        candidates[0].content = new Content("");
        

    }
}
//OpenAI data format

[System.Serializable]
public class ChatMessage {
    public string role;
    public string content;
}

[System.Serializable]
public class CompletionRequestData
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
public class ChatCompletionRequestData {
    //public string prompt;    
    public List<ChatMessage> messages;
    public string model;
    public float temperature;
    public int max_tokens;

}


[System.Serializable]
public class CompletionChoice
{
    public string text;
    public int index ;
    public float logprobs;
    public string finish_reason ;
}


[System.Serializable]
public class ChatCompletionChoice {
    public ChatMessage message;
   
}

[System.Serializable]
public class CompletionUsage
{
    public int prompt_tokens ;
    public int completion_tokens ;
    public int total_tokens ;
}

[System.Serializable]
public class CompletionResponse
{
    public string id;
    public string Object;
    public long created;
    public string model;
    public List<CompletionChoice> choices;
    public CompletionUsage usage ;
}

[System.Serializable]
public class ChatCompletionResponse {
    public string id;
    public string Object;
    public long created;
    public string model;
    public List<ChatCompletionChoice> choices;
    public CompletionUsage usage;
}
