#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS;

namespace NeoFPSEditor
{
	[CustomEditor (typeof (ScenePoolHandler))]
	public class ScenePoolHandlerEditor : BasePoolInfoEditor
	{
        protected override SerializedProperty GetPoolInfoArrayProperty()
        {
            return serializedObject.FindProperty("m_ScenePools");
        }

        public override void OnInspectorGUI ()
		{
			serializedObject.Update ();

            EditorGUILayout.LabelField("Starting Pools", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Collections"), true);
            DoLayoutPoolInfo();

			serializedObject.ApplyModifiedProperties ();
		}
	}
}

#endif