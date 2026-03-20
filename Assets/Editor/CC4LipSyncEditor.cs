using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(OVRLipSyncContextMorphTarget))]
public class CC4LipSyncEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the original Inspector fields first
        DrawDefaultInspector();

        OVRLipSyncContextMorphTarget script = (OVRLipSyncContextMorphTarget)target;

        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f); // Light green button
        
        if (GUILayout.Button("Auto-Map CC4 Visemes", GUILayout.Height(30)))
        {
            MapVisemes(script);
        }
        
        GUI.backgroundColor = Color.white;
    }

    private void MapVisemes(OVRLipSyncContextMorphTarget morphTargetScript)
    {
        SkinnedMeshRenderer smr = morphTargetScript.skinnedMeshRenderer;
        if (smr == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Skinned Mesh Renderer first!", "OK");
            return;
        }

        Mesh mesh = smr.sharedMesh;
        Undo.RecordObject(morphTargetScript, "Auto Map CC4 Visemes");

       Dictionary<int, string> mapping = new Dictionary<int, string>
        {
            
            { 1, "B_M_P" },      // PP
            { 2, "F_V" },        // FF
            { 3, "TH" },         // TH
            { 4, "T_L_D_N" },    // DD
            { 5, "K_G_H_NG" },   // kk
            { 6, "Ch_J" },       // CH
            { 7, "S_Z" },        // SS
            { 8, "T_L_D_N" },    // nn
            { 9, "R" },          // RR (Could also be 'Er')
            { 10, "Ah" },        // aa
            { 11, "EE" },        // E
            { 12, "IH" },        // ih
            { 13, "Oh" },        // oh
            { 14, "W_OO" }       // ou
        };

        morphTargetScript.visemeToBlendTargets[0] = -1; //sil
        int count = 0;
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string shapeName = mesh.GetBlendShapeName(i);
            foreach (var entry in mapping)
            {
                if (shapeName.Equals(entry.Value))
                {
                    Debug.Log($"Mapping viseme {entry.Key} to blendshape {shapeName}");
                    morphTargetScript.visemeToBlendTargets[entry.Key] = i;
                    count++;
                }
            }
        }

        

        EditorUtility.SetDirty(morphTargetScript);
        Debug.Log($"Successfully mapped {count} CC4 blendshapes!");
    }
}