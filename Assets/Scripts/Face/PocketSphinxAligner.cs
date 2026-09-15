using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>Optional local English forced alignment. Requires a desktop Python runtime.</summary>
public sealed class PocketSphinxAligner : MonoBehaviour {
    [Tooltip("Python executable with PocketSphinx, NumPy and SciPy. Blank uses Tools/PocketSphinx/.venv/bin/python3 in the project.")]
    public string pythonExecutable = "";
    [Min(1)] public float timeoutSeconds = 120;
    [Tooltip("Optional JSON dictionary of custom ARPAbet pronunciations, e.g. {\"ew\":\"UW\"}.")]
    public TextAsset pronunciationOverrides;

    [Serializable]
    public sealed class Result {
        public float duration;
        public List<AlignedPhoneme> phonemes;
        public string method;
        [JsonIgnore] public string VisemeJson;
    }

    private readonly HashSet<Process> runningProcesses = new HashSet<Process>();

    private void OnDisable() {
        foreach (var process in runningProcesses) {
            try { if (!process.HasExited) process.Kill(); } catch (InvalidOperationException) { }
        }
    }

    // Arguments are passed to Python directly, never through a shell.
    private static string Quote(string value) {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public IEnumerator Align(string wavPath, string transcript, Action<Result> onSuccess, Action<string> onFailure) {
        string temp = null;
        Process process = null;
        Task<string> output = null, errors = null;
        string setupError = null;
        try {
            if (string.IsNullOrWhiteSpace(transcript)) throw new ArgumentException("A spoken transcript is required.");
            if (!File.Exists(wavPath)) throw new FileNotFoundException("Audio file was not found.", wavPath);
            string python = string.IsNullOrWhiteSpace(pythonExecutable)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/PocketSphinx/.venv/bin/python3"))
                : pythonExecutable;
            if (!File.Exists(python)) throw new FileNotFoundException("Set up PocketSphinx first; see Tools/PocketSphinx/README.md. Python executable not found.", python);
            string script = Path.Combine(Application.streamingAssetsPath, "PocketSphinx", "align.py");
            if (!File.Exists(script)) throw new FileNotFoundException("PocketSphinx alignment script is missing.", script);
            temp = Path.Combine(Application.temporaryCachePath, "phoneme-alignment-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            var pronunciations = pronunciationOverrides == null ? new Dictionary<string, string>()
                : JsonConvert.DeserializeObject<Dictionary<string, string>>(pronunciationOverrides.text);
            string request = Path.Combine(temp, "request.json");
            File.WriteAllText(request, JsonConvert.SerializeObject(new { audio = Path.GetFullPath(wavPath), transcript,
                pronunciations = pronunciations ?? new Dictionary<string, string>() }));
            process = new Process { StartInfo = new ProcessStartInfo {
                FileName = python, Arguments = Quote(script) + " " + Quote(request),
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            } };
            if (!process.Start()) throw new InvalidOperationException("Could not start Python.");
            runningProcesses.Add(process);
            output = process.StandardOutput.ReadToEndAsync();
            errors = process.StandardError.ReadToEndAsync();
        } catch (Exception ex) { setupError = ex.Message; }

        try {
            if (setupError != null) { onFailure?.Invoke(setupError); yield break; }
            float started = Time.realtimeSinceStartup;
            while (!process.HasExited || !output.IsCompleted || !errors.IsCompleted) {
                if (!isActiveAndEnabled) { onFailure?.Invoke("Alignment cancelled."); yield break; }
                if (Time.realtimeSinceStartup - started > Mathf.Max(1, timeoutSeconds)) {
                    onFailure?.Invoke("PocketSphinx alignment timed out."); yield break;
                }
                yield return null;
            }
            string error = null;
            Result result = null;
            try {
                if (process.ExitCode != 0) throw new InvalidOperationException(errors.Result.Trim());
                result = JsonConvert.DeserializeObject<Result>(output.Result);
                if (result == null) throw new InvalidOperationException("Empty alignment result.");
                result.VisemeJson = JsonConvert.SerializeObject(
                    PhonemeToVisemeConverter.Convert(result.phonemes, result.duration), Formatting.Indented);
            } catch (Exception ex) { error = ex.Message; }
            if (error != null) onFailure?.Invoke(error);
            else onSuccess?.Invoke(result);
        } finally {
            if (process != null) {
                runningProcesses.Remove(process);
                try { if (!process.HasExited) process.Kill(); } catch (InvalidOperationException) { }
                process.Dispose();
            }
            if (temp != null && Directory.Exists(temp)) Directory.Delete(temp, true);
        }
    }
}
