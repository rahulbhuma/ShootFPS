#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NeoFPS;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(WieldableItemBodyAnimOverrides))]
    public class WieldableItemBodyAnimOverridesEditor : Editor
    {
        private bool m_Initialised = false;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            NeoFpsEditorGUI.ScriptField(serializedObject);

            var profileProp = serializedObject.FindProperty("m_CharacterProfile");
            var clipsProp = serializedObject.FindProperty("m_Clips");
            
            EditorGUILayout.PropertyField(profileProp);
            if (profileProp.objectReferenceValue == null)
            {
                clipsProp.arraySize = 0;
                m_Initialised = false;
                EditorGUILayout.HelpBox("Please assign a character profile to see the available animations you can override", MessageType.None);
            }
            else
            {
                var profile = profileProp.objectReferenceValue as FirstPersonCharacterAnimationProfile;

                if (!m_Initialised)
                {
                    m_Initialised = true;

                    // Check clips prop array size
                    int count = profile.clips.Length;
                    clipsProp.arraySize = count;

                    // Get order of clips in profile
                    var profileOrder = new Dictionary<AnimationClip, int>();
                    for (int i = 0; i < count; ++i)
                        profileOrder.Add(profile.clips[i].clip, i);

                    // Sort the clip pairs
                    while (true)
                    {
                        bool clear = true;
                        for (int i = 0; i < count; ++i)
                        {
                            var element = clipsProp.GetArrayElementAtIndex(i);
                            var clip = element.FindPropertyRelative("original").objectReferenceValue as AnimationClip;
                            int target;
                            if (clip != null && profileOrder.TryGetValue(clip, out target) && target != i)
                            {
                                var swap = clipsProp.GetArrayElementAtIndex(target);
                                var swapClip1 = swap.FindPropertyRelative("original").objectReferenceValue;
                                var swapClip2 = swap.FindPropertyRelative("replacement").objectReferenceValue;
                                clipsProp.MoveArrayElement(i, target);
                                element.FindPropertyRelative("original").objectReferenceValue = swapClip1;
                                element.FindPropertyRelative("replacement").objectReferenceValue = swapClip2;

                                clear = false;
                            }
                        }

                        if (clear)
                            break;
                    }

                    // Fill out null clips
                    for (int i = 0; i < count; ++i)
                    {
                        var originalClip = clipsProp.GetArrayElementAtIndex(i).FindPropertyRelative("original");
                        if (originalClip.objectReferenceValue as AnimationClip == null)
                            originalClip.objectReferenceValue = profile.clips[i].clip;
                    }
                }
                
                // Show clip pairs
                for (int i = 0; i < clipsProp.arraySize; ++i)
                {
                    var replacementProp = clipsProp.GetArrayElementAtIndex(i).FindPropertyRelative("replacement");
                    EditorGUILayout.PropertyField(replacementProp, new GUIContent(profile.clips[i].description));
                }                
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif