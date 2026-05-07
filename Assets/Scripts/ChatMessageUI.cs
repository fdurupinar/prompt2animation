using UnityEngine;
using TMPro;

/// <summary>
/// A simple helper script to be placed on your chat message prefabs.
/// It provides a simple method to set the message text.
/// </summary>
public class ChatMessageUI : MonoBehaviour
{
    [Tooltip("The TextMeshProUGUI component that displays the chat message.")]
    [SerializeField]
    private TMP_Text messageText;

    /// <summary>
    /// Sets the text of the message UI element.
    /// </summary>
    /// <param name="text">The message to display.</param>
    public void SetText(string text)
    {
        if (messageText != null)
        {
            messageText.text = text;
        }
        else
        {
            Debug.LogError("ChatMessageUI: No 'messageText' (TMP_Text) component assigned!");
        }
    }
}
