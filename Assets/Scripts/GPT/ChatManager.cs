using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro; // Use TextMeshPro for better text rendering
using Newtonsoft.Json; // EXTENDED: We must use Newtonsoft for advanced JSON
using System.Linq;
using System.IO;
using System;


/// <summary>
/// Manages the chat UI, user input, and communication with the Gemini API,
/// including handling tool calls for simulation control.
/// </summary>
public class ChatManager : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("Enter your Gemini API key here. DO NOT expose this in production builds.")]
    
    private string apiKey = "[YOUR_API_KEY_HERE]";
    // EXTENDED: Changed constant to be just the base URL
    private const string ApiUrlBase =  "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-pro-preview:generateContent?key=" ; // "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key=" ;

    [Header("UI References")]
    [SerializeField]
    private TMP_InputField userInputField;
    [SerializeField]
    private Button sendButton;
    [SerializeField]
    private Transform chatContentPanel;
    [SerializeField]
    private GameObject userMessagePrefab;
    [SerializeField]
    public GameObject llmMessagePrefab;
    [SerializeField]
    private ScrollRect chatScrollRect;

    [SerializeField]
    TextAsset systemPrompt;
    
    [SerializeField]
    TextAsset visemePrompt;
        


    
    private string _savePath;
    private string _chatResponsePath;
    private GameObject _progressObject;
    
    
    public class ChatHistoryWrapper
    {
        public string timestamp;
        public List<Content> messages;
}


    // List to maintain the conversation history for the API
    private List<Content> chatHistory;
    private Content systemInstruction;
    
    
    
    private string Key()
    {
      
        TextAsset keyAsset = Resources.Load<TextAsset>("gemini-api-key");
    
        apiKey = keyAsset.text.Trim();
        return apiKey;

    }

    void Start()
    {
        apiKey = Key();

        sendButton.onClick.AddListener(OnSendButtonClick);
        userInputField.onSubmit.AddListener((_) => OnSendButtonClick());
        chatHistory = new List<Content>();

        _savePath = Path.Combine(Application.dataPath, "Resources", "ChatHistory");
        if (!Directory.Exists(_savePath)) Directory.CreateDirectory(_savePath);

        _chatResponsePath = Path.Combine(Application.dataPath, "Resources", "ChatResponse");
        if (!Directory.Exists(_chatResponsePath)) Directory.CreateDirectory(_chatResponsePath);

        
        
        // Set up the system prompt and tool definitions
        InitializeDirector();
        
        
        // DrawRegionInput.OnAgentSelectedByID += OnAgentSelectedByID; // Updated
    }
    



    /// <summary>
    /// EXTENDED: Sets up the initial system prompt and defines the tools
    /// the LLM can use.
    /// </summary>
    private void InitializeDirector()
    {
        // 1. Add the System Prompt to the chat history
        // This tells the LLM its job.
        //FUNDA
        
        Debug.Log(systemPrompt.text);
                this.systemInstruction = new Content
        {
            role = "system",
            parts = new List<Part> { new Part { text = systemPrompt.text } }
        };

        
    }



    private void OnSendButtonClick()
    {
        string message = userInputField.text;
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        SetUIInteractable(false);

        AddMessageToUI(message, userMessagePrefab);

        
        // Add the user message to the chat history
        chatHistory.Add(new Content { role = "user", parts = new List<Part> { new Part { text = message } } });


        userInputField.text = "";

        // EXTENDED: Call the modified coroutine
        StartCoroutine(GetLLMResponseLoop());
    }

    
    public void AddMessageToUI(string message, GameObject prefab)
    {
        if (chatContentPanel == null || prefab == null) return;

        GameObject messageObject = Instantiate(prefab, chatContentPanel);

        ChatMessageUI chatMessage = messageObject.GetComponent<ChatMessageUI>();
        if (chatMessage != null)
        {
            chatMessage.SetText(message);
        }
        else
        {
            TMP_Text tmpText = messageObject.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = message;
            }
            else
            {
                Debug.LogWarning("Message prefab is missing ChatMessageUI script and TMP_Text component.");
            }
        }
    }

    private void SaveResponseAsJson(string response) {
        string trimmed = response.Trim();
        if (trimmed.StartsWith("```"))
        {
            int firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0) trimmed = trimmed[(firstNewline + 1)..];
            if (trimmed.EndsWith("```")) trimmed = trimmed[..trimmed.LastIndexOf("```")].TrimEnd();
        }
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(_chatResponsePath, $"response_{timestamp}.json");
        File.WriteAllText(filePath, trimmed);
        if (OCCController.Instance != null) OCCController.Instance.SetLatestChatResponse(trimmed);
        StartCoroutine(SynthesizeAndExtractVisemes(trimmed));
    }

    private IEnumerator SynthesizeAndExtractVisemes(string animationJson) {
        // 1. Parse utterance and duration from the animation JSON
        var (_, utterance, duration) = Parsers.ParseJson(animationJson);
        if (string.IsNullOrEmpty(utterance)) yield break;

        ShowProgress("Synthesizing speech...");

        // 2. Synthesize WAV via OS speech synthesis
        string audioDir = Path.Combine(Application.dataPath, "Resources", "Audio");
        if (!Directory.Exists(audioDir)) Directory.CreateDirectory(audioDir);
        string wavTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string wavPath = Path.Combine(audioDir, $"audio_{wavTimestamp}.wav");
        if (File.Exists(wavPath)) File.Delete(wavPath);

        string safeText = utterance.Replace("\"", "\\\"");
        var process = System.Diagnostics.Process.Start(
            "/usr/bin/say",
            $"-o \"{wavPath}\" --data-format=LEF32@44100 \"{safeText}\""
        );
        if (process == null) { Debug.LogError("Failed to start speech synthesis."); yield break; }
        while (!process.HasExited) yield return null;

        if (!File.Exists(wavPath) || new FileInfo(wavPath).Length == 0) {
            Debug.LogError("Speech synthesis produced no audio file.");
            yield break;
        }

        // 3. Base64-encode the WAV
        string base64Audio = Convert.ToBase64String(File.ReadAllBytes(wavPath));

        ShowProgress("Extracting visemes...");

        // 5. Build multimodal Gemini request
        string durationInstruction = $"The total animation duration is {duration:F3} seconds. " +
            $"Scale all viseme timestamps so the sequence spans exactly {duration:F3} seconds.";

        var payload = new GeminiPayload {
            contents = new List<Content> {
                new() {
                    role = "user",
                    parts = new List<Part> {
                        new() { inlineData = new() { mimeType = "audio/wav", data = base64Audio } },
                        new() { text = durationInstruction }
                    }
                }
            },
            systemInstruction = new() {
                role = "system",
                parts = new List<Part> { new() { text = visemePrompt.text } }
            }
        };

        string jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore
        });

        // 6. Send to Gemini
        using UnityWebRequest request = new(ApiUrlBase + apiKey, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Viseme extraction error: " + request.error);
            yield break;
        }

        var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(request.downloadHandler.text);
        string visemeText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text;
        if (string.IsNullOrEmpty(visemeText)) { Debug.LogError("Empty viseme response from Gemini."); yield break; }

        // 7. Strip markdown fences
        visemeText = visemeText.Trim();
        if (visemeText.StartsWith("```")) {
            int nl = visemeText.IndexOf('\n');
            if (nl >= 0) visemeText = visemeText[(nl + 1)..];
            if (visemeText.EndsWith("```")) visemeText = visemeText[..visemeText.LastIndexOf("```")].TrimEnd();
        }

        // 8. Save viseme JSON to Resources/Speech/
        string speechDir = Path.Combine(Application.dataPath, "Resources", "Visemes");
        if (!Directory.Exists(speechDir)) Directory.CreateDirectory(speechDir);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        File.WriteAllText(Path.Combine(speechDir, $"visemes_{timestamp}.json"), visemeText);

        // 9. Push viseme data to all agents
        if (OCCController.Instance != null)
            OCCController.Instance.SetVisemeData(visemeText);

        // 10. Load out.wav and set as the speech clip on all agents
        using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(
            "file://" + wavPath, AudioType.WAV);
        yield return audioRequest.SendWebRequest();

        if (audioRequest.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Failed to load synthesized WAV: " + audioRequest.error);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
        if (OCCController.Instance != null)
            OCCController.Instance.SetSpeechClip(clip);

        HideProgress();
        AddMessageToUI("Animation and visemes are ready. You can now play the animation.", llmMessagePrefab);
        ScrollToBottom();
    }

    private void ShowProgress(string text) {
        if (_progressObject == null)
            _progressObject = Instantiate(llmMessagePrefab, chatContentPanel);
        var chatMsg = _progressObject.GetComponent<ChatMessageUI>();
        if (chatMsg != null)
            chatMsg.SetText(text);
        else {
            var tmp = _progressObject.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = text;
        }
        ScrollToBottom();
    }

    private void HideProgress() {
        if (_progressObject != null) {
            Destroy(_progressObject);
            _progressObject = null;
        }
    }

    private void SetUIInteractable(bool isInteractable)
    {
        userInputField.interactable = isInteractable;
        sendButton.interactable = isInteractable;
    }

    private void ScrollToBottom()
    {
        StartCoroutine(ForceScrollDown());
    }

    private IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }
    
    
    
    /// <summary>
/// Makes a single, non-conversational call to the LLM.
/// Used for internal tasks like data generation.
/// </summary>
      private IEnumerator GetLLMResponseLoop()
    {
        ShowProgress("Generating animation...");
        string url = ApiUrlBase + apiKey;


        var payload = new GeminiPayload
        {
            contents = this.chatHistory,
            systemInstruction = this.systemInstruction
        };

        // EXTENDED: Serialize using Newtonsoft.Json
        string jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // EXTENDED: Deserialize using Newtonsoft.Json
                var response = JsonConvert.DeserializeObject<GeminiResponse>(request.downloadHandler.text);

                if (response?.candidates == null || response.candidates.Count == 0)
                {
                    Debug.LogError("Gemini API Response: Invalid format or empty candidates.");
                    AddMessageToUI("Error: Could not parse LLM response.", llmMessagePrefab);
                    SetUIInteractable(true);
                    yield break;
                }

                // Get the response content from the first candidate
                Content responseContent = response.candidates[0].content;
                // Add the model's response (whether text or tool) to history
                chatHistory.Add(responseContent);


                if (responseContent.parts != null && responseContent.parts.Count > 0)
                {
                    
                    if (responseContent.parts[0].text != null)
                    {
                        // This is a regular text response
                        string llmResponseText = responseContent.parts[0].text;
                        HideProgress();
                        AddMessageToUI(llmResponseText, llmMessagePrefab);
                        SaveResponseAsJson(llmResponseText);
                        
                        SetUIInteractable(true);
                        userInputField.ActivateInputField();
                        ScrollToBottom();
                    }
                    else
                    {
                        // No valid parts
                        AddMessageToUI("Received an empty response.", llmMessagePrefab);
                        SetUIInteractable(true);
                    }
                }
                else
                {
                    Debug.Log("Empty response");    
                    SetUIInteractable(true);
                }
            }
            else
            {
                Debug.LogError($"Gemini API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                AddMessageToUI($"Error: {request.error}", llmMessagePrefab);
                SetUIInteractable(true);
            }
        }
    }
 
    
}