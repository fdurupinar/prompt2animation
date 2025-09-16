using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using System.Linq;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ActionUnit{
    
    public int AU { get; set; }    
    public List<float> Times { get; set; }
    public List<float> Intensities { get; set; }
    public int currInd { get; set; }    
    public string Semantics { get; set; }
}
[Serializable]
public class Viseme {
    public string viseme { get; set; }
    public string Semantics { get; set; }
    public List<float> Times { get; set; }
    public List<float> Intensities { get; set; }
    public int currInd { get; set; }
}
public class ShapeKey {    
    public int Ind { get; set; }    
    public float MaxValue { get; set; } ///This is specific to the model's shape keys
}

[Serializable]
public struct Personality {
    public float openness;
    public float conscientiousness;
    public float extroversion;
    public float agreeableness;
    public float neuroticism;
}




//Ee Er IH Ah Oh W_OO S_Z Ch_J F_V TH T_L_D_N B_M_P K_G_H_NG AE R
public enum VisemeEnum {
    EE,
    ER,
    IH,
    AH,
    OH,
    W_OO,
    S_Z,
    CH_J,
    F_V,
    TH,
    T_L_D_N,
    B_M_P,
    K_G_H_NG,
    AE,
    R
};



public class FACS : MonoBehaviour
{
    public static Dictionary<string, int> VisemeDict = new Dictionary<string, int> {
    {"EE", 0},
    {"ER", 1},
    {"IH", 2},
    {"AH", 3},
    {"OH", 4},
    {"W_OO", 5},
    {"S_Z", 6},
    {"CH_J", 7},
    {"F_V", 8},
    {"TH", 9},
    {"T_L_D_N", 10},
    {"B_M_P", 11},
    {"K_G_H_NG", 12},
    {"AE", 13},
    {"R", 14},    
};

    public bool IsSpeechEnabled = false;
    public int TestAUInd = 0;
    public SkinnedMeshRenderer _meshRendererBody;
    private SkinnedMeshRenderer _meshRendererTongue;
    public int ShapeKeyCntBody, ShapeKeyCntTongue;

    //public TMP_InputField SpeechBubble;

    public Text SpeechBubble;

    
    public float [] ShapeKeyVals;
    public float[] ShapeKeyTargets;
    private string _speech;
    public string Speech{
        set {
            _speech = value;
            

        }
        get {
            return _speech;
        }
    }
    private string _utterance;
    public string Utterance {
        set {
            _utterance = value;
            SpeechBubble.text = value;

        }
        get {
            return _utterance;
        }
    }

    public Transform Jaw;
    public Transform Head;
    public Transform Neck;
    public Transform [] Eyes;


    float _headTiltLeft = 0;
    float _headTiltRight = 0;

    float _headTurnLeft = 0;
    float _headTurnRight = 0;

    float _headTurnUp = 0;
    float _headTurnDown = 0;

    float _headForward = 0;
    float _headBackward = 0;

    float _eyeLookLeft = 0;
    float _eyeLookRight = 0;


    float _eyeLookUp = 0;
    float _eyeLookDown = 0;



    private Quaternion _jawRotInit;
    private Quaternion _jawRot; //We need this because animation overwrites the updates
    private Quaternion _headRotInit;
    private Quaternion _headRot; 
    private Quaternion _neckRotInit;
    private Quaternion _neckRot; //We need this because animation overwrites the updates
    private Quaternion [] _eyesRotInit  = new Quaternion[2];
    private Quaternion[] _eyesRot = new Quaternion[2];


    public List<ShapeKey>[] AUShapeKeys; //at each AU index, related blendshape keys are stored
    public List<ShapeKey>[] VisemeShapeKeys; //at each viseme index, related blendshape keys are stored

   

    [SerializeField]
    private float _startTimeAU;
    private float _startTimeViseme;


    public List<ActionUnit> AUList;
    public List<Viseme> VisemeList;

    public Viseme CurrentViseme { get; set; }


    public string Voice = "Alex";
    public int Wpm = 175;

    public float Duration;

    Personality _personality;

    public bool VisemesOn = true;
    public bool AUsOn = true;
    public ActionUnit currentAU = null;


    [SerializeField]
    bool _isTalking = false;
    public bool IsTalking
    {        
        get
        {
            return _isTalking;
        }
    }

    //ChatNetClient _client;

    Dictionary<string, int> _shapeKeyDict = new Dictionary<string, int>();

    public AudioClip SpeechClip {
        
        set {
            _audioSource.clip = value;
        }
    }
    AudioSource _audioSource;

    public TMP_Text UtteranceTMP;


    //Dictionary<string, int> _shapeKeyDictTongue = new Dictionary<string, int>();
    


    public bool IsWaitingResponse = false;

    private void Awake() {
        _jawRotInit = _jawRot = Jaw.localRotation;
        _headRotInit = _headRot = Head.localRotation;       
        _neckRotInit = _neckRot = Neck.localRotation;


        _eyesRotInit[0] = _eyesRot[0] = Eyes[0].localRotation;
        _eyesRotInit[1] = _eyesRot[1] = Eyes[1].localRotation;
    }
    // Start is called before the first frame update
    void Start() {

        AUShapeKeys = new List<ShapeKey>[66];
        VisemeShapeKeys = new List<ShapeKey>[15];

          //_client = GetComponent<ChatNetClient>();

        //_client.Prompt(prompt, TestResponseCb);

        _meshRendererBody = transform.Find("CC_Base_Body").GetComponent<SkinnedMeshRenderer>();
        _meshRendererTongue = transform.Find("CC_Base_Tongue").GetComponent<SkinnedMeshRenderer>();

       

        InitShapeKeysAndAUs();


        
        InitVisemes();


        ShapeKeyVals = new float[ShapeKeyCntBody+ShapeKeyCntTongue];
        ShapeKeyTargets = new float[ShapeKeyCntBody + ShapeKeyCntTongue];



        AUList = new List<ActionUnit>();

        
        VisemeList = new List<Viseme>();

        _personality = new Personality();

        //foreach(ActionUnit au in AUList) {

        //    //Debug.Log($"AU: {au.AU}");
        //    Debug.Log($"AU: {au.AU} Times: {string.Join(", ", au.Times)} Intensities: {string.Join(", ", au.Intensities)}");
        //    //Debug.Log($"Intensities: {string.Join(", ", au.Intensities)}");
        //    Debug.Log("------");
        //}

        //PlayAnimation();


        _audioSource = gameObject.AddComponent<AudioSource>();
        
        
    
    }

    //Ee Er IH Ah Oh W_OO S_Z Ch_J F_V TH T_L_D_N B_M_P K_G_H_NG AE R
    void InitVisemes() {

        for(int i = 0; i < VisemeShapeKeys.Length; i++)
            VisemeShapeKeys[i] = new List<ShapeKey>();

        VisemeShapeKeys[(int)VisemeEnum.EE].Add(new ShapeKey { Ind = _shapeKeyDict["EE"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.ER].Add(new ShapeKey { Ind = _shapeKeyDict["ER"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.IH].Add(new ShapeKey { Ind = _shapeKeyDict["IH"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.AH].Add(new ShapeKey { Ind = _shapeKeyDict["AH"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.OH].Add(new ShapeKey { Ind = _shapeKeyDict["OH"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.W_OO].Add(new ShapeKey { Ind = _shapeKeyDict["W_OO"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.S_Z].Add(new ShapeKey { Ind = _shapeKeyDict["S_Z"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.CH_J].Add(new ShapeKey { Ind = _shapeKeyDict["CH_J"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.F_V].Add(new ShapeKey { Ind = _shapeKeyDict["F_V"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.TH].Add(new ShapeKey { Ind = _shapeKeyDict["TH"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.T_L_D_N].Add(new ShapeKey { Ind = _shapeKeyDict["T_L_D_N"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.B_M_P].Add(new ShapeKey { Ind = _shapeKeyDict["B_M_P"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.K_G_H_NG].Add(new ShapeKey { Ind = _shapeKeyDict["K_G_H_NG"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.AE].Add(new ShapeKey { Ind = _shapeKeyDict["AE"], MaxValue = 100f });
        VisemeShapeKeys[(int)VisemeEnum.R].Add(new ShapeKey { Ind = _shapeKeyDict["R"], MaxValue = 100f });


    }

    
    void InitShapeKeysAndAUs() {

        ShapeKeyCntBody = _meshRendererBody.sharedMesh.blendShapeCount;
        ShapeKeyCntTongue = 0;


        string s = "";
        for(int i = 0; i < ShapeKeyCntBody; i++) {
            _shapeKeyDict.Add(_meshRendererBody.sharedMesh.GetBlendShapeName(i).ToUpper(), i);
            s += _meshRendererBody.sharedMesh.GetBlendShapeName(i).ToUpper() + " ";

        }

        
        ShapeKeyCntBody += 4;

        if(_meshRendererTongue) {
            ShapeKeyCntTongue = _meshRendererTongue.sharedMesh.blendShapeCount;

            for(int i = 0; i < ShapeKeyCntTongue; i++) {
                if(!_shapeKeyDict.ContainsKey(_meshRendererTongue.sharedMesh.GetBlendShapeName(i).ToUpper()))
                    _shapeKeyDict.Add(_meshRendererTongue.sharedMesh.GetBlendShapeName(i).ToUpper(), i + ShapeKeyCntBody);
                s += _meshRendererTongue.sharedMesh.GetBlendShapeName(i).ToUpper() + " ";

            }
        }

           
        for(int i = 0; i < AUShapeKeys.Length; i++)
            AUShapeKeys[i] = new List<ShapeKey>();

        


        //Assign blendshapes to aus
        AUShapeKeys[1].Add(new ShapeKey{ Ind = _shapeKeyDict["Brow_Raise_Inner_L".ToUpper()], MaxValue = 80f});
        AUShapeKeys[1].Add(new ShapeKey { Ind = _shapeKeyDict["Brow_Raise_Inner_R".ToUpper()], MaxValue = 80f });


        AUShapeKeys[2].Add(new ShapeKey { Ind = _shapeKeyDict["Brow_Raise_Outer_R".ToUpper()], MaxValue = 80f });
        AUShapeKeys[2].Add(new ShapeKey { Ind = _shapeKeyDict["Brow_Raise_Outer_L".ToUpper()], MaxValue = 80f });

        AUShapeKeys[3].Add(new ShapeKey { Ind = _shapeKeyDict["Brow_Raise_Outer_R".ToUpper()], MaxValue = 80f });
        AUShapeKeys[3].Add(new ShapeKey { Ind = _shapeKeyDict["Brow_Raise_Outer_L".ToUpper()], MaxValue = 80f});

        AUShapeKeys[4].Add(new ShapeKey { Ind = _shapeKeyDict["Brow_Drop_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[4].Add(new ShapeKey { Ind = _shapeKeyDict["Brow_Drop_L".ToUpper()], MaxValue = 100f });


        AUShapeKeys[5].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Wide_L".ToUpper()], MaxValue = 100f });
        AUShapeKeys[5].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Wide_R".ToUpper()], MaxValue = 100f });

        AUShapeKeys[6].Add(new ShapeKey { Ind = _shapeKeyDict["Cheek_Raise_R".ToUpper()], MaxValue = 80f });
        AUShapeKeys[6].Add(new ShapeKey { Ind = _shapeKeyDict["Cheek_Raise_L".ToUpper()], MaxValue = 80f });


        AUShapeKeys[7].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Squint_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[7].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Squint_L".ToUpper()], MaxValue = 100f });

        AUShapeKeys[8].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Squint_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[8].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Squint_L".ToUpper()], MaxValue = 100f});



        AUShapeKeys[9].Add(new ShapeKey { Ind = _shapeKeyDict["Nose_Sneer_R".ToUpper()], MaxValue = 100f});
        AUShapeKeys[9].Add(new ShapeKey { Ind = _shapeKeyDict["Nose_Sneer_L".ToUpper()], MaxValue = 100f });
        AUShapeKeys[9].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Shrug_Upper".ToUpper()], MaxValue = 60f});

        AUShapeKeys[10].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Shrug_Upper".ToUpper()], MaxValue = 100f});
        

        AUShapeKeys[11].Add(new ShapeKey { Ind = _shapeKeyDict["Nose_Sneer_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[11].Add(new ShapeKey { Ind = _shapeKeyDict["Nose_Sneer_L".ToUpper()], MaxValue = 100f });

        AUShapeKeys[12].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Smile_R".ToUpper()], MaxValue = 80f});
        AUShapeKeys[12].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Smile_L".ToUpper()], MaxValue = 80f});


        //AUShapeKeys[13].Add(new ShapeKey{ Ind = _shapeKeyDict["V_Wide"], MaxValue = 80f});//??
        AUShapeKeys[13].Add(new ShapeKey { Ind = _shapeKeyDict["Cheek_Puff_R".ToUpper()], MaxValue = 80f});//??
        AUShapeKeys[13].Add(new ShapeKey { Ind = _shapeKeyDict["Cheek_Puff_L".ToUpper()], MaxValue = 80f});


        AUShapeKeys[14].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Dimple_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[14].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Dimple_L".ToUpper()], MaxValue = 100f });

        AUShapeKeys[15].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Frown_R".ToUpper()], MaxValue = 60});
        AUShapeKeys[15].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Frown_L".ToUpper()], MaxValue = 60f });

        AUShapeKeys[16].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Down_Lower_L".ToUpper()], MaxValue = 100f});
        AUShapeKeys[16].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Down_Lower_R".ToUpper()], MaxValue = 100f });



        AUShapeKeys[17].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Shrug_Lower".ToUpper().ToUpper()], MaxValue = 100f});



        AUShapeKeys[18].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Pucker_Up_L".ToUpper()], MaxValue = 100f });
        AUShapeKeys[18].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Pucker_Up_R".ToUpper()], MaxValue = 100f });


        AUShapeKeys[20].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Stretch_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[20].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Stretch_L".ToUpper()], MaxValue = 100f });

        AUShapeKeys[21].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Stretch_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[21].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Stretch_L".ToUpper()], MaxValue = 100f });

        
        AUShapeKeys[22].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Funnel_Up_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[22].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Funnel_Up_L".ToUpper()], MaxValue = 100f });

        
        AUShapeKeys[23].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Tighten_L".ToUpper()], MaxValue = 50f });
        AUShapeKeys[23].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Tighten_R".ToUpper()], MaxValue = 50f });



        AUShapeKeys[24].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Press_R".ToUpper()], MaxValue = 30f });
        AUShapeKeys[24].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Press_L".ToUpper()], MaxValue = 30f });

        AUShapeKeys[25].Add(new ShapeKey { Ind = _shapeKeyDict["IH"], MaxValue = 50f });//lip a bit parted

        AUShapeKeys[26].Add(new ShapeKey { Ind = _shapeKeyDict["Jaw_Open".ToUpper()], MaxValue = 10 }); //jaw a bit parted

        //AUShapeKeys[27].Add(new ShapeKey { Ind = _shapeKeyDict["V_Lip_Open"], MaxValue = 100f}); // jaw dropped
        AUShapeKeys[27].Add(new ShapeKey { Ind = _shapeKeyDict["Jaw_Open".ToUpper()], MaxValue = 20f }); // jaw dropped

        
        AUShapeKeys[28].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Roll_In_Upper_L".ToUpper()], MaxValue = 100f });
        AUShapeKeys[28].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Roll_In_Upper_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[28].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Roll_In_Lower_L".ToUpper()], MaxValue = 100f });
        AUShapeKeys[28].Add(new ShapeKey { Ind = _shapeKeyDict["Mouth_Roll_In_Lower_R".ToUpper()], MaxValue = 100f });


        AUShapeKeys[41].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_R".ToUpper()], MaxValue = 30f}); //lid droop
        AUShapeKeys[41].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_L".ToUpper()], MaxValue = 30f}); 

        AUShapeKeys[42].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_R".ToUpper()], MaxValue = 40f}); //slit
        AUShapeKeys[42].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_L".ToUpper()], MaxValue = 40f});

        AUShapeKeys[43].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_R".ToUpper()], MaxValue = 100f}); //eyes closed
        AUShapeKeys[43].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_L".ToUpper()], MaxValue = 100f});

        AUShapeKeys[44].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Squint_R".ToUpper()], MaxValue = 100f }); //squint
        AUShapeKeys[44].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Squint_L".ToUpper()], MaxValue = 100f });


        AUShapeKeys[45].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_R".ToUpper()], MaxValue = 100f}); //blink
        AUShapeKeys[45].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_L".ToUpper()], MaxValue = 100f});

        AUShapeKeys[46].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_R".ToUpper()], MaxValue = 100f}); //wink
        AUShapeKeys[46].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_Blink_L".ToUpper()], MaxValue = 30f });




        AUShapeKeys[51].Add(new ShapeKey { Ind = _shapeKeyDict["Head_Turn_L".ToUpper()], MaxValue = 30f });

        AUShapeKeys[52].Add(new ShapeKey { Ind = _shapeKeyDict["Head_Turn_R".ToUpper()], MaxValue = 30f });


        AUShapeKeys[53].Add(new ShapeKey { Ind = _shapeKeyDict["Head_Turn_Up".ToUpper()], MaxValue = 20f });

        AUShapeKeys[54].Add(new ShapeKey { Ind = _shapeKeyDict["Head_Turn_Down".ToUpper()], MaxValue = 20f });

        AUShapeKeys[55].Add(new ShapeKey { Ind = _shapeKeyDict["Head_Tilt_L".ToUpper()], MaxValue = 20f });

        AUShapeKeys[56].Add(new ShapeKey { Ind = _shapeKeyDict["Head_Tilt_R".ToUpper()], MaxValue = 20f });
        


        AUShapeKeys[57].Add(new ShapeKey { Ind = _shapeKeyDict["Head_Forward".ToUpper()], MaxValue = 10f });

        AUShapeKeys[58].Add(new ShapeKey { Ind = _shapeKeyDict["Head_Backward".ToUpper()], MaxValue = 10f });


        AUShapeKeys[61].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_L_Look_L".ToUpper()], MaxValue = 100f });
        AUShapeKeys[61].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_R_Look_L".ToUpper()], MaxValue = 100f });


        AUShapeKeys[62].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_L_Look_R".ToUpper()], MaxValue = 100f });
        AUShapeKeys[62].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_R_Look_R".ToUpper()], MaxValue = 100f });


        AUShapeKeys[63].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_L_Look_Up".ToUpper()], MaxValue = 100f });
        AUShapeKeys[63].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_R_Look_Up".ToUpper()], MaxValue = 100f });


        AUShapeKeys[64].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_L_Look_Down".ToUpper()], MaxValue = 100f });
        AUShapeKeys[64].Add(new ShapeKey { Ind = _shapeKeyDict["Eye_R_Look_Down".ToUpper()], MaxValue = 100f });


    }

    void ResetShapeKey(int ind) {

        ShapeKeyVals[ind] = 0f;
        ShapeKeyTargets[ind] = 0f;

        if(ind < _meshRendererBody.sharedMesh.blendShapeCount) //other keys don't have corresponding blendshapes 
           _meshRendererBody.SetBlendShapeWeight(ind, 0f);

        ResetShapeKeyRotation(ind);

        

    }

    void ResetShapeKeyRotation(int ind) {
        ShapeKeyVals[ind] = 0f;
        ShapeKeyTargets[ind] = 0f;

        _headTiltLeft = _headTiltRight = _headTurnDown = _headTurnUp = _headTurnLeft = _headTurnRight = 0;
        _headForward = _headBackward = 0;
        _eyeLookDown = _eyeLookUp = _eyeLookLeft = _eyeLookRight = 0;



        if(ind == _shapeKeyDict["Jaw_Open".ToUpper()] || ind == _shapeKeyDict["IH"]) { 
            _jawRot = _jawRotInit;
        }
        else if(ind >= _shapeKeyDict["Head_Forward".ToUpper()] && ind <= _shapeKeyDict["Head_Backward".ToUpper()]) {            
            _neckRot = _neckRotInit;
        }
        else if(ind >= _shapeKeyDict["Head_Turn_L".ToUpper()] && ind <= _shapeKeyDict["Head_Tilt_R".ToUpper()]) {            
            _headRot = _headRotInit;
            
        }
        else if(ind >= _shapeKeyDict["Eye_L_Look_L".ToUpper()] && ind <= _shapeKeyDict["Eye_R_Look_Down".ToUpper()]) {            
            _eyesRot[0] = _eyesRotInit[0];
            _eyesRot[1] = _eyesRotInit[1];
        }

    }


    static float CatmullRom(float p0, float p1, float p2, float p3, float t) {
        // standard Catmull–Rom spline
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    
    IEnumerator AnimateAllAUShapeKeys(ActionUnit au) {
        int i = au.currInd;
        int last = au.Intensities.Count - 1;
        int i0 = Mathf.Max(i - 1, 0), i1 = i, i2 = Mathf.Min(i + 1, last), i3 = Mathf.Min(i + 2, last);

        float v0 = au.Intensities[i0], v1 = au.Intensities[i1],
              v2 = au.Intensities[i2], v3 = au.Intensities[i3];

        // wait until this AU’s start time
        yield return new WaitUntil(() => Time.time - _startTimeAU >= au.Times[i1]);

        float duration = au.Times[i2] - au.Times[i1];
        float timeElapsed = 0f;
        float eyeCoef = 0.2f;
        


        while(timeElapsed < duration) {
            
            timeElapsed += Time.deltaTime;

            float t = Mathf.Clamp01(timeElapsed / duration);
            float percent = CatmullRom(v0, v1, v2, v3, t);
            float wPct = percent / 100f;

       
            foreach(ShapeKey sk in AUShapeKeys[au.AU]) {

                // Blend-shape
                float blendW = sk.MaxValue * wPct;
                _meshRendererBody.SetBlendShapeWeight(sk.Ind, blendW);

                //Rotation

                float startValue = sk.MaxValue * au.Intensities[au.currInd] / 100f;


                //ResetShapeKeyRotation(sk.Ind);

                ShapeKeyTargets[sk.Ind] = sk.MaxValue * au.Intensities[au.currInd + 1] / 100f;


                if(sk.Ind == _shapeKeyDict["Jaw_Open".ToUpper()]) {

                    Quaternion startJaw = _jawRotInit;
                    Quaternion targetJaw = _jawRotInit * Quaternion.Euler(0, 0, -ShapeKeyTargets[sk.Ind] * 0.1f);


                    _jawRot = Quaternion.Slerp(startJaw, targetJaw, blendW);
                }

                else if(sk.Ind == _shapeKeyDict["Head_Tilt_R".ToUpper()]) {
                    _headTiltRight = blendW;
                }

                else if(sk.Ind == _shapeKeyDict["Head_Tilt_L".ToUpper()]) {
                    _headTiltLeft = blendW;
                }

                else if(sk.Ind == _shapeKeyDict["Head_Turn_L".ToUpper()]) {
                    _headTurnLeft = blendW;
                }

                else if(sk.Ind == _shapeKeyDict["Head_Turn_R".ToUpper()]) {
                    _headTurnRight = blendW;
                }
               
                else if(sk.Ind == _shapeKeyDict["Head_Turn_Down".ToUpper()]) {
                    _headTurnDown = blendW;
                }
               
                else if(sk.Ind == _shapeKeyDict["Head_Turn_Up".ToUpper()]) {
                    _headTurnUp = blendW;
                }

                else if(sk.Ind == _shapeKeyDict["Head_Forward".ToUpper()]) {
                    _headForward = blendW;
                }

                else if(sk.Ind == _shapeKeyDict["Head_Backward".ToUpper()]) {
                    _headBackward = blendW;
                }

                else if(sk.Ind == _shapeKeyDict["Eye_L_Look_L".ToUpper()] || sk.Ind == _shapeKeyDict["Eye_R_Look_L".ToUpper()]) {
                    _eyeLookLeft =  blendW;
                }

                else if(sk.Ind == _shapeKeyDict["Eye_L_Look_R".ToUpper()] || sk.Ind == _shapeKeyDict["Eye_R_Look_R".ToUpper()]) {
                    _eyeLookRight =blendW;                    
                }

                else if(sk.Ind == _shapeKeyDict["Eye_L_Look_Up".ToUpper()] || sk.Ind == _shapeKeyDict["Eye_R_Look_Up".ToUpper()]) {
                    _eyeLookUp =  blendW;                   
                }

                else if(sk.Ind == _shapeKeyDict["Eye_L_Look_Down".ToUpper()] || sk.Ind == _shapeKeyDict["Eye_R_Look_Down".ToUpper()]) {
                    _eyeLookDown =  blendW;                   
                }

            }

            
            /////// HEAD /////////////////////
            float tiltAmount = _headTiltRight - _headTiltLeft;     // AU56 – AU55
            float turnAmount = _headTurnRight - _headTurnLeft;     // AU51 – AU52 
            float nodAmount = - _headTurnUp + _headTurnDown;     // AU53 – AU54


            
            Quaternion qRoll = Quaternion.AngleAxis(tiltAmount, Head.forward);  
            Quaternion qYaw = Quaternion.AngleAxis(turnAmount, Head.up);       
            Quaternion qPitch = Quaternion.AngleAxis(nodAmount, Head.right);   

            
            Quaternion targetRot = qYaw * qPitch * qRoll * _headRotInit;



            _headRot = targetRot; //Quaternion.Slerp(_headRot, targetRot, t);


            /////// EYES ////////////////
            
            float eyeSideAmount = _eyeLookRight - _eyeLookLeft;   
            float eyeUpAmount = _eyeLookUp - _eyeLookDown;   


            for(int j = 0; j < 2; ++j) {            
                Quaternion qYawEye = Quaternion.AngleAxis(eyeSideAmount * eyeCoef, Eyes[j].right);
                
                Quaternion qPitchEye = Quaternion.AngleAxis(-eyeUpAmount * eyeCoef, Eyes[j].up);
               
                Quaternion targetEyeRot = qYawEye * qPitchEye * _eyesRotInit[j];
                
                _eyesRot[j] = Quaternion.Slerp(_eyesRot[j], targetEyeRot, t);
            }

            yield return null;


        }

        
        
        // final snap to exact v2
        float finalPct = v2 / 100f;
        foreach(ShapeKey sk in AUShapeKeys[au.AU]) {
            _meshRendererBody.SetBlendShapeWeight(sk.Ind, sk.MaxValue * finalPct);
        }

  
    }



    //Animate all the shape keys  related to viseme for the given period
    IEnumerator AnimateAllVisemeShapeKeys(Viseme v) {

        
        float duration = v.Times[v.currInd + 1] - v.Times[v.currInd]; //duration is the same for all keys related to au

        
        float timeElapsed = 0f;

        

        //Wait until the viseme's turn for animation comes
        float deltaTime = Time.time - _startTimeViseme;
        while(deltaTime < v.Times[v.currInd]) {
            deltaTime = Time.time - _startTimeViseme;
            yield return null;
        }


        while(timeElapsed < duration) {
            CurrentViseme = v;

            float t = timeElapsed / duration;

            foreach(ShapeKey sk in VisemeShapeKeys[VisemeDict[v.viseme]]) {
                ResetShapeKey(sk.Ind);

                float startValue = sk.MaxValue * v.Intensities[v.currInd] / 100f;
                ShapeKeyTargets[sk.Ind] = sk.MaxValue * v.Intensities[v.currInd + 1] / 100f;

                ShapeKeyVals[sk.Ind] = Mathf.Lerp(startValue, ShapeKeyTargets[sk.Ind], t);
                
                if(sk.Ind == _shapeKeyDict["IH"]) {
                    Quaternion startJaw = _jawRotInit;
                    Quaternion targetJaw = _jawRotInit * Quaternion.Euler(0, 0, -ShapeKeyTargets[sk.Ind]*0.1f);                    
                    _jawRot = Quaternion.Slerp(startJaw, targetJaw, t);
                    
                }


                if(sk.Ind < ShapeKeyCntBody) 
                    _meshRendererBody.SetBlendShapeWeight(sk.Ind, ShapeKeyVals[sk.Ind]);
                else if(sk.Ind < ShapeKeyCntBody+ShapeKeyCntTongue)
                    _meshRendererTongue.SetBlendShapeWeight(sk.Ind-ShapeKeyCntBody, ShapeKeyVals[sk.Ind]);

                
                timeElapsed += Time.deltaTime;

                
                yield return null;

            }
        }


        //We should assign values for 1
        //To reset the keys if t cannot make it to 1 because of deltaTime being bigger than viseme duration
        foreach (ShapeKey sk in VisemeShapeKeys[VisemeDict[v.viseme]]) {
            ResetShapeKey(sk.Ind);
            _jawRot = _jawRotInit;
        }


    }


    public void LateUpdate() {
    //Jaw and head must be updated here
        Jaw.localRotation = _jawRot;
        Head.localRotation = _headRot;
        Neck.localRotation = _neckRot;
        Eyes[0].localRotation = _eyesRot[0];
        Eyes[1].localRotation = _eyesRot[1];


        
    }


    IEnumerator AnimateAU(ActionUnit au) {

        au.currInd = 0;


        while(au.currInd < au.Times.Count() - 1) {
            
            yield return StartCoroutine(AnimateAllAUShapeKeys(au));

            
            au.currInd += 1;

            if (au.currInd >= au.Times.Count() - 1)
            {

                _isTalking = false;
            }
                

        }
       
    }

    IEnumerator AnimateViseme(Viseme v) {

        v.currInd = 0;

        while(v.currInd < v.Times.Count() - 1) {

            
            yield return StartCoroutine(AnimateAllVisemeShapeKeys(v));
            


            v.currInd += 1;

        

    }

    }
    void AnimateAllAUs() {
        
        _startTimeAU = Time.time;
        
        foreach(ActionUnit au in AUList)            
            StartCoroutine(AnimateAU(au));
    }

    void Speak(string voice, int rate,  string text) {
        
        string cmdArgs = string.Format(" -v {0} -r {1} \"{2}\"", voice, rate, text.Replace("\"", ","));
        UnityEngine.Debug.Log(cmdArgs);

        Process speechProcess = Process.Start("/usr/bin/say", cmdArgs);
        _audioSource.Play();

}

    void AnimateAllVisemes() {
        
        _startTimeViseme = Time.time;


        if(IsSpeechEnabled)
            Speak(Voice, Wpm, Speech);


        foreach(Viseme v in VisemeList) {
            
            StartCoroutine(AnimateViseme(v));
        }

    }

    public void ResetShapeKeys() {
        for(int i = 0; i < ShapeKeyCntBody + ShapeKeyCntTongue; i++) { 
            ResetShapeKey(i);
            

        }

    }
    public void PlayAnimation() {

        //Call these once for aus + visemes - they have mutually exclusive shape keys
        ResetShapeKeys();
        StopAllCoroutines();

        _isTalking = true;
        if (AUsOn)
            AnimateAllAUs();

        if(VisemesOn)
            AnimateAllVisemes();

    }


    public void AUResponseCb(string response) {
        //Debug.Log("Response text is " + response);
        IsWaitingResponse = false;

        
        (AUList, VisemeList, Utterance, Speech, _personality, Duration) = Parsers.ParseJson(response);

        
        
        //UnityEngine.Debug.Log("response received");
        PlayAnimation();

    }


    public void GetAUsAndDuration(string response) {
        
        (AUList, Duration) = Parsers.ParseAU(response);


        UnityEngine.Debug.Log(Duration);
        

    }


   
        
    
    public string AUListToString() {
        string auStr = "AU\tSemantics\tTimes\tIntensities\n";
        foreach(ActionUnit au in AUList) {
            auStr += $"{au.AU}\t{au.Semantics}\t[{string.Join(", ", au.Times)}]\t[{string.Join(", ", au.Intensities)}]\n";


        }
        return auStr;
    }

    public string VisemeListToString() {
        string vStr = "Viseme\tTimes\tIntensities\n";
        foreach(Viseme v in VisemeList) {
            vStr += $"{v.viseme}\t{v.Semantics}\t[{string.Join(", ", v.Times)}]\t[{string.Join(", ", v.Intensities)}]\n";


        }
        return vStr;
    }

    

}
