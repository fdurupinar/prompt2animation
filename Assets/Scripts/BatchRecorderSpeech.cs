using UnityEngine;

/// <summary>
/// This component holds the references for the batch recording process.
/// Attach this to a GameObject in your scene.
/// </summary>
public class BatchRecorderSpeech : MonoBehaviour {
    [Header("Asset References")]
    [Tooltip("Any scenario JSON, including custom tests. Its basename selects Audio/<name> and Visemes/<name>_visemes under Resources.")]
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

    [Min(0f)]
    [Tooltip("Extra time recorded after the audio/animation duration to capture the final mouth transition. Applied equally to both suppression versions.")]
    public float recordingTailSeconds = 0.5f;

    [Header("Viseme subtitles")]
    public bool showSuppressionOverlay = true;
    [Tooltip("Also show subtitles in Play mode outside batch recording.")]
    public bool previewOverlay;
    [Tooltip("Show the viseme on suppression-off recordings too.")]
    public bool showOverlayWithoutSuppression;
    [Tooltip("Fixed top-center position in 1920x1080 reference pixels.")]
    public Vector2 overlayPosition = new Vector2(960f, 620f);
    [Min(250f)] public float overlayPanelWidth = 520f;
    [System.NonSerialized] public bool IsRecording;
    private GUIStyle overlayStyle;

    private void OnGUI() {
        if (!Application.isPlaying || !showSuppressionOverlay || (!IsRecording && !previewOverlay)) return;
        var controller = GetComponent<OCCController>();
        if (controller == null || controller.Agents == null) return;
        if (overlayStyle == null) {
            overlayStyle = new GUIStyle(GUI.skin.box) {
                alignment = TextAnchor.UpperLeft, fontSize = 24,
                padding = new RectOffset(18, 18, 14, 14), wordWrap = true
            };
            overlayStyle.normal.textColor = Color.white;
        }
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f));
        try {
            float y = overlayPosition.y;
            foreach (var agent in controller.Agents) {
                var face = agent != null ? agent.GetComponent<FACS>() : null;
                if (face == null || !face.IsSpeechEnabled ||
                    (face.SuppressionOff && !showOverlayWithoutSuppression)) continue;
                string text = face.GetSuppressionOverlayText();
                float width = Mathf.Clamp(overlayPanelWidth, 250f, 1920f);
                float height = overlayStyle.CalcHeight(new GUIContent(text), width);
                float x = Mathf.Clamp(overlayPosition.x - width / 2f, 0f, 1920f - width);
                GUI.Box(new Rect(x, y, width, height), text, overlayStyle);
                y += height + 12f;
            }
        } finally { GUI.matrix = previous; }
    }

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
