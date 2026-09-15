using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(OCCController))]
public class OCCControllerEditor : Editor {
    public override void OnInspectorGUI() {
        var controller = (OCCController)target;
        var previousScenarios = controller.Scenarios?.ToArray() ?? new TextAsset[0];
        var previousAgents = controller.Agents?.ToArray() ?? new GameObject[0];
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        bool changed = !previousScenarios.SequenceEqual(controller.Scenarios ?? new TextAsset[0]) ||
            !previousAgents.SequenceEqual(controller.Agents ?? new GameObject[0]);
        EditorGUILayout.HelpBox("Change Scenarios here to update each character's emotion, viseme JSON, and Audio Source clip. Matching files are found by scenario name under Resources/Audio and Resources/Visemes. The character's speech on/off setting is preserved.", MessageType.Info);
        if (changed || GUILayout.Button("Refresh Linked Scenario Files")) {
            if (controller.Agents != null) {
                foreach (var agent in controller.Agents) {
                    if (agent == null) continue;
                    var face = agent.GetComponent<FACS>();
                    var audio = agent.GetComponent<AudioSource>();
                    if (face != null) Undo.RecordObject(face, "Update scenario links");
                    if (audio != null) Undo.RecordObject(audio, "Update scenario links");
                }
            }
            controller.SetLatestChatResponse(null);
            controller.SyncScenarios();
            if (controller.Agents != null) {
                foreach (var agent in controller.Agents) {
                    if (agent == null) continue;
                    var face = agent.GetComponent<FACS>();
                    var audio = agent.GetComponent<AudioSource>();
                    if (face != null) {
                        EditorUtility.SetDirty(face);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(face);
                    }
                    if (audio != null) {
                        EditorUtility.SetDirty(audio);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(audio);
                    }
                }
            }
        }
    }
}
