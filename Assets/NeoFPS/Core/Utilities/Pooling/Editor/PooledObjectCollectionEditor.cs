#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS;

namespace NeoFPSEditor
{
	[CustomEditor (typeof (PooledObjectCollection))]
	public class PooledObjectCollectionEditor : BasePoolInfoEditor
    {
        protected override SerializedProperty GetPoolInfoArrayProperty()
        {
            return serializedObject.FindProperty("m_PooledObjects");
        }

        public override void OnInspectorGUI ()
		{
			serializedObject.Update ();
            
            DoLayoutPoolInfo();

			serializedObject.ApplyModifiedProperties ();
		}
	}
}

#endif