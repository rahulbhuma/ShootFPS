#if UNITY_EDITOR

using NeoFPS;
using NeoFPS.ModularFirearms;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomEditor(typeof(ModularFirearmAttachmentUIHandler), true)]
    public class ModularFirearmAttachmentUIHandlerEditor : AnimatedWeaponInspectEditor
    {
        protected override void OnInspectorGUIInternal()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PopupPrefab"));

            base.OnInspectorGUIInternal();
        }
    }
}

#endif