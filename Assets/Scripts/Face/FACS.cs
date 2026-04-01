
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using System.IO;
using System.Linq;

using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine;
using UnityEditor.Search;

[Serializable]
public class ActionUnit{
    
    public int AU { get; set; }    
    public List<float> Times { get; set; }
    public List<float> Intensities { get; set; }
    public List<float> InitialIntensities { get; set; }
    public int currInd { get; set; }
    public float suppressionFactor; //if 0 no suppression, if 1 fully suppressed at the currInd
    public string Semantics { get; set; }
}

public class ShapeKey {    
    public int Ind { get; set; }    
    public float MaxValue { get; set; } ///This is specific to the model's shape keys
}



//Ee Er IH Ah Oh W_OO S_Z Ch_J F_V TH T_L_D_N B_M_P K_G_H_NG AE R
// public enum VisemeEnum
// {
//     EE,
//     ER,
//     IH,
//     AH,
//     OH,
//     W_OO,
//     S_Z,
//     CH_J,
//     F_V,
//     TH,
//     T_L_D_N,
//     B_M_P,
//     K_G_H_NG,
//     AE,
//     R,
//     sil
// };



public class FACS : MonoBehaviour
{
//     public static Dictionary<string, int> VisemeDict = new Dictionary<string, int> {
//     {"sil", -1},
//     {"EE", 0},
//     {"ER", 1},
//     {"IH", 2},
//     {"AH", 3},
//     {"OH", 4},
//     {"W_OO", 5},
//     {"S_Z", 6},
//     {"CH_J", 7},
//     {"F_V", 8},
//     {"TH", 9},
//     {"T_L_D_N", 10},
//     {"B_M_P", 11},
//     {"K_G_H_NG", 12},
//     {"AE", 13},
//     {"R", 14},
    
// };
    public static Dictionary<int, string> VisemeDict = new Dictionary<int, string> {
    {-1, "sil"},
    {0, "EE"},
    {1, "ER"},
    {2, "IH"},
    {3, "AH"},
    {4, "OH"},
    {5, "W_OO"},
    {6, "S_Z"},
    {7, "CH_J"},
    {8, "F_V"},
    {9, "TH"},
    {10, "T_L_D_N"},
    {11, "B_M_P"},
    {12, "K_G_H_NG"},
    {13, "AE"},
    {14, "R"}};
    //Visemeinds directly correspond to the keys in the visemeDict. 
    
    public bool IsSpeechEnabled = false;
    public int TestAUInd = 0;
    public SkinnedMeshRenderer _meshRendererBody;
    private SkinnedMeshRenderer _meshRendererTongue;
    public int ShapeKeyCntBody, ShapeKeyCntTongue;

    

    public Text SpeechBubble;

    
    public float [] ShapeKeyVals;
    public float[] ShapeKeyTargets;
    private string _speech;
    public string Speech{
        set {
            _speech = value;
            SpeechBubble.text = _speech;

        }
        get {        
            return SpeechBubble.text;
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
    

   
   

    [SerializeField]
    private float _startTimeAU;
    


    public List<ActionUnit> AUList;
    

    public string Voice = "Alex";
    public int Wpm = 175;

    public float Duration;

    public bool AUsOn = true;

    public string ActiveVisemeName;


    Dictionary<string, int> _shapeKeyDict = new Dictionary<string, int>();
    

    public AudioClip SpeechClip {
        
        set {
            _audioSource.clip = value;
        }
    }
    AudioSource _audioSource;

    public TMP_InputField UtteranceTMP;

    float [] _visemeWeight = new float[15];
    //Dictionary<string, int> _shapeKeyDictTongue = new Dictionary<string, int>();
    


    public bool IsWaitingResponse = false;


    //RHUBARB
    [Header("Visemes")]
    public TextAsset visemeJsonFile; // If using JSON
    [TextArea(5, 10)]
    public string rawVisemeData;    // If pasting the text list directly


    // private struct VisemeFrame
    // {
    //     public float time;
    //     public VisemeEnum viseme;
    // }
    private List<VisemeFrame> _visemeFrames = new List<VisemeFrame>();
    public float VisemeSmoothSpeed = 20f; // Higher is faster/snappier, lower is smoother/lazier

    
    private void Awake() {
        _jawRotInit = _jawRot = Jaw.localRotation;
        _headRotInit = _headRot = Head.localRotation;       
        _neckRotInit = _neckRot = Neck.localRotation;


        _eyesRotInit[0] = _eyesRot[0] = Eyes[0].localRotation;
        _eyesRotInit[1] = _eyesRot[1] = Eyes[1].localRotation;
        
        _audioSource = GetComponent<AudioSource>();
    }
    // Start is called before the first frame update
    void Start() {

        AUShapeKeys = new List<ShapeKey>[66];
        

        _meshRendererBody = transform.Find("CC_Base_Body").GetComponent<SkinnedMeshRenderer>();
        _meshRendererTongue = transform.Find("CC_Base_Tongue").GetComponent<SkinnedMeshRenderer>();

    
        InitShapeKeysAndAUs();
    
        
        ShapeKeyVals = new float[ShapeKeyCntBody+ShapeKeyCntTongue];
        ShapeKeyTargets = new float[ShapeKeyCntBody + ShapeKeyCntTongue];


        AUList = new List<ActionUnit>();

        
        _audioSource = gameObject.GetComponent<AudioSource>();


        //ParseVisemeText();
        _visemeFrames = Parsers.ParseVisemes(rawVisemeData);
        
        
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

    float GetSuppression(int auInd, string visemeName)
    {
        
        if(visemeName.ToUpper().Equals("SIL"))
            return 0f; 
        //Check how much the a viseme should suppress the an AU
        int visemeInd = VisemeDict.FirstOrDefault(x => x.Value == visemeName).Key;
        float wt = _visemeWeight[visemeInd];
        
        if(visemeName.Equals("B_M_P"))
        {
            int[] conflictingAUs = { 9, 10, 15, 16, 22 };
            if (conflictingAUs.Contains(auInd)){
                return 1f;
            }

        }
        else if (visemeName.Equals("EE"))
        {
            int[] conflictingAUs = { 27 };
            if (conflictingAUs.Contains(auInd))
                return 1f;

            if (new int[] { 20, 21 }.Contains(auInd))
                return wt;

        }

        else if (visemeName.Equals("AH") || visemeName.Equals("AE"))
        {
            int[] conflictingAUs = { 18, 22, 23 };

            if (conflictingAUs.Contains(auInd))
                return wt;


        }
        else if (visemeName.Equals("W_OO") || visemeName.Equals("OH"))
        {
            int[] conflictingAUs = { 18, 22, 23, 12 };

            if (conflictingAUs.Contains(auInd))
                return wt;
        }

        else if (visemeName.Equals("F_V") || visemeName.Equals("S_Z"))
        {
            int[] conflictingAUs = { 18 };

            if (conflictingAUs.Contains(auInd))
                return wt;
        }
        else if (visemeName.Equals("F_V"))
        {
            int[] conflictingAUs = { 16, 17, 18 };

            if (conflictingAUs.Contains(auInd))
                return wt;
        }

        else if (visemeName.Equals("S_Z"))
        {
            int[] conflictingAUs = { 18 };

            if (conflictingAUs.Contains(auInd))
                return wt;
        }
        
        
        if(auInd == 12){ //Smilesuppressed in all cases
            // Debug.Log(VisemeDict.FirstOrDefault(x => x.Value == visemeInd).Key + " " +wt);
            return 0.5f; //TODO
        }
    
        return 0f;
    }

    void UpdateAUIntensitiesBySpeech()
    {
        foreach (ActionUnit au in AUList)
        {
            // We iterate through segments: [i] to [i+1]
            for (int i = 0; i < au.Times.Count - 1; i++)
            {
                float auStart = au.Times[i];
                float auEnd = au.Times[i + 1];
                float maxSuppression = 0f;

                // Find all visemes that overlap with this AU interval
                for (int v = 0; v < _visemeFrames.Count; v++)
                {
                    float vStart = _visemeFrames[v].time;
                    // If it's the last frame, assume it lasts indefinitely or to a set duration
                    float vEnd = (v < _visemeFrames.Count - 1) ? _visemeFrames[v + 1].time : float.MaxValue;

                    // Check for interval overlap
                    if (Mathf.Max(auStart, vStart) < Mathf.Min(auEnd, vEnd))
                    {
                    
                    
                    
                    
                        float factor = GetSuppression(au.AU, _visemeFrames[v].viseme);
                        
                        // If multiple visemes overlap one AU segment, 
                        // we usually take the strongest suppression
                        if (factor > maxSuppression) maxSuppression = factor;
                    }
                }

                // Apply suppression to the segment start point
                au.Intensities[i] = au.InitialIntensities[i] * (1 - maxSuppression);
            }
        }
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
        
        //Should update AU intensities
            
         
        while(timeElapsed < duration) {
        
            float suppressionFactor = GetSuppression(au.AU, ActiveVisemeName);

            
            timeElapsed += Time.deltaTime;
            
            //Check if current AU needs to be suppressed
            
    
            float t = Mathf.Clamp01(timeElapsed / duration);
            float percent = CatmullRom(v0, v1, v2, v3, t);
            float wPct = percent / 100f;

        
            //if(au.AU==12)
              //  Debug.Log(VisemeDict.FirstOrDefault(x => x.Value == ActiveVisemeInd).Key + " " +au.AU + " " + v0 + " " +v1 + " " +v2 + " " +v3  +  " " + percent);    
            
            foreach (ShapeKey sk in AUShapeKeys[au.AU])
            {


                // Blend-shape
                float blendW = sk.MaxValue * wPct;



                _meshRendererBody.SetBlendShapeWeight(sk.Ind, blendW);


                //Rotation

                float startValue = sk.MaxValue * au.Intensities[au.currInd] / 100f;


                //ResetShapeKeyRotation(sk.Ind);

                ShapeKeyTargets[sk.Ind] = sk.MaxValue * au.Intensities[au.currInd + 1] / 100f;


                if (sk.Ind == _shapeKeyDict["Jaw_Open".ToUpper()])
                {

                    Quaternion startJaw = _jawRotInit;
                    Quaternion targetJaw = _jawRotInit * Quaternion.Euler(0, 0, -ShapeKeyTargets[sk.Ind] * 0.1f);


                    
                    if (ActiveVisemeName.Equals("FV") || ActiveVisemeName.Equals("B_M_P") || ActiveVisemeName.Equals("CH_J") || ActiveVisemeName.Equals("S_Z"))
                        targetJaw = _jawRotInit; // no update
                    
                    _jawRot = Quaternion.Slerp(startJaw, targetJaw, blendW);
                }

                else if (sk.Ind == _shapeKeyDict["Head_Tilt_R".ToUpper()])
                {
                    _headTiltRight = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Head_Tilt_L".ToUpper()])
                {
                    _headTiltLeft = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Head_Turn_L".ToUpper()])
                {
                    _headTurnLeft = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Head_Turn_R".ToUpper()])
                {
                    _headTurnRight = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Head_Turn_Down".ToUpper()])
                {
                    _headTurnDown = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Head_Turn_Up".ToUpper()])
                {
                    _headTurnUp = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Head_Forward".ToUpper()])
                {
                    _headForward = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Head_Backward".ToUpper()])
                {
                    _headBackward = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Eye_L_Look_L".ToUpper()] || sk.Ind == _shapeKeyDict["Eye_R_Look_L".ToUpper()])
                {
                    _eyeLookLeft = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Eye_L_Look_R".ToUpper()] || sk.Ind == _shapeKeyDict["Eye_R_Look_R".ToUpper()])
                {
                    _eyeLookRight = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Eye_L_Look_Up".ToUpper()] || sk.Ind == _shapeKeyDict["Eye_R_Look_Up".ToUpper()])
                {
                    _eyeLookUp = blendW;
                }

                else if (sk.Ind == _shapeKeyDict["Eye_L_Look_Down".ToUpper()] || sk.Ind == _shapeKeyDict["Eye_R_Look_Down".ToUpper()])
                {
                    _eyeLookDown = blendW;
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


            //TODO: why was this open?
            //_headRot = targetRot; //Quaternion.Slerp(_headRot, targetRot, t);
            _headRot = Quaternion.Slerp(_headRot, targetRot, t);

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
        
        float finalPct = au.Intensities[i2] / 100f;
        foreach(ShapeKey sk in AUShapeKeys[au.AU]) {
            _meshRendererBody.SetBlendShapeWeight(sk.Ind, sk.MaxValue * finalPct);
        }

  
    }


    void GetCurrentNormalizedVisemeWeights()
    {
            for(int i = 0; i < _visemeWeight.Length; i++) //this also includes sil
                _visemeWeight[i]  = _meshRendererBody.GetBlendShapeWeight(i) / 100f;
        
    }


    public void LateUpdate()
    {
        //Jaw and head must be updated here
        Jaw.localRotation = _jawRot;
        Head.localRotation = _headRot;
        Neck.localRotation = _neckRot;
        Eyes[0].localRotation = _eyesRot[0];
        Eyes[1].localRotation = _eyesRot[1];

        int aeInd = VisemeDict.FirstOrDefault(x => x.Value == "AE").Key;
        int ahInd = VisemeDict.FirstOrDefault(x => x.Value == "AH").Key;
        int ohInd = VisemeDict.FirstOrDefault(x => x.Value == "OH").Key;
        int wOOInd = VisemeDict.FirstOrDefault(x => x.Value == "W_OO").Key;
        int thInd = VisemeDict.FirstOrDefault(x => x.Value == "TH").Key;
        int ihInd = VisemeDict.FirstOrDefault(x => x.Value == "IH").Key;
        int eeInd = VisemeDict.FirstOrDefault(x => x.Value == "EE").Key;
        int kghngInd = VisemeDict.FirstOrDefault(x => x.Value == "K_G_H_NG").Key;
        int rInd = VisemeDict.FirstOrDefault(x => x.Value == "R").Key;  
        GetCurrentNormalizedVisemeWeights();
        
        //TODO
        // Jaw positions
        float jawOpen = Mathf.Max(_visemeWeight[aeInd], _visemeWeight[ahInd], _visemeWeight[ohInd] * 0.8f,
         _visemeWeight[wOOInd] * 0.6f, _visemeWeight[thInd] * 0.2f,
          _visemeWeight[ihInd] * 0.2f, _visemeWeight[eeInd] * 0.2f,
           _visemeWeight[kghngInd] * 0.2f, _visemeWeight[rInd] * 0.2f);


        if (jawOpen > 0.05)
        { //it means visemes are working, so they take over other blendshapes
            // Get jaw rotation from the blendshape weight
            float jawAngleInc = Mathf.Lerp(0, 7, jawOpen);

            _jawRot = _jawRotInit * Quaternion.Euler(0, 0, -jawAngleInc);
        }

      //  if (_visemeWeight[(int)VisemeEnum.F_V] > 0.05f || _visemeWeight[(int)VisemeEnum.B_M_P] > 0.05f || _visemeWeight[(int)VisemeEnum.CH_J] > 0.05f || _visemeWeight[(int)VisemeEnum.S_Z] > 0.05f)
        //    _jawRot = _jawRotInit; //don't open the jaw


    }



    IEnumerator AnimateAU(ActionUnit au) {

        au.currInd = 0;


        while(au.currInd < au.Times.Count() - 1) {
            
            yield return StartCoroutine(AnimateAllAUShapeKeys(au));
            
            au.currInd += 1;

        }
       
    }

    
    void AnimateAllAUs() {
        
        _startTimeAU = Time.time;
        
        foreach(ActionUnit au in AUList)            
            StartCoroutine(AnimateAU(au));
    }

    
private IEnumerator GenerateAndPlaySpeech(string text)
    {
        // Define a temporary path to save the generated audio file
        // string fileName = "temp_speech.wav";
        // string filePath = Path.Combine(Application.temporaryCachePath, fileName);

        // UnityEngine.Debug.Log(filePath);
        // // string filePath = "Assets/Crazy Minnow Studio/Examples/Audio/Promo-male.mp3";

        // // Configure the macOS 'say' command process
        // Process process = new Process();
        // process.StartInfo.FileName = "say";
        
        // // Escape quotes in the text to prevent command line injection/errors
        string safeText = text.Replace("\"", "\\\"");

        // // -o outputs to a file. 
        // // --data-format=LEF32@44100 forces a 32-bit float WAV file at 44.1kHz, which Unity reads flawlessly.
        // process.StartInfo.Arguments = $"-o \"{filePath}\" --data-format=LEF32@44100 \"{safeText}\"";
        // process.StartInfo.UseShellExecute = false;
        // process.StartInfo.CreateNoWindow = true;

        // // Start the synthesis
        // process.Start();

        string filePath = Path.Combine(Application.dataPath, "out.wav");
    
    
    if (File.Exists(filePath))
        File.Delete(filePath);

    

    string cmdArgs = $"-o \"{filePath}\" --data-format=LEF32@44100 \"{safeText}\"";

    System.Diagnostics.Process process = System.Diagnostics.Process.Start("/usr/bin/say", cmdArgs);

    if (process == null)
    {
        UnityEngine.Debug.LogError("Failed to start /usr/bin/say");
        yield break;
    }

    while (!process.HasExited)
        yield return null;

    if (!File.Exists(filePath))
    {
        Debug.LogError("say finished, but no audio file was created: " + filePath);
        yield break;
    }

    var info = new FileInfo(filePath);
    if (info.Length == 0)
    {
        Debug.LogError("Audio file was created but is empty: " + filePath);
        yield break;
    }

    string uri = "file://" + filePath;


        // Wait for the OS to finish writing the audio file
        while (!process.HasExited)
        {
            yield return null;
        }

        // Load the generated file into Unity
        
       using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV);
    yield return www.SendWebRequest();

    if (www.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError("Error loading synthesized speech: " + www.error);
        yield break;
    }

    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

    if (clip == null)
    {
        Debug.LogError("Loaded clip is null.");
        yield break;
    }

    
    
        // GetComponent<OVRLipSyncContextBase>().audioSource.clip = clip;
        // GetComponent<OVRLipSyncContextBase>().audioSource.Play();
    
    }
    
    public void ResetAUIntensities()
    {
       foreach (ActionUnit au in AUList){
            if (au.Intensities != null) {        
                au.InitialIntensities = new List<float>(au.Intensities);
            }
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
        ResetAUIntensities();
        
        StopAllCoroutines();


        

        if (IsSpeechEnabled)
        {
            UpdateAUIntensitiesBySpeech();
            _audioSource.Play();
            StartCoroutine(PlayVisemeSequence()); // Starts in the same frame as animating AUs
        }
        
        if (AUsOn)
            AnimateAllAUs();

            
        
        // GetComponent<OVRLipSyncContextBase>().audioSource.Play() ;
        
     
    }
    
    


    public void AUResponseCb(string response) {
        //Debug.Log("Response text is " + response);
        IsWaitingResponse = false;

        
        (AUList,  Utterance,  Duration) = Parsers.ParseJson(response);
        
        
        //UnityEngine.Debug.Log("response received");
        PlayAnimation();

    }


    public void GetAUsAndDuration(string response) {
        
        (AUList, Duration) = Parsers.ParseAU(response);

        

        

    }

    public string AUListToString() {
        string auStr = "AU\tSemantics\tTimes\tIntensities\n";
        foreach(ActionUnit au in AUList) {
            auStr += $"{au.AU}\t{au.Semantics}\t[{string.Join(", ", au.Times)}]\t[{string.Join(", ", au.Intensities)}]\n";


        }
        return auStr;
    }


    // void ParseVisemeText() {
    //     _visemeFrames.Clear();
    //     string[] lines = rawVisemeData.Split('\n');
    //     foreach (string line in lines) {
    //         string[] parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    //         if (parts.Length >= 2 && float.TryParse(parts[0], out float t) && Enum.TryParse(parts[1], true, out VisemeEnum v)) {
    //             _visemeFrames.Add(new VisemeFrame { time = t, viseme = v });
                
    //         }
    //     }
    // }


    IEnumerator PlayVisemeSequence() {
        if (_visemeFrames.Count == 0)
            Parsers.ParseVisemes(rawVisemeData);
            //ParseVisemeText();
        
        int frameIndex = 0;
        while (_audioSource.isPlaying) {
            float currentTime = _audioSource.time;
            
            // Find current frame based on audio time
            while (frameIndex < _visemeFrames.Count - 1 && currentTime >= _visemeFrames[frameIndex + 1].time) {
                frameIndex++;
            }

            ActiveVisemeName = _visemeFrames[frameIndex].viseme.ToUpper();
            int activeVisemeInd = VisemeDict.FirstOrDefault(x => x.Value == ActiveVisemeName).Key;
            // Smoothly transition ALL viseme weights
            for (int i = 0; i < _visemeWeight.Length; i++) {
                float target = (i == activeVisemeInd) ? 1.0f : 0.0f;
                
                // MoveTowards provides a consistent linear transition (better for speech "snaps")
                // Use Mathf.Lerp if you want a more "organic/lazy" feel
                _visemeWeight[i] = Mathf.MoveTowards(_visemeWeight[i], target, Time.deltaTime * VisemeSmoothSpeed);
                
                _meshRendererBody.SetBlendShapeWeight(i, _visemeWeight[i] * 100f);
            }

            // Apply these smoothed weights to the Actual Blendshapes
            // ApplyVisemeWeightsToMesh();


            yield return null;
        }

        // Return to neutral smoothly when audio stops
        float transitionReset = 0;
        while (transitionReset < 1.0f) {
            transitionReset += Time.deltaTime * VisemeSmoothSpeed;
            for (int i = 0; i < _visemeWeight.Length; i++) {
                _visemeWeight[i] = Mathf.MoveTowards(_visemeWeight[i], 0, Time.deltaTime * VisemeSmoothSpeed);
                _meshRendererBody.SetBlendShapeWeight(i, _visemeWeight[i] * 100f);
            }
            
            
            // ApplyVisemeWeightsToMesh();
            yield return null;
        }
    }

    // void ApplyVisemeWeightsToMesh() {
    //     // Maps your VisemeEnum to the actual CC4 indices in _shapeKeyDict
    //     foreach (var pair in VisemeDict) {
    //         if (_shapeKeyDict[pair.Key].TryGetValue(pair.Key, out int meshIndex)) {
    //             if(pair.Value>=0)
    //                 _meshRendererBody.SetBlendShapeWeight(meshIndex, _visemeWeight[pair.Value] * 100f);
    //         }
    //     }
    // }    

}
