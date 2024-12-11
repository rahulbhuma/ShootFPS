#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.Behaviours;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(DisableDamageBehaviour))]
    public class DisableDamageBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DamageOnEnter"), new GUIContent("On Enter"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DamageOnExit"), new GUIContent("On Exit"));
        }
    }
}

#endif
