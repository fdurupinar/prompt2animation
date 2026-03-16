using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public static class Parsers {

    

    [Serializable]
    public class FacialData {
        public string text;
        public string speech;
        public Personality personality;        
        public List<ActionUnit> facial_actions;
        public List<Viseme> visemes;
        public float duration;
    }



    public static Dictionary<int, string> AUSemanticsDict =  new Dictionary<int, string> {
      { 1, "Inner Brow Raiser" },
        { 2, "Outer Brow Raiser" },
        { 4, "Brow Lowerer" },
        { 5, "Upper Lid Raiser" },
        { 6, "Cheek Raiser" },
        { 7, "Lid Tightener" },
        { 9, "Nose Wrinkler" },
        { 10, "Upper Lip Raiser" },
        { 12, "Lip Corner Puller" },
        { 14, "Dimpler" },
        { 15, "Lip Corner Depressor" },
        { 17, "Chin Raiser" },
        { 20, "Lip Stretcher" },
        { 23, "Lip Tightener" },
        { 24, "Lip Pressor" },
        { 25, "Lips Part" },
        { 26, "Jaw Drop" },
        { 27, "Mouth Stretch" },
        { 28, "Lip Suck" },
        { 29, "Jaw Thrust" },
        { 30, "Jaw Sideways" },
        { 31, "Jaw Clencher" },
        { 32, "Bite" },
        { 33, "Cheek Blow" },
        { 34, "Cheek Puff" },
        { 35, "Cheek Suck" },
        { 36, "Tongue Bulge" },
        { 37, "Lip Wipe" },
        { 38, "Nostril Dilator" },
        { 39, "Nostril Compressor" },
        { 41, "Glabella Lowerer" },
        { 42, "Nasal Root Compressor" },
        { 43, "Eyes Closed" },
        { 44, "Squint" },
        { 45, "Blink" },
        { 46, "Wink" },
        { 51, "Head Turn Left" },
        { 52, "Head Turn Right" },
        { 53, "Head Up" },
        { 54, "Head Down" },
        { 55, "Head Tilt Left" },
        { 56, "Head Tilt Right" },
        { 57, "Head Forward" },
        { 58, "Head Backward" },
        { 61, "Eyes Turn Left" },
        { 62, "Eyes Turn Right" },
        { 63, "Eyes Up" },
        { 64, "Eyes Down" }
    };



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

    public static (List<ActionUnit>, List<Viseme>, string, string, Personality, float) ParseJson(string json) {

        FacialData data = JsonConvert.DeserializeObject<FacialData>(json);

        
        //foreach(var au in data.facial_actions) {
            
        //    Debug.Log($"AU {au.AU} | Times: [{string.Join(", ", au.Times)}] | Intensities: [{string.Join(", ", au.Intensities)}]");
        //}



        //update viseme times

        if(data.visemes!=null) {
            for(int i = 0; i < data.visemes.Count; i++) {

                data.visemes[i].viseme = data.visemes[i].viseme.ToUpper();
                float[] updatedIntensities = new float[2];

                if(i == 0) {
                    updatedIntensities[0] = 0;
                    updatedIntensities[1] = 100;
                }
                else if(i == data.visemes.Count - 1) {
                    updatedIntensities[0] = 50;
                    updatedIntensities[1] = 0;
                }
                else {
                    updatedIntensities[0] = 50;
                    updatedIntensities[1] = 100;
                }
                data.visemes[i].Intensities[0] = updatedIntensities[0];
                data.visemes[i].Intensities[1] = updatedIntensities[1];
            }
        }


    

        return (data.facial_actions, data.visemes, data.text, data.speech, data.personality, data.duration);
    }

    public static (List<ActionUnit>, float) ParseAU(string json) {

        FacialData data = JsonConvert.DeserializeObject<FacialData>(json);

        Debug.Log($"Utterance: {data.text}");
        foreach(var au in data.facial_actions) {
            Debug.Log($"AU {au.AU} | Times: [{string.Join(", ", au.Times)}] | Intensities: [{string.Join(", ", au.Intensities)}]");
        }


        return (data.facial_actions, data.duration);
    }


    


  

    
   


    }