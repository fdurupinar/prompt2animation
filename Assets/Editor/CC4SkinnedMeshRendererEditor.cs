using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SkinnedMeshRenderer))]
[CanEditMultipleObjects]
public class CC4SkinnedMeshEditor : Editor 
{
    private string _searchText = "";
    private bool _showOnlyRelevant = true;

    // Your core FACS and Viseme keys
    private readonly HashSet<string> _relevantKeys = new HashSet<string> {
        "EE", "ER", "IH", "AH", "OH", "W_OO", "S_Z", "CH_J", "F_V", "TH", "T_L_D_N", "B_M_P", "K_G_H_NG", "AE", "R",
        "MOUTH_SHRUG_UPPER", "MOUTH_SHRUG_LOWER", "MOUTH_PUCKER_UP_L", "MOUTH_PUCKER_UP_R",
        "MOUTH_FUNNEL_UP_L", "MOUTH_FUNNEL_UP_R", "MOUTH_PRESS_L", "MOUTH_PRESS_R",
        "MOUTH_TIGHTEN_L", "MOUTH_TIGHTEN_R", "MOUTH_STRETCH_L", "MOUTH_STRETCH_R",
        "MOUTH_DIMPLE_L", "MOUTH_DIMPLE_R", "MOUTH_SMILE_L", "MOUTH_SMILE_R",
        "MOUTH_FROWN_L", "MOUTH_FROWN_R", "MOUTH_DOWN_LOWER_L", "MOUTH_DOWN_LOWER_R",
        "MOUTH_ROLL_IN_UPPER_L", "MOUTH_ROLL_IN_UPPER_R", "MOUTH_ROLL_IN_LOWER_L", "MOUTH_ROLL_IN_LOWER_R",
        "BROW_RAISE_INNER_L", "BROW_RAISE_INNER_R", "BROW_RAISE_OUTER_L", "BROW_RAISE_OUTER_R", "BROW_DROP_L", "BROW_DROP_R",
        "EYE_WIDE_L", "EYE_WIDE_R", "CHEEK_RAISE_L", "CHEEK_RAISE_R", "EYE_SQUINT_L", "EYE_SQUINT_R",
        "NOSE_SNEER_L", "NOSE_SNEER_R", "EYE_BLINK_L", "EYE_BLINK_R", "JAW_OPEN"
    };

    public override void OnInspectorGUI() 
    {
        serializedObject.Update();
        
        // Draw standard header properties
        EditorGUILayout.LabelField("Mesh Settings", EditorStyles.boldLabel);
        DrawProperty("m_Mesh");
        DrawProperty("m_Materials");
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Blendshape Filters", EditorStyles.boldLabel);

        // UI Controls
        EditorGUILayout.BeginHorizontal();
        _searchText = EditorGUILayout.TextField("Search", _searchText);
        if (GUILayout.Button("Clear", GUILayout.Width(50))) _searchText = "";
        EditorGUILayout.EndHorizontal();

        _showOnlyRelevant = EditorGUILayout.Toggle("Show Only Relevant", _showOnlyRelevant);

        EditorGUILayout.Space(5);

        SkinnedMeshRenderer smr = (SkinnedMeshRenderer)target;
        Mesh mesh = smr.sharedMesh;

        if (mesh != null) 
        {
            for (int i = 0; i < mesh.blendShapeCount; i++) 
            {
                string name = mesh.GetBlendShapeName(i);
                string upperName = name.ToUpper();

                // 1. Filter by Relevance (if enabled)
                bool isRelevant = _relevantKeys.Any(key => upperName.Equals(key));
                if (_showOnlyRelevant && !isRelevant) continue;

                // 2. Filter by Search Text
                if (!string.IsNullOrEmpty(_searchText) && !upperName.Contains(_searchText.ToUpper())) continue;

                // 3. Draw Slider
                float weight = smr.GetBlendShapeWeight(i);
                EditorGUI.BeginChangeCheck();
                float newWeight = EditorGUILayout.Slider(name, weight, 0, 100);
                if (EditorGUI.EndChangeCheck()) 
                {
                    Undo.RecordObject(smr, "Change Blendshape Weight");
                    smr.SetBlendShapeWeight(i, newWeight);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawProperty(string name) 
    {
        SerializedProperty prop = serializedObject.FindProperty(name);
        if (prop != null) EditorGUILayout.PropertyField(prop, true);
    }
}