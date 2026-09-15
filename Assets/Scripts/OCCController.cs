using System.Collections;
using UnityEngine;



public class OCCController : MonoBehaviour {

    
    [Tooltip("Select a scenario here. Each character automatically uses Audio/<scenario name> and Visemes/<scenario name>_visemes from Resources.")]
    public TextAsset [] Scenarios;
    public ProceduralHeatmapAnalyzer heatmapAnalyzer;

    public GameObject[] Agents;
    private FACS [] _facs;

    public float AnimationDuration { get; private set; }

    public bool ShowHeatmap = false;

    [Header("UI to hide during playback")]
    public Canvas UICanvas;

    public event System.Action OnAnimationStart;
    public event System.Action OnAnimationComplete;
    private int _pendingAgentCount;

    

    public static OCCController Instance { get; private set; }

    private string _sessionChatResponse;
    public bool IsPreparingSpeech { get; set; }

    public void SetLatestChatResponse(string json) => _sessionChatResponse = json;

    public void SetVisemeData(string json) {
        foreach (FACS facs in _facs)
            facs.SetVisemeData(json);
    }

    public void SetSpeechClip(AudioClip clip) {
        foreach (FACS facs in _facs) {
            facs.SpeechClip = clip;
            facs.IsSpeechEnabled = true;
        }
    }

    private void Start() {
        Instance = this;

        SyncScenarios();

        if (heatmapAnalyzer != null)            
            heatmapAnalyzer.DisableHeatmap();
    }


    // Also called by the Inspector so the linked character fields update in edit mode.
    public void SyncScenarios() {
        _facs = new FACS[Agents != null ? Agents.Length : 0];
        AnimationDuration = 0f;
        for (int i = 0; i < _facs.Length; i++) {
            _facs[i] = Agents[i] != null ? Agents[i].GetComponent<FACS>() : null;
            if (_facs[i] == null) continue;
            TextAsset scenario = Scenarios != null && i < Scenarios.Length ? Scenarios[i] : null;
            ApplyScenario(_facs[i], scenario);
            AnimationDuration = Mathf.Max(AnimationDuration, _facs[i].Duration);
        }
    }

    private void ApplyScenario(FACS face, TextAsset scenario) {
        string scenarioName = scenario != null ? scenario.name : "";
        face.EmotionName = scenarioName;
        face.visemeJsonFile = scenario != null
            ? Resources.Load<TextAsset>("Visemes/" + scenarioName + "_visemes") : null;
        face.SetVisemeData(face.visemeJsonFile != null ? face.visemeJsonFile.text : "[]");
        face.SpeechClip = scenario != null ? Resources.Load<AudioClip>("Audio/" + scenarioName) : null;
        if (scenario != null)
            (face.AUList, face.Utterance, face.Duration) = Parsers.ParseJson(scenario.text);
        else {
            face.AUList = new System.Collections.Generic.List<ActionUnit>();
            face.Utterance = "";
            face.Duration = 0f;
        }
    }

    public void UpdateScenario(TextAsset scenario, int agentIndex) {
        if (Scenarios == null || Scenarios.Length <= agentIndex)
            System.Array.Resize(ref Scenarios, agentIndex + 1);
        Scenarios[agentIndex] = scenario;
        _sessionChatResponse = null;
        // Only update this agent: batch setup may already have prepared other characters.
        if (_facs == null || _facs.Length != Agents.Length) _facs = new FACS[Agents.Length];
        _facs[agentIndex] = Agents[agentIndex].GetComponent<FACS>();
        ApplyScenario(_facs[agentIndex], scenario);
        AnimationDuration = 0f;
        foreach (var face in _facs)
            if (face != null) AnimationDuration = Mathf.Max(AnimationDuration, face.Duration);
        if (heatmapAnalyzer != null) heatmapAnalyzer.DisableHeatmap();
    }

    public void PlayResponse() {
        if (IsPreparingSpeech) {
            Debug.LogWarning("Speech is still being prepared. Wait until animation and visemes are ready.");
            return;
        }
        if (_sessionChatResponse == null) SyncScenarios();
        if (_facs == null || _facs.Length == 0) return;
        for (int i = 0; i < _facs.Length; i++) {
            if (_facs[i] == null || (_sessionChatResponse == null &&
                (Scenarios == null || i >= Scenarios.Length || Scenarios[i] == null))) {
                Debug.LogError("Assign a scenario and a FACS character for each OCC Controller slot.");
                return;
            }
            if (_sessionChatResponse == null && _facs[i].IsSpeechEnabled &&
                (_facs[i].visemeJsonFile == null || _facs[i].GetComponent<AudioSource>()?.clip == null)) {
                Debug.LogError($"Missing Audio/{Scenarios[i].name} or Visemes/{Scenarios[i].name}_visemes. Playback cancelled to avoid mismatched speech.");
                return;
            }
        }

        OnAnimationStart?.Invoke();
        SetUIVisible(false);

        // Reset subscriptions to avoid duplicates on repeated calls
        foreach (FACS facs in _facs)
            facs.OnAnimationComplete -= HandleAgentAnimationComplete;

        _pendingAgentCount = Agents.Length;

        foreach (FACS facs in _facs)
            facs.OnAnimationComplete += HandleAgentAnimationComplete;

        for(int i = 0; i < Agents.Length; i++)
        {
            string response = _sessionChatResponse ?? Scenarios[i].text;

            if (_sessionChatResponse == null) _facs[i].EmotionName = Scenarios[i].name;
            Agents[i].GetComponent<FACS>().ResetShapeKeys();

            if (heatmapAnalyzer != null)
            {
                if (ShowHeatmap)
                    heatmapAnalyzer.EnableHeatmap();
                else
                    heatmapAnalyzer.DisableHeatmap();
            }

            _facs[i].AUResponseCb(response);
        }

        if(ShowHeatmap)
            StartCoroutine(DisplayHeatmap());
    }

    private void HandleAgentAnimationComplete() {
        _pendingAgentCount--;
        if (_pendingAgentCount <= 0) {
            foreach (FACS facs in _facs)
                facs.OnAnimationComplete -= HandleAgentAnimationComplete;
            SetUIVisible(true);
            OnAnimationComplete?.Invoke();
        }
    }

    private void SetUIVisible(bool visible) {
        if (UICanvas != null) UICanvas.enabled = visible;
    }


    IEnumerator DisplayHeatmap(int agentIndex = 0) {


        //Debug.Log(_facs.Duration);
        yield return new WaitForSeconds(_facs[agentIndex].Duration+ 1f);

         _facs[agentIndex].ResetShapeKeys();
       

        //heatmapAnalyzer.ResetHeatmapData();
        heatmapAnalyzer.DisableHeatmap();
        heatmapAnalyzer.EnableHeatmap();


    }

   

} 

    
    
    
    
    

    
