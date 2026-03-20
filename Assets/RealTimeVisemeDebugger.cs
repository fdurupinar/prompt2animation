using UnityEngine;

public class RealTimeVisemeDebugger : MonoBehaviour
{
    private OVRLipSyncContext lipsyncContext;
    private AudioSource audioSource;

    void Start()
    {
        lipsyncContext = GetComponent<OVRLipSyncContext>();
        audioSource = GetComponent<AudioSource>();

        if (lipsyncContext == null)
        {
            Debug.LogError("RealTimeVisemeDebugger needs an OVRLipSyncContext on this object!");
        }
    }

    void Update()
    {
        if (lipsyncContext == null) return;

        // 1. Get the current live frame from the analysis engine
        OVRLipSync.Frame currentFrame = lipsyncContext.GetCurrentPhonemeFrame();

        if (currentFrame == null) return;

        // 2. Identify the viseme with the highest weight
        int topVisemeIndex = -1;
        float maxWeight = 0.15f; // Threshold to ignore tiny movements/noise

        for (int i = 0; i < currentFrame.Visemes.Length; i++)
        {
            if (currentFrame.Visemes[i] > maxWeight)
            {
                maxWeight = currentFrame.Visemes[i];
                topVisemeIndex = i;
            }
        }

        // 3. Print the active viseme and the current audio timestamp
        if (topVisemeIndex != -1)
        {
            string visemeName = ((OVRLipSync.Viseme)topVisemeIndex).ToString();
            float timeStamp = audioSource != null ? audioSource.time : Time.time;

            Debug.Log($"[Time: {timeStamp:F3}s] Active: {visemeName} | Weight: {maxWeight:F2}");
        }
    }
}