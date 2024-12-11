#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.States;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.States
{
    [CustomEditor(typeof(MatchTransformState))]
    public class MatchTransformStateEditor : MotionGraphStateEditor
    {
        protected override void OnInspectorGUIInternal()
        {
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
            MotionGraphEditorGUI.ParameterDropdownField<TransformParameter>(container, serializedObject.FindProperty("m_TargetTransform"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Match Transform Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MatchingMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_BlendInTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DisableCollisions"));
        }
    }
}

#endif