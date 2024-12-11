#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.ModularFirearms;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomEditor(typeof(HeadMoveAimer))]
    public class HeadMoveAimerEditor : OffsetBaseAimerEditor
    {
        protected override void InspectConcreteAimerProperties() { }

        protected override void InspectConcreteAimerTransitions() { }
    }
}

#endif