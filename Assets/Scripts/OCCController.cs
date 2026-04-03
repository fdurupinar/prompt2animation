using System.Collections;
using UnityEngine;



public class OCCController : MonoBehaviour {

    
    public TextAsset [] Scenarios;
    public ProceduralHeatmapAnalyzer heatmapAnalyzer;

    public GameObject[] Agents;
    private FACS [] _facs;

    public float AnimationDuration { get; private set; }

    public bool ShowHeatmap = false;
    

    private void Start() {

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

        for(int i = 0; i < Agents.Length; i++)
        {
         
        
            string response = Scenarios[i].text;

            Agents[i].GetComponent<FACS>().EmotionName = Scenarios[i].name;
            Agents[i].GetComponent<FACS>().ResetShapeKeys();


            //heatmapAnalyzer.ResetHeatmapData();
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


    IEnumerator DisplayHeatmap(int agentIndex = 0) {


        //Debug.Log(_facs.Duration);
        yield return new WaitForSeconds(_facs[agentIndex].Duration+ 1f);

         _facs[agentIndex].ResetShapeKeys();
       

        //heatmapAnalyzer.ResetHeatmapData();
        heatmapAnalyzer.DisableHeatmap();
        heatmapAnalyzer.EnableHeatmap();


    }

   

} 

    
    
    
    
    

    
