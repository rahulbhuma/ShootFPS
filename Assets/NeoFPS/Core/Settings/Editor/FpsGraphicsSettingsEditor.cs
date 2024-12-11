#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(FpsGraphicsSettings), true)]
    public class FpsGraphicsSettingsEditor : SettingsContextEditor
    {
        GUIContent m_VerticalFoVLabel = null;
        GUIContent m_HorizontalFoVLabel = null;

        const float aspect = 1.77777778f;

        protected override void OnInspectorGUIInternal()
        {
            EditorGUILayout.HelpBox("Resolution, fullscreen and vsync settings are initialised on first run based on the Unity player settings.", MessageType.None);
            EditorGUILayout.Space();

            // Script
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;


            // FoV
            var fovProp = serializedObject.FindProperty("m_VerticalFOV");

            if (m_VerticalFoVLabel == null)
                m_VerticalFoVLabel = new GUIContent("Vertical FoV", fovProp.tooltip);
            if (m_HorizontalFoVLabel == null)
                m_HorizontalFoVLabel = new GUIContent("Horizontal 16:9", "This value is derived from the vertical resolution. Changing it will change the vertical to match.");

            // Vertical FoV property
            EditorGUILayout.DelayedFloatField(fovProp, new GUIContent("Vertical FoV", fovProp.tooltip));

            float horizontalFoV = Camera.VerticalToHorizontalFieldOfView(fovProp.floatValue, aspect);
            float newHorizontalFoV = Mathf.Clamp(EditorGUILayout.DelayedFloatField(m_HorizontalFoVLabel, horizontalFoV), 40f, 160f);
            if (!Mathf.Approximately(horizontalFoV, newHorizontalFoV))
                fovProp.floatValue = Camera.HorizontalToVerticalFieldOfView(newHorizontalFoV, aspect);
        }
    }
}

#endif
