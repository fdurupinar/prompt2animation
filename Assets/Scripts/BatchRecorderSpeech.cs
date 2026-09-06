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

    /// <summary>
    /// This is a helper method called by the editor script to update the state.
    /// It uses the currently assigned Scenario variable.
    /// </summary>
    public void SetCurrentlyProcessingFile() {
        OCCController oCCController = GetComponent<OCCController>();
        
        for(int i = 0; i < oCCController.Agents.Length; i++)
        { 
            // Pass the assigned Scenario to the controller
            oCCController.UpdateScenario(Scenario, i);
            
            FACS facs = oCCController.Agents[i].GetComponent<FACS>();
            
            if (facs != null)
            {
                // The naming is based on the EmotionName variable within FACS.cs
                string emotionName = facs.EmotionName;

                // Update Visemes (.json) from Resources/Visemes with the "_visemes" suffix
                TextAsset visemeAsset = Resources.Load<TextAsset>("Visemes/" + emotionName + "_visemes");
                Debug.Log(visemeAsset);
                if (visemeAsset != null)
                {
                    facs.SetVisemeData(visemeAsset.text);
                }
                else
                {
                    Debug.LogWarning($"Viseme JSON not found for emotion: {emotionName}_visemes in Resources/Visemes");
                }

                // Update Audio (.wav) from Resources/Audio
                AudioClip audioClip = Resources.Load<AudioClip>("Audio/" + emotionName);
                                Debug.Log(audioClip);

                if (audioClip != null)
                {
                    facs.SpeechClip = audioClip;
                }
                else
                {
                    Debug.LogWarning($"AudioClip not found for emotion: {emotionName} in Resources/Audio");
                }
            }
            else
            {
                Debug.LogError("FACS component missing on Agent " + i);
            }

            recordingDuration = oCCController.AnimationDuration;
        }

        oCCController.PlayResponse();
    }
}