using UnityEngine;

/// <summary>
/// This component holds the references for the batch recording process.
/// Attach this to a GameObject in your scene. The actual logic is handled
/// by the BatchRecorderEditor.cs script.
/// </summary>
public class BatchRecorder : MonoBehaviour {
    [Header("Asset References")]
    [Tooltip("The TextAsset that will be updated with each file from the folder. A script in your scene can read this asset to configure itself based on the file content.")]
    public TextAsset Scenario;

    //[Tooltip("The Animator component that will play the animation to be recorded.")]
    //public Animator animator;

    
    [Tooltip("Drag the folder containing your scenario files (e.g., .txt, .json) here.")]
    public UnityEditor.DefaultAsset sourceFolder;

    [Header("Recording Settings")]
    [Tooltip("The output resolution for the recorded videos.")]
    public Vector2Int videoResolution = new Vector2Int(1920, 1080);

    [Tooltip("Subfolder within your project's root directory to save recordings.")]
    public string outputFolder = "Recordings";


    [Tooltip("The duration (in seconds) to record for each scenario file. Use this for procedural animations that don't have a fixed AnimationClip length.")]
    public float recordingDuration = 10.0f;


    
    /// <summary>
    /// This is a helper method called by the editor script to update the state.
    /// </summary>
    public void SetCurrentlyProcessingFile() {

        GetComponent<OCCController>().UpdateScenario(Scenario);
        recordingDuration = GetComponentInChildren<FACS>().Duration;       
        GetComponent<OCCController>().PlayResponse();

    }


   
}
