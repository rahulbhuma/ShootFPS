#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.Conditions;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Conditions
{
    [MotionGraphConditionDrawer(typeof(VectorTypeCondition))]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgc-vectortypecondition.html")]
    public class VectorTypeConditionDrawer : MotionGraphConditionDrawer
    {
        protected override int numLines
        {
            get { return 2; }
        }

        protected override void Inspect (Rect line1)
        {
            Rect r1 = line1;
            Rect r2 = r1;
            r1.width *= 0.5f;
            r2.width *= 0.5f;
            r2.x += r1.width;

            EditorGUI.LabelField(r1, "Parameter");
            MotionGraphEditorGUI.ParameterDropdownField<VectorParameter>(r2, graphRoot, serializedObject.FindProperty("m_Property"));

            r1.y += lineOffset;
            r1.width -= 2;
            r2.y += lineOffset;

            EditorGUI.PropertyField(r1, serializedObject.FindProperty("m_What"), new GUIContent());
            EditorGUI.PropertyField(r2, serializedObject.FindProperty("m_IsTrue"));
        }
    }
}

#endif