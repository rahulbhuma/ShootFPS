#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.Conditions;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Conditions
{
    [MotionGraphConditionDrawer(typeof(EnhancedRayCastCondition))]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgc-enhancedraycastcondition.html")]
    public class EnhancedRayCastConditionDrawer : MotionGraphConditionDrawer
    {
        protected override int numLines
        {
            get
            {
                if (serializedObject.FindProperty("m_CastType").enumValueIndex > 1)
                    return 10;
                else
                    return 9;
            }
        }

        protected override void Inspect (Rect line1)
        {
            Rect r1 = line1;
            Rect r2 = r1;
            r1.width *= 0.5f;
            r2.width *= 0.5f;
            r2.x += r1.width;

            EditorGUI.LabelField (r1, "Source Height");
            EditorGUI.PropertyField(r2, serializedObject.FindProperty("m_NormalisedHeight"), GUIContent.none);

            r1.y += lineOffset;
            r2.y += lineOffset;

            var typeProp = serializedObject.FindProperty("m_CastType");
            EditorGUI.LabelField(r1, "Cast Type");
            EditorGUI.PropertyField(r2, typeProp, GUIContent.none);

            r1.y += lineOffset;
            r2.y += lineOffset;

            if (typeProp.enumValueIndex < 2)
            {
                EditorGUI.LabelField(r1, "Cast Vector");
                EditorGUI.PropertyField(r2, serializedObject.FindProperty("m_CastVector"), GUIContent.none);
            }
            else
            {
                EditorGUI.LabelField(r1, "Direction Parameter");
                MotionGraphEditorGUI.ParameterDropdownField<VectorParameter>(r2, graphRoot, serializedObject.FindProperty("m_CastVectorParameter"));

                r1.y += lineOffset;
                r2.y += lineOffset;

                EditorGUI.LabelField(r1, "Distance");
                EditorGUI.PropertyField(r2, serializedObject.FindProperty("m_Distance"), GUIContent.none);
            }

            r1.y += lineOffset;
            r2.y += lineOffset;

            EditorGUI.LabelField(r1, "Slope Effect");
            EditorGUI.PropertyField(r2, serializedObject.FindProperty("m_SlopeEffect"), GUIContent.none);

            r1.y += lineOffset;
            r2.y += lineOffset;

            EditorGUI.LabelField (r1, "Layer Mask");
            EditorGUI.PropertyField(r2, serializedObject.FindProperty("m_LayerMask"), GUIContent.none);

            r1.y += lineOffset;
            r2.y += lineOffset;

            EditorGUI.LabelField (r1, "Does Hit");
            EditorGUI.PropertyField(r2, serializedObject.FindProperty("m_DoesHit"), GUIContent.none);

            r1.y += lineOffset;
            r2.y += lineOffset;

            EditorGUI.LabelField(r1, "Hit Point Output");
            MotionGraphEditorGUI.ParameterDropdownField<VectorParameter>(r2, graphRoot, serializedObject.FindProperty("m_OutputPoint"));

            r1.y += lineOffset;
            r2.y += lineOffset;

            EditorGUI.LabelField(r1, "Hit Normal Output");
            MotionGraphEditorGUI.ParameterDropdownField<VectorParameter>(r2, graphRoot, serializedObject.FindProperty("m_OutputNormal"));

            r1.y += lineOffset;
            r2.y += lineOffset;

            EditorGUI.LabelField(r1, "Hit Transform Output");
            MotionGraphEditorGUI.ParameterDropdownField<TransformParameter>(r2, graphRoot, serializedObject.FindProperty("m_OutputTransform"));
        }
    }
}

#endif