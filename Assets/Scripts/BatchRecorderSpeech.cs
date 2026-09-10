using UnityEngine;

/// <summary>
/// This component holds the references for the batch recording process.
/// Attach this to a GameObject in your scene.
/// </summary>
public class BatchRecorderSpeech : MonoBehaviour {
    [Header("Asset References")]
    [Tooltip("The TextAsset that will be updated with each file from the folder. A script in your scene can read this asset to configure itself based on the file content.")]
    public TextAsset Scenario;

#if UNITY_EDITOR
    [Tooltip("Drag the folder containing your scenario files (e.g., .txt, .json) here.")]
    public UnityEditor.DefaultAsset sourceFolder;
#endif

    [Header("Recording Settings")]
    [Tooltip("The output resolution for the recorded videos.")]
    public Vector2Int videoResolution = new Vector2Int(1920, 1080);

    [Tooltip("Subfolder within your project's root directory to save recordings.")]
    public string outputFolder = "Recordings";

    [Tooltip("The duration (in seconds) to record for each scenario file. Use this for procedural animations that don't have a fixed AnimationClip length.")]
    public float recordingDuration = 10.0f;

    // Retain immediate playback for callers outside the recording editor.
    public void SetCurrentlyProcessingFile() {
        if (PrepareScenario())
            GetComponent<OCCController>().PlayResponse();
    }

    // Load the complete scenario before recording, without starting playback.
    public bool PrepareScenario() {
        OCCController controller = GetComponent<OCCController>();
        if (Scenario == null || controller == null || controller.Agents == null || controller.Agents.Length == 0) {
            Debug.LogError("Cannot prepare speech recording: assign a scenario, controller, and agents.");
            return false;
        }

        string scenarioName = Scenario.name;
        TextAsset visemes = Resources.Load<TextAsset>("Visemes/" + scenarioName + "_visemes");
        AudioClip audio = Resources.Load<AudioClip>("Audio/" + scenarioName);
        if (visemes == null || audio == null) {
            Debug.LogError($"Cannot record '{scenarioName}': matching audio or visemes are missing.");
            return false;
        }

        FACS[] faces = new FACS[controller.Agents.Length];
        for (int i = 0; i < faces.Length; i++) {
            faces[i] = controller.Agents[i] != null ? controller.Agents[i].GetComponent<FACS>() : null;
            if (faces[i] == null) {
                Debug.LogError($"Cannot record '{scenarioName}': agent {i} has no FACS component.");
                return false;
            }
        }

        // Batch recordings use their selected scenario, not a previous chat response.
        controller.SetLatestChatResponse(null);
        recordingDuration = audio.length;
        for (int i = 0; i < faces.Length; i++) {
            controller.UpdateScenario(Scenario, i);
            faces[i].EmotionName = scenarioName;
            faces[i].SetVisemeData(visemes.text);
            faces[i].SpeechClip = audio;
            faces[i].IsSpeechEnabled = true;
            recordingDuration = Mathf.Max(recordingDuration, faces[i].Duration);
        }
        return true;
    }
}
