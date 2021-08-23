using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;



namespace Autohand {
    [CustomEditor(typeof(GrabbablePose))]
    public class GrabPoseEditor : Editor{
        GrabbablePose grabPose;

        private void OnEnable() {
            grabPose = target as GrabbablePose;
        }

        public override void OnInspectorGUI() {
            if(grabPose.gameObject != null && PrefabStageUtility.GetPrefabStage(grabPose.gameObject) == null){
                DrawDefaultInspector();
                EditorUtility.SetDirty(grabPose);
            
                var rect = EditorGUILayout.GetControlRect();
                if(grabPose.rightPoseSet)
                    EditorGUI.DrawRect(rect, Color.green);
                else
                    EditorGUI.DrawRect(rect, Color.red);

                rect.width -= 4;
                rect.height -= 2;
                rect.x += 2;
                rect.y += 1;

                if(GUI.Button(rect, "Save Right Pose"))
                    grabPose.SaveGrabPose(grabPose.editorHand, false);


                rect = EditorGUILayout.GetControlRect();
                if(grabPose.leftPoseSet)
                    EditorGUI.DrawRect(rect, Color.green);
                else
                    EditorGUI.DrawRect(rect, Color.red);

                rect.x += 2;
                rect.y += 1;
                rect.width -= 4;
                rect.height -= 2;

                if(GUI.Button(rect, "Save Left Pose"))
                    grabPose.SaveGrabPose(grabPose.editorHand, true);

            
                

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            
                GUILayout.Label(new GUIContent("-------- For tweaking poses --------"), new GUIStyle(){ fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
                GUILayout.Label(new GUIContent("This will create a copy that should be deleted"), new GUIStyle(){ fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
            
                if(GUILayout.Button("Create Copy - Set Pose"))
                    grabPose.SetGrabPoseEditor(grabPose.editorHand);
                if(GUILayout.Button("Reset Hand"))
                    grabPose.editorHand.RelaxHand();
                EditorGUILayout.Space();
                rect = EditorGUILayout.GetControlRect();
                EditorGUI.DrawRect(rect, Color.red);
                if(GUILayout.Button("Delete Copy")){
                    if(string.Equals(grabPose.editorHand.name, "HAND COPY DELETE"))
                        DestroyImmediate(grabPose.editorHand.gameObject);
                    else
                        Debug.LogError("Not a copy - Will not delete");
                }
                if(GUILayout.Button("Clear Poses")){
                    grabPose.Clear();
                }
            }
            else {
                GUILayout.Label(new GUIContent(" - This will not work in prefab mode - "), new GUIStyle(){ fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
                GUILayout.Label(new GUIContent("Use scene to create poses"), new GUIStyle(){ alignment = TextAnchor.MiddleCenter });
            }
        }
    }
}
