using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using System.Collections;
using System.IO;
using System.Linq;
using System;
using UnityEngine;

/// <summary>
/// This is a custom editor for the BatchRecorderSpeech component.
/// It adds a button to the Inspector to start the batch process for procedural animations.
/// This script MUST be placed in a folder named "Editor".
/// </summary>
[CustomEditor(typeof(BatchRecorderSpeech))]
public class BatchRecorderSpeechEditor : Editor {

    private Unity.EditorCoroutines.Editor.EditorCoroutine currentBatchCoroutine;
    // A static flag to indicate that we should start recording as soon as we enter Play Mode.
    private static bool startRecordingOnPlay = false;

    public override void OnInspectorGUI() {
        // Draw the default inspector fields (for Scenario, folder, etc.)
        DrawDefaultInspector();

        BatchRecorderSpeech recorderComponent = (BatchRecorderSpeech)target;
        bool isRunning = currentBatchCoroutine != null;
        
        // Disable the start buttons if already running
        GUI.enabled = !isRunning;

        // Style the button to make it more prominent
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1.0f); // A nice blue color
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) {
            fontStyle = FontStyle.Bold,
            fontSize = 14,
            fixedHeight = 40
        };

        if(GUILayout.Button("Record All Scenarios", buttonStyle)) {
            if(Application.isPlaying) {
                currentBatchCoroutine = Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(RunBatchProcess(recorderComponent), this);
            }
        }

        if(GUILayout.Button("Record Single Scenario", buttonStyle)) {
            if(Application.isPlaying) {
                currentBatchCoroutine = Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(RunProcess(recorderComponent), this);
            }
        }

        // Re-enable GUI to allow the Cancel button to work
        GUI.enabled = true;

        if (isRunning) {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("STOP / CANCEL RECORDING", buttonStyle)) {
                StopActiveProcess();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void StopActiveProcess() {
        if (currentBatchCoroutine != null) {
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StopCoroutine(currentBatchCoroutine);
            currentBatchCoroutine = null;
            EditorUtility.ClearProgressBar();
            Debug.LogWarning("Batch recording was forcibly stopped by user.");
        }
    }

    public void StartRunProcessCoroutine(BatchRecorderSpeech recorderComponent) {
        Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(RunProcess(recorderComponent), this);
    }

    private IEnumerator RunProcess(BatchRecorderSpeech recorderComponent) {
        startRecordingOnPlay = false;
        Debug.Log("stopped play>");

        string fileName = recorderComponent.Scenario.name;  

        if (!recorderComponent.PrepareScenario()) {
            currentBatchCoroutine = null;
            EditorUtility.ClearProgressBar();
            yield break;
        }
    
        EditorUtility.SetDirty(recorderComponent);     // Mark the component as dirty to ensure the UI updates

        // --- 3. Configure and Start Recorder ---
        RecorderControllerSettings controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        RecorderController recorderController = new RecorderController(controllerSettings);

        var movieRecorder = SetupMovieRecorder(recorderComponent, fileName);
        controllerSettings.AddRecorderSettings(movieRecorder);

        // Set the recording to cover the duration specified in the inspector
        float durationInSeconds = recorderComponent.recordingDuration;

        controllerSettings.SetRecordModeToTimeInterval(0, durationInSeconds);

        Debug.Log($"Starting recording for '{fileName}'. Duration: {durationInSeconds:F2} seconds.");
        recorderController.PrepareRecording();
        if (!recorderController.StartRecording()) {
            recorderController.StopRecording();
            currentBatchCoroutine = null;
            EditorUtility.ClearProgressBar();
            Debug.LogError($"Could not start recording '{fileName}'.");
            yield break;
        }
        recorderComponent.GetComponent<OCCController>().PlayResponse();
        
        GameObject playAuObj = GameObject.Find("PlayAU");
        if (playAuObj != null) playAuObj.SetActive(false);
        
        // We just need to wait for the specified duration. The procedural animation should
        // be running in the background based on the new TextAsset.
        yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(durationInSeconds + 1f); // Add a small buffer for safety

        // Ensure the recorder has fully stopped and saved the file
        while(recorderController.IsRecording()) {
            if (playAuObj != null) playAuObj.SetActive(false);
            yield return null;
        }

        Debug.Log($"Finished recording for '{fileName}'.");

        // Clean up the recorder controller to free up memory
        recorderController.StopRecording();

        if (playAuObj != null) playAuObj.SetActive(true);
     
        EditorUtility.ClearProgressBar();
        currentBatchCoroutine = null;
    }

    private IEnumerator RunBatchProcess(BatchRecorderSpeech recorderComponent) {
        startRecordingOnPlay = false;
        if(!Validate(recorderComponent)) {
            yield break; // Stop if validation fails
        }
        
        string folderPath = AssetDatabase.GetAssetPath(recorderComponent.sourceFolder);

        // --- MODIFIED FILE SEARCH LOGIC ---
        // Find all files in the folder and filter them to only include files with the ".json" extension, ignoring case.
        string[] filePaths = Directory.GetFiles(folderPath)
                                     .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();

        if(filePaths.Length == 0) {
            Debug.LogWarning($"No '.json' files found in folder: {folderPath}. Make sure your scenario files are in the selected folder and have the correct extension.");
            yield break;
        }

        Debug.Log($"Found {filePaths.Length} JSON files to process. Starting batch recording...");

        // --- 2. Loop through each file ---
        for(int i = 0; i < filePaths.Length; i++) {
            string filePath = filePaths[i];
            string fileName = Path.GetFileName(filePath);

            // Display progress in the console and the Editor progress bar
            EditorUtility.DisplayProgressBar(
                "Batch Recording",
                $"Processing file: {fileName} ({i + 1}/{filePaths.Length})",
                (float)(i + 1) / filePaths.Length
            );

            recorderComponent.Scenario = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
            
            if (!recorderComponent.PrepareScenario()) {
                currentBatchCoroutine = null;
                EditorUtility.ClearProgressBar();
                yield break;
            }
            
            EditorUtility.SetDirty(recorderComponent); // Mark the component as dirty to ensure the UI updates
            
            yield return null;

            // --- 3. Configure and Start Recorder ---
            RecorderControllerSettings controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            RecorderController recorderController = new RecorderController(controllerSettings);

            var movieRecorder = SetupMovieRecorder(recorderComponent, fileName);
            controllerSettings.AddRecorderSettings(movieRecorder);

            float durationInSeconds = recorderComponent.recordingDuration;
            controllerSettings.SetRecordModeToTimeInterval(0, durationInSeconds);

            Debug.Log($"Starting recording for '{fileName}'. Duration: {durationInSeconds:F2} seconds.");
            recorderController.PrepareRecording();
            if (!recorderController.StartRecording()) {
                recorderController.StopRecording();
                currentBatchCoroutine = null;
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Could not start recording '{fileName}'.");
                yield break;
            }
            recorderComponent.GetComponent<OCCController>().PlayResponse();

            // --- 4. Wait for Recording to Finish ---
            // We just need to wait for the specified duration. The procedural animation should
            // be running in the background based on the new TextAsset.
            yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(durationInSeconds + 0.2f); // Add a small buffer for safety

            // Ensure the recorder has fully stopped and saved the file
            while(recorderController.IsRecording()) {
                yield return null;
            }

            Debug.Log($"Finished recording for '{fileName}'.");

            // Clean up the recorder controller to free up memory
            recorderController.StopRecording();

            // Optional: A small delay between recordings can help prevent issues
            yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(1f);
        }

        // --- 5. Cleanup ---
        EditorUtility.ClearProgressBar();
        Debug.Log("Batch recording process finished successfully!");
        
        currentBatchCoroutine = null;
    }

    /// <summary>
    /// Configures the settings for a MovieRecorder instance.
    /// </summary>
    private MovieRecorderSettings SetupMovieRecorder(BatchRecorderSpeech recorderComponent, string inputFileName) {
        var movieRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorder.name = "Scenario Video Recorder";
        movieRecorder.Enabled = true;

        // Video Settings from the component
        movieRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movieRecorder.VideoBitRateMode = VideoBitrateMode.High;
        movieRecorder.ImageInputSettings = new GameViewInputSettings {
            OutputWidth = recorderComponent.videoResolution.x,
            OutputHeight = recorderComponent.videoResolution.y
        };

        // Audio Settings
        movieRecorder.AudioInputSettings.PreserveAudio = true;

        // --- CORRECTED FILE PATH LOGIC ---
        // We get the name of the input file without its extension (e.g., ".txt" or ".json")
        string outputFileName = Path.GetFileNameWithoutExtension(inputFileName);

        // We manually combine the output folder and the desired filename.
        // The Recorder will automatically add the correct extension (e.g., ".mp4").
        string desiredPath = Path.Combine(recorderComponent.outputFolder, outputFileName);

        // Use forward slashes for cross-platform compatibility
        movieRecorder.OutputFile = desiredPath.Replace("\\", "/");

        return movieRecorder;
    }

    private bool Validate(BatchRecorderSpeech recorderComponent) {
        if(recorderComponent.sourceFolder == null) {
            Debug.LogError("Validation Failed: Source Folder is not assigned on the BatchRecorderSpeech component.", recorderComponent);
            return false;
        }
        if(recorderComponent.recordingDuration <= 0) {
            Debug.LogError("Validation Failed: Recording Duration must be greater than zero.", recorderComponent);
            return false;
        }
        return true;
    }
}