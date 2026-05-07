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


/// <summary>
/// Manages the chat UI, user input, and communication with the Gemini API,
/// including handling tool calls for simulation control.
/// </summary>
public class ChatManager : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("Enter your Gemini API key here. DO NOT expose this in production builds.")]
    [SerializeField]
    private string apiKey = "[YOUR_API_KEY_HERE]";
    // EXTENDED: Changed constant to be just the base URL
    private const string ApiUrlBase = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key=" ;

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

    
    private string _savePath;
    
    
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
        TextAsset obscure = Resources.Load("gemini-api-key") as TextAsset;
        return obscure.text.Trim();
    }

    void Start()
    {
        sendButton.onClick.AddListener(OnSendButtonClick);
        userInputField.onSubmit.AddListener((_) => OnSendButtonClick());
        chatHistory = new List<Content>();

        _savePath = Path.Combine(Application.dataPath, "Resources", "ChatHistory");
        if (!Directory.Exists(_savePath)) Directory.CreateDirectory(_savePath);

        apiKey = Key();

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
        TextAsset systemPrompt = Resources.Load("LLM-Context/occContext") as TextAsset;
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
        string url = ApiUrlBase + apiKey;

        Debug.Log(url);
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
                        AddMessageToUI(llmResponseText, llmMessagePrefab);
                        
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