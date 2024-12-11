#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(SetRootMotionStrengthBehaviour))]
    public class SetRootMotionStrengthBehaviourEditor : MotionGraphBehaviourEditor
    {
        private GUIContent m_BlendTimeLabel = new GUIContent("Blend Time", "The amount of time it would take to blend from 0-1 or 1-0.");
        
        protected override void OnInspectorGUI()
        {
            var whenProp = serializedObject.FindProperty("m_When");
            EditorGUILayout.PropertyField(whenProp);

            switch(whenProp.enumValueIndex)
            {
                case 0:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnEnterValue"));
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnEnterBlendTime"), m_BlendTimeLabel);
                    --EditorGUI.indentLevel;
                    break;
                case 1:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnExitValue"));
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnExitBlendTime"), m_BlendTimeLabel);
                    --EditorGUI.indentLevel;
                    break;
                case 2:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnEnterValue"));
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnEnterBlendTime"), m_BlendTimeLabel);
                    --EditorGUI.indentLevel;

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnExitValue"));
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnExitBlendTime"), m_BlendTimeLabel);
                    --EditorGUI.indentLevel;
                    break;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RootMotionDamping"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PositionMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RotationMultiplier"));
        }
    }
}

#endif
