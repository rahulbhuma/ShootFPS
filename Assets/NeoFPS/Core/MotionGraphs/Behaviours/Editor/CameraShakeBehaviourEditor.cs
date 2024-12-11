#if UNITY_EDITOR

using UnityEditor;
using NeoFPS.CharacterMotion.Parameters;
using NeoFPSEditor.CharacterMotion;

namespace NeoFPS
{
    [MotionGraphBehaviourEditor(typeof(CameraShakeBehaviour))]
    public class CameraShakeBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            MotionGraphEditorGUI.ParameterDropdownField<FloatParameter>(
                owner.container,
                serializedObject.FindProperty("m_ShakeMultiplier")
                );

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ShakeStrength"));
        }
    }
}

#endif
