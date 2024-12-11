#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.States;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.States
{
    [CustomEditor(typeof(MoveToPointState))]
    public class MoveToPointStateEditor : MotionGraphStateEditor
    {
        protected override void OnInspectorGUIInternal()
        {
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
            MotionGraphEditorGUI.ParameterDropdownField<VectorParameter>(container, serializedObject.FindProperty("m_TargetPosition"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Move To Point Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Duration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Interpolation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DisableCollisions"));
        }
    }
}

#endif