#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.ModularFirearms;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomEditor(typeof(WeaponMoveAimer))]
    public class WeaponMoveAimerEditor : OffsetBaseAimerEditor
    {
        protected override void InspectConcreteAimerProperties() { }

        protected override void InspectConcreteAimerTransitions()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PositionTransition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RotationTransition"));
        }
    }
}

#endif