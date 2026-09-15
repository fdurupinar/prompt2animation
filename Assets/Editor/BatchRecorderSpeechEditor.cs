using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using Unity.EditorCoroutines.Editor;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

[CustomEditor(typeof(BatchRecorderSpeech))]
public class BatchRecorderSpeechEditor : Editor {
    private EditorCoroutine currentBatchCoroutine;
    private RecorderController activeRecorder;
    private bool running;
    private BatchRecorderSpeech recordingComponent;
    private FACS[] comparisonFaces;
    private bool[] savedSuppressionOff, savedAUsOn, savedSpeechEnabled;
    private OCCController controller;
    private bool savedCanvasEnabled;

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox(
            "Any scenario name is supported. For groupPhotoSmile.json, use Audio/groupPhotoSmile.wav " +
            "and Visemes/groupPhotoSmile_visemes.json under Resources. Both Versions enables AUs and " +
            "records suppression on/off into separate subfolders, then restores your settings.", MessageType.Info);
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play mode to record.", MessageType.Info);

        var component = (BatchRecorderSpeech)target;
        using (new EditorGUI.DisabledScope(running || !Application.isPlaying)) {
            if (GUILayout.Button("Record Single Scenario")) StartRecording(component, false, false);
            if (GUILayout.Button("Record Single Scenario — Both Versions")) StartRecording(component, false, true);
            if (GUILayout.Button("Record All Scenarios")) StartRecording(component, true, false);
            if (GUILayout.Button("Record All Scenarios — Both Versions")) StartRecording(component, true, true);
        }
        if (running && GUILayout.Button("STOP / CANCEL RECORDING")) StopActiveProcess();
    }

    public void StartRunProcessCoroutine(BatchRecorderSpeech component) {
        StartRecording(component, false, false);
    }

    private void StartRecording(BatchRecorderSpeech component, bool all, bool both) {
        if (running || !Application.isPlaying) return;
        running = true;
        recordingComponent = component;
        currentBatchCoroutine = EditorCoroutineUtility.StartCoroutine(Run(component, all, both), this);
    }

    private void OnDisable() { StopActiveProcess(); }

    private void StopActiveProcess() {
        if (currentBatchCoroutine != null) {
            EditorCoroutineUtility.StopCoroutine(currentBatchCoroutine);
            currentBatchCoroutine = null;
        }
        CleanupRecording();
    }

    private void CleanupRecording() {
        if (recordingComponent != null) recordingComponent.IsRecording = false;
        recordingComponent = null;
        if (activeRecorder != null) {
            activeRecorder.StopRecording();
            activeRecorder = null;
        }
        if (comparisonFaces != null) {
            for (int i = 0; i < comparisonFaces.Length; i++) {
                var face = comparisonFaces[i];
                if (face == null) continue;
                face.StopAllCoroutines();
                var audio = face.GetComponent<AudioSource>();
                if (audio != null) audio.Stop();
                face.ResetShapeKeys();
                face.SuppressionOff = savedSuppressionOff[i];
                face.AUsOn = savedAUsOn[i];
                face.IsSpeechEnabled = savedSpeechEnabled[i];
            }
            comparisonFaces = null;
        }
        if (controller != null && controller.UICanvas != null)
            controller.UICanvas.enabled = savedCanvasEnabled;
        controller = null;
        EditorUtility.ClearProgressBar();
        running = false;
    }

    private IEnumerator Run(BatchRecorderSpeech component, bool all, bool both) {
        // Let StartRecording retain the coroutine handle before validation can finish.
        yield return null;
        try {
            TextAsset[] scenarios;
            if (all) {
                string folder = AssetDatabase.GetAssetPath(component.sourceFolder);
                if (!AssetDatabase.IsValidFolder(folder)) {
                    Debug.LogError("Assign a scenario Source Folder before batch recording.");
                    yield break;
                }
                scenarios = Directory.GetFiles(folder)
                    .Where(p => p.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .Select(p => AssetDatabase.LoadAssetAtPath<TextAsset>(p)).ToArray();
            } else {
                scenarios = new[] { component.Scenario };
            }
            if (scenarios.Length == 0 || scenarios.Any(s => s == null)) {
                Debug.LogError("Assign a scenario JSON file, or choose a folder containing scenario JSON files.");
                yield break;
            }
            controller = component.GetComponent<OCCController>();
            if (controller == null || controller.Agents == null || controller.Agents.Length == 0) {
                Debug.LogError("Assign an OCCController and its agents before recording.");
                yield break;
            }
            savedCanvasEnabled = controller.UICanvas != null && controller.UICanvas.enabled;
            var faces = controller.Agents.Select(a => a != null ? a.GetComponent<FACS>() : null).ToArray();
            if (faces.Any(f => f == null)) {
                Debug.LogError("Every recording agent needs a FACS component.");
                yield break;
            }
            savedSuppressionOff = faces.Select(f => f.SuppressionOff).ToArray();
            savedAUsOn = faces.Select(f => f.AUsOn).ToArray();
            savedSpeechEnabled = faces.Select(f => f.IsSpeechEnabled).ToArray();
            comparisonFaces = faces;

            foreach (var scenario in scenarios) {
                int versions = both ? 2 : 1;
                for (int version = 0; version < versions; version++) {
                    component.Scenario = scenario;
                    if (both) {
                        foreach (var face in faces) {
                            face.SuppressionOff = version == 1;
                            face.AUsOn = true;
                        }
                    }
                    // Reload original AU values for each condition.
                    if (!component.PrepareScenario()) yield break;
                    string condition = both ? (version == 0 ? "Suppression-On" : "Suppression-Off") : "";
                    string folder = both ? Path.Combine(component.outputFolder, condition) : component.outputFolder;
                    Directory.CreateDirectory(folder);
                    string output = Path.Combine(folder, scenario.name).Replace("\\", "/");
                    EditorUtility.DisplayProgressBar("Speech recording", output, 0f);

                    var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                    var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
                    try {
                        movie.name = "Scenario Speech Recorder";
                        movie.Enabled = true;
                        movie.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
                        movie.VideoBitRateMode = VideoBitrateMode.High;
                        movie.ImageInputSettings = new GameViewInputSettings {
                            OutputWidth = component.videoResolution.x,
                            OutputHeight = component.videoResolution.y
                        };
                        movie.AudioInputSettings.PreserveAudio = true;
                        movie.OutputFile = output;
                        settings.AddRecorderSettings(movie);
                        // Audio and AU coroutines can outlast the nominal scenario duration.
                        settings.SetRecordModeToManual();
                        activeRecorder = new RecorderController(settings);
                        activeRecorder.PrepareRecording();
                        if (!activeRecorder.StartRecording()) {
                            Debug.LogError($"Could not start recording '{output}'.");
                            yield break;
                        }
                        Debug.Log($"Recording '{output}.mp4' until playback completes, then adding a tail; " +
                            string.Join(", ", faces.Select(f => $"{f.name}: SuppressionOff={f.SuppressionOff}, AUsOn={f.AUsOn}")));
                        component.IsRecording = true;
                        controller.PlayResponse();
                        float playbackStart = Time.time;
                        float? completedAt = null;
                        float tail = Mathf.Max(0f, component.recordingTailSeconds);
                        foreach (var face in faces)
                            tail = Mathf.Max(tail, 1f / Mathf.Max(0.01f, face.VisemeSmoothSpeed));
                        while (activeRecorder.IsRecording()) {
                            if (!Application.isPlaying) yield break;
                            bool playbackActive = Time.time - playbackStart < component.recordingDuration ||
                                faces.Any(f => f.IsAUAnimationPlaying ||
                                    (f.GetComponent<AudioSource>() != null && f.GetComponent<AudioSource>().isPlaying));
                            if (playbackActive) completedAt = null;
                            else if (!completedAt.HasValue) completedAt = Time.time;
                            else if (Time.time - completedAt.Value >= tail) break;
                            yield return null;
                        }
                        Debug.Log($"Finished recording '{output}.mp4'.");
                    } finally {
                        component.IsRecording = false;
                        if (activeRecorder != null) {
                            activeRecorder.StopRecording();
                            activeRecorder = null;
                        }
                        DestroyImmediate(movie);
                        DestroyImmediate(settings);
                    }
                    foreach (var face in faces) {
                        face.StopAllCoroutines();
                        var audio = face.GetComponent<AudioSource>();
                        if (audio != null) audio.Stop();
                        face.ResetShapeKeys();
                    }
                    yield return null;
                }
            }
        } finally {
            CleanupRecording();
            currentBatchCoroutine = null;
        }
    }
}
