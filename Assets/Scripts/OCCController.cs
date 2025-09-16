using System.Collections;
using UnityEngine;



public class OCCController : MonoBehaviour {

    
    public TextAsset Scenario;
    public ProceduralHeatmapAnalyzer heatmapAnalyzer;

    public GameObject Agent;
    private FACS _facs;

    public bool ShowHeatmap = false;
    
    private void Start() {

        _facs = Agent.GetComponent<FACS>();

        _facs.VisemesOn = false;


        if (heatmapAnalyzer != null)            
            heatmapAnalyzer.DisableHeatmap();
    }



    public void UpdateScenario(TextAsset scenario) {
        Scenario = scenario;
        _facs.GetAUsAndDuration(Scenario.text);

        if (heatmapAnalyzer != null)            
            heatmapAnalyzer.DisableHeatmap();

    }

    public void PlayResponse() {


        string response = Scenario.text;

        Agent.GetComponent<FACS>().ResetShapeKeys();


        //heatmapAnalyzer.ResetHeatmapData();
        if (heatmapAnalyzer != null)
        {
            if (ShowHeatmap)                
                heatmapAnalyzer.EnableHeatmap();
            
            else

                heatmapAnalyzer.DisableHeatmap();
            
        }


        _facs.AUResponseCb(response);

        if(ShowHeatmap)
            StartCoroutine(DisplayHeatmap());


    }


    IEnumerator DisplayHeatmap() {


        //Debug.Log(_facs.Duration);
        yield return new WaitForSeconds(_facs.Duration+ 1f);

         _facs.ResetShapeKeys();
       

        //heatmapAnalyzer.ResetHeatmapData();
        heatmapAnalyzer.DisableHeatmap();
        heatmapAnalyzer.EnableHeatmap();


    }

   

} 

    
    
    
    
    

    
