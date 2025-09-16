
using UnityEngine;
using TMPro;


public class LLMController : MonoBehaviour {

    
    public TMP_InputField InputTMP;

    public TMP_Dropdown HighLowDD;
    

    bool _isGemini = true;


    string _utterance;    
    
    
    private Transform _agent;
    

    private bool _isWaitingResponse;


    ChatNetClient _client;
    public string PersonalityDescription { get; set; }
    public string EmotionDescription { get; set; }

    public string context;

    string _personality = "high extroversion";

    // Start is called before the first frame update
    void Start()
    {
        _isWaitingResponse = false;

        for(int i = 0; i < transform.childCount; i++) {
            Transform child = transform.GetChild(i);
            if(child.gameObject.activeSelf) {
                _agent = child;
            }
        }

        _client = gameObject.AddComponent<ChatNetClient>();

        _utterance = InputTMP.text;




        

        context = Resources.Load<TextAsset>("LLM-Context/occPrompt").text;


    }

    public bool IsWaiting() {
        return _isWaitingResponse;
    }

    

    public void UpdateUtteranceFromTMP() {
        _utterance = InputTMP.text;
        
    }

    

    public void UpdateUtterance(string val) {
        _utterance = val;       
        
    }


    public void GetLLMResponseAndPlay() {

        if(!_isWaitingResponse) {
            
           
            _client.Prompt(_isGemini, context , FaceResponseCb);
    

            _isWaitingResponse = true;
            Debug.Log("waiting");
        }

    }
    
    public void FaceResponseCb(string response) {
        _isWaitingResponse = false;
        _agent.GetComponent<FACS>().AUResponseCb(response);
        Debug.Log("responded");
        
    }


    public void Replay() {

        _agent.GetComponent<FACS>().PlayAnimation();
        

    }



}
