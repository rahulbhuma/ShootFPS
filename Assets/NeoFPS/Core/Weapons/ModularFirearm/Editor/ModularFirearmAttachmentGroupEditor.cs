#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS.ModularFirearms;
using System;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomEditor(typeof(ModularFirearmAttachmentGroup))]
    public class ModularFirearmAttachmentGroupEditor : Editor
    {
        private ReorderableList m_AttachmentsList = null;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            NeoFpsEditorGUI.ScriptField(serializedObject);

            var list = CheckAttachmentsList();
            list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        ReorderableList CheckAttachmentsList()
        {
            if (m_AttachmentsList == null)
            {
                m_AttachmentsList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_Attachments"), true, true, true, true);
                m_AttachmentsList.elementHeightCallback = GetAttachmentsListElementHeight;
                m_AttachmentsList.drawElementCallback = DrawAttachmentsListElement;
                m_AttachmentsList.drawHeaderCallback = DrawAttachmentsListHeader;
            }
            return m_AttachmentsList;
        }

        private float GetAttachmentsListElementHeight(int index)
        {
            if (index >= 0)
                return EditorGUIUtility.standardVerticalSpacing * 3f + EditorGUIUtility.singleLineHeight * 2f;
            else
                return EditorGUIUtility.singleLineHeight;
        }

        private void DrawAttachmentsListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Attachments");
        }

        private void DrawAttachmentsListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2f;
            EditorGUI.PropertyField(rect, m_AttachmentsList.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none, true);
        }
    }
}

#endif