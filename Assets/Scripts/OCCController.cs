using System.Collections;
using UnityEngine;



public class OCCController : MonoBehaviour {

    
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

        _facs = new FACS[Agents.Length];
        AnimationDuration = 0f;
        for (int i = 0; i < Agents.Length; i++)
        {
            _facs[i] = Agents[i].GetComponent<FACS>();
            if(AnimationDuration<_facs[i].Duration)
            {
                AnimationDuration = _facs[i].Duration;
            }
        }

        


        if (heatmapAnalyzer != null)            
            heatmapAnalyzer.DisableHeatmap();
    }


    public void UpdateScenario(TextAsset scenario, int agentIndex) {
        Scenarios[agentIndex] = scenario;
        _facs[agentIndex].GetAUsAndDuration(Scenarios[agentIndex].text);

        if(AnimationDuration<_facs[agentIndex].Duration)
        {
            AnimationDuration = _facs[agentIndex].Duration;
        }

        if (heatmapAnalyzer != null)            
            heatmapAnalyzer.DisableHeatmap();

    }
    
    
    
    
    public void PlayResponse() {

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

            Agents[i].GetComponent<FACS>().EmotionName = Scenarios[i].name;
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

    
    
    
    
    

    
