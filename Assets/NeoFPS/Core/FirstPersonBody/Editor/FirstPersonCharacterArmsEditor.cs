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
    [CustomEditor(typeof(FirstPersonCharacterArms))]
    public class FirstPersonCharacterArmsEditor : Editor
    {
        HandBoneOffsetsEditor m_OffsetsEditor = null;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            var prop = serializedObject.FindProperty("m_ArmsRootTransform");
            EditorGUILayout.PropertyField(prop);

            if (prop.objectReferenceValue != null)
                EditorGUILayout.HelpBox("The transform above will be repositioned and rotated each frame to match the weapon view model. Use this when you have a dual-rig weapon setup where the arm and weapon animations are synced.\n\nIf you only want to align the hands and fingers then set the above property to \"None\".", MessageType.Info, true);

            var offsetsProp = serializedObject.FindProperty("m_Offsets");
            var oldOffsetsObject = offsetsProp.objectReferenceValue;

            // Show offsets
            EditorGUILayout.PropertyField(offsetsProp);

            // Show create offsets button
            if (GUILayout.Button("Create Offsets Asset"))
            {
                var cast = target as FirstPersonCharacterArms;
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

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif