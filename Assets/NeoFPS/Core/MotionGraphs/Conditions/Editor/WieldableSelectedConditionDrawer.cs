#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.Conditions;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Conditions
{
    [MotionGraphConditionDrawer(typeof(WieldableSelectedCondition))]
    [HelpURL("http://docs.neofps.com/manual/motiongraphref-mgc-wieldableselectedcondition.html")]
    public class WieldableSelectedConditionDrawer : MotionGraphConditionDrawer
    {
        protected override void Inspect (Rect line1)
        {
            Rect r1 = line1;
            Rect r2 = r1;
            r1.width *= 0.5f;
            r2.width *= 0.5f;
            r2.x += r1.width;
            r1.width -= 2f;
            r1.height += 1f;
            r1.y -= 1f;

            //MotionGraphEditorGUI.ParameterDropdownField<IntParameter>(r1, graphRoot, serializedObject.FindProperty("m_Property"));
            EditorGUI.PropertyField(r1, serializedObject.FindProperty("m_ItemKey"), GUIContent.none);

            // Draw the compare value property
            EditorGUI.PropertyField(r2, serializedObject.FindProperty("m_Selected"), GUIContent.none);
        }
    }
}

#endif