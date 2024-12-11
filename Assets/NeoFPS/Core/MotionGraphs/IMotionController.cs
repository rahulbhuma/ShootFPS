using System;
using UnityEngine.Events;
using UnityEngine;
using NeoCC;

namespace NeoFPS.CharacterMotion
{
    public interface IMotionController
    {
        MotionGraphContainer motionGraph { get; }
        MotionGraphState currentState { get; }

        event UnityAction onCurrentStateChanged;

        Transform localTransform { get; }
        Animator bodyAnimator { get; }
        INeoCharacterController characterController { get; }
        IAimController aimController { get; }
        
        IRootMotionHandler rootMotionHandler { get; }
        float rootMotionStrength { get; }
        float rootMotionDamping { get; set; }
        float rootMotionPositionMultiplier { get; set; }
        float rootMotionRotationMultiplier { get; set; }
        void SetRootMotionStrength(float strength, float blendTime);

        //float characterHeightMultiplier { get; set; }
        float GetHeightMultiplier();
        void SetHeightMultiplier(float multiplier, float duration, CharacterResizePoint point = CharacterResizePoint.Automatic);
        bool CheckIsHeightMultiplierRestricted (float multiplier);
        void ChangeStandingHeight(float height);
        
        Vector2 inputMoveDirection { get; set; }
        float inputMoveScale { get; set; }
        
        // Add monobehaviour methods to remove need for casting if required
        GameObject gameObject { get; }
        Transform transform { get; }
        T GetComponent<T>();
        T GetComponentInChildren<T>();
        T GetComponentInParent<T>();
        T[] GetComponents<T>();
        T[] GetComponentsInChildren<T>(bool includeInactive = false);
        T[] GetComponentsInParent<T>(bool includeInactive = false);
        Component GetComponent(Type t);
        Component GetComponentInChildren(Type t);
        Component GetComponentInParent(Type t);
        Component[] GetComponents(Type t);
        Component[] GetComponentsInChildren(Type t, bool includeInactive = false);
        Component[] GetComponentsInParent(Type t, bool includeInactive = false);
    }

    public enum CharacterResizePoint
    {
        Automatic,
        Bottom,
        Top
    }
}

