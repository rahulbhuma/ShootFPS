#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace NeoFPSEditor
{
    [CustomEditor(typeof(WieldableItemKinematics))]
    public class WieldableItemKinematicsEditor : Editor
    {
        HandBoneOffsetsEditor m_OffsetsEditor = null;
        GUIContent[] m_FingerLabels = null;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            // Script field
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Animator"));

            EditorGUILayout.LabelField("Hands", EditorStyles.boldLabel);

            // Settings props
            var handMatching = serializedObject.FindProperty("m_HandMatching");
            var fingerMatching = serializedObject.FindProperty("m_FingerMatching");
            var rigType = serializedObject.FindProperty("m_RigType");

            // Hand matching (don't show the rest if false)
            EditorGUILayout.PropertyField(handMatching);
            if (handMatching.enumValueIndex != 0)
            {
                EditorGUILayout.PropertyField(fingerMatching);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(rigType);

                // Show generic rig transform properties
                if (rigType.enumValueIndex == 0) // Generic
                {
                    // With fingers
                    if (fingerMatching.boolValue)
                    {
                        // Check finger bone array is the correct size
                        var fingerBoneArray = serializedObject.FindProperty("m_FingerBones");
                        if (fingerBoneArray.arraySize != 30)
                            fingerBoneArray.arraySize = 30;

                        if (handMatching.enumValueIndex != 2)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LeftHandIkTarget"));
                            InspectFingers("Left Hand Fingers", fingerBoneArray, serializedObject.FindProperty("expandLeftFingers"), 0);
                        }
                        if (handMatching.enumValueIndex != 1)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RightHandIkTarget"));
                            InspectFingers("Right Hand Fingers", fingerBoneArray, serializedObject.FindProperty("expandRightFingers"), 15);
                        }
                    }
                    else
                    {
                        // Without fingers
                        if (handMatching.enumValueIndex != 2)
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LeftHandIkTarget"));
                        if (handMatching.enumValueIndex != 1)
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RightHandIkTarget"));
                    }
                }
            }

            var offsetsProp = serializedObject.FindProperty("m_Offsets");
            var oldOffsetsObject = offsetsProp.objectReferenceValue;

            // Show offsets
            EditorGUILayout.PropertyField(offsetsProp);

            // Show create offsets button
            if (GUILayout.Button("Create Offsets Asset"))
            {
                var cast = target as WieldableItemKinematics;
                string path = string.Empty;

                var prefabStage = PrefabStageUtility.GetPrefabStage(cast.gameObject);
                if (prefabStage == null)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(cast))
                    {
                        // Get path from prefab instance
                        path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(cast);
                    }
                    else
                    {
                        // Get path from prefab asset
                        path = AssetDatabase.GetAssetPath(cast.transform.root);
                    }
                }
                else
                {
                    // Get path from prefab stage (editing asset)
#if UNITY_2020_1_OR_NEWER
                    path = prefabStage.assetPath;
#else
                    path = prefabStage.prefabAssetPath;
#endif
                }

                if (!string.IsNullOrEmpty(path))
                {
                    path = AssetDatabase.GenerateUniqueAssetPath(path.Replace(".prefab", "Offsets.asset"));
                    var offsets = CreateInstance<HandBoneOffsets>();
                    AssetDatabase.CreateAsset(offsets, path);
                    offsetsProp.objectReferenceValue = offsets;
                }
                else
                {
                    Debug.LogError("Object must be a prefab to automatically add an offsets asset.");
                }
            }

            // Clear offsets editor if not known
            var newOffsetsObject = offsetsProp.objectReferenceValue;
            if (oldOffsetsObject != newOffsetsObject)
            {
                // Destroy editor
                if (m_OffsetsEditor != null)
                {
                    DestroyImmediate(m_OffsetsEditor);
                    m_OffsetsEditor = null;
                }
            }

            if (offsetsProp.objectReferenceValue != null)
            {
                var expandOffsets = serializedObject.FindProperty("editOffsets");
                EditorGUILayout.PropertyField(expandOffsets);
                if (expandOffsets.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FingerGizmoScale"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_HandGizmoScale"));

                    // Get the editor
                    if (m_OffsetsEditor == null) // Actually, better to use the editor == null or something
                        m_OffsetsEditor = CreateEditor(offsetsProp.objectReferenceValue) as HandBoneOffsetsEditor;

                    if (m_OffsetsEditor != null)
                        m_OffsetsEditor.InspectOffsets();
                }
            }

            GUILayout.Space(2);

            serializedObject.ApplyModifiedProperties();
        }

        void InspectFingers(string header, SerializedProperty prop, SerializedProperty expand, int offset)
        {
            InitialiseLabels();
            
            expand.boolValue = EditorGUILayout.Foldout(expand.boolValue, header, true);
            if (expand.boolValue)
            {
                ++EditorGUI.indentLevel;
                for (int i = 0; i < 15; ++i)
                    EditorGUILayout.PropertyField(prop.GetArrayElementAtIndex(i + offset), m_FingerLabels[i]);
                NeoFpsEditorGUI.Separator();
                --EditorGUI.indentLevel;
            }
        }

        void InitialiseLabels()
        {
            if (m_FingerLabels == null)
            {
                m_FingerLabels = new GUIContent[] {
                    new GUIContent("Thumb 1 (Proximal)"),
                    new GUIContent("Thumb 2 (Intermediate)"),
                    new GUIContent("Thumb 3 (Distal)"),
                    new GUIContent("Index 1 (Proximal)"),
                    new GUIContent("Index 2 (Intermediate)"),
                    new GUIContent("Index 3 (Distal)"),
                    new GUIContent("Middle 1 (Proximal)"),
                    new GUIContent("Middle 2 (Intermediate)"),
                    new GUIContent("Middle 3 (Distal)"),
                    new GUIContent("Ring 1 (Proximal)"),
                    new GUIContent("Ring 2 (Intermediate)"),
                    new GUIContent("Ring 3 (Distal)"),
                    new GUIContent("Pinky 1 (Proximal)"),
                    new GUIContent("Pinky 2 (Intermediate)"),
                    new GUIContent("Pinky 3 (Distal)")
                };
            }
        }

        private void OnDestroy()
        {
            if (m_OffsetsEditor != null)
            {
                DestroyImmediate(m_OffsetsEditor);
                m_OffsetsEditor = null;
            }
        }
    }
}

#endif