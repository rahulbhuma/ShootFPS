#if UNITY_EDITOR

using NeoFPS;
using NeoFPS.ModularFirearms;
using UnityEditor;
using UnityEngine;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomPropertyDrawer(typeof(AttachmentOption))]
    public class AttachmentOptionPropertyDrawer : NeoPropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetHeight(2);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = GetFirstLine(position);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("attachment"), label);

            position = NextLine(position);
            ++EditorGUI.indentLevel;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("offset"));
            --EditorGUI.indentLevel;
        }
    }
}

#endif