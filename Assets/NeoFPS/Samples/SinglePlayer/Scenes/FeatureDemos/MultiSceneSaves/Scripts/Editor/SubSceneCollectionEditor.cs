#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS.SinglePlayer;
using System;

namespace NeoFPSEditor.SinglePlayer
{
    [CustomEditor(typeof(SubSceneCollection), true)]
    public class SubSceneCollectionEditor : Editor
    {
        private ReorderableList m_SubSceneList = null;

        protected void OnEnable()
        {
            m_SubSceneList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_SubScenes"));
            m_SubSceneList.drawHeaderCallback = DrawSubSceneListHeader;
            m_SubSceneList.drawElementCallback = DrawSubSceneListElement;
        }

        private void DrawSubSceneListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Sub-Scenes");
        }

        private void DrawSubSceneListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            NeoFpsEditorGUI.SceneStringField(rect, m_SubSceneList.serializedProperty.GetArrayElementAtIndex(index));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            m_SubSceneList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif