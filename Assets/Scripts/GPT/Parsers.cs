using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text.RegularExpressions;
[Serializable]
public class VisemeFrame
{
    [JsonProperty("time")]
    public float time { get; set; }

    [JsonProperty("viseme")]    
    public string viseme { get; set; }
}
    
public static class Parsers {

    

    [Serializable]
    public class FacialData {
        public string utterance;

        public List<ActionUnit> facial_actions;        
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




    public static (List<ActionUnit>, string,  float) ParseJson(string json) {

        FacialData data = JsonConvert.DeserializeObject<FacialData>(json);

        

        return (data.facial_actions,  data.utterance,  data.duration);
    }

    public static (List<ActionUnit>, float) ParseAU(string json) {

        FacialData data = JsonConvert.DeserializeObject<FacialData>(json);

        Debug.Log($"Utterance: {data.utterance}");
        foreach(var au in data.facial_actions) {
            Debug.Log($"AU {au.AU} | Times: [{string.Join(", ", au.Times)}] | Intensities: [{string.Join(", ", au.Intensities)}]");
        }


        return (data.facial_actions, data.duration);
    }

    public static void WriteAUs(string path, List<ActionUnit> aus) {
        FacialData data = new FacialData {
            facial_actions = aus
        };

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        System.IO.File.WriteAllText(path + ".json", json);
    }
    
    public static List<VisemeFrame> ParseVisemes(string visemeJson) {
    

        try {
            // Deserializes the entire array directly into your list
            return JsonConvert.DeserializeObject<List<VisemeFrame>>(visemeJson);
        }
        catch (JsonException e) {
            // Handle malformed JSON here
            Debug.LogError($"Failed to parse viseme JSON: {e.Message}");
            return null;
                
        }
    }

  

    
   


    }