#if UNITY_EDITOR

using NeoFPS;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(AnimatedWeaponInspect), true)]
    public class AnimatedWeaponInspectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            NeoFpsEditorGUI.ScriptField(serializedObject);

            OnInspectorGUIInternal();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnInspectorGUIInternal()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_InspectKey"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReleaseDelay"));
            var numPoses = serializedObject.FindProperty("m_NumPoses");
            EditorGUILayout.PropertyField(numPoses);
            if (numPoses.intValue > 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PoseKey"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ResetPose"));
            }
        }
    }
}

#endif