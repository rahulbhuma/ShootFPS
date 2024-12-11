using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-carrysystemencumbrancehandler.html")]
    public class CarrySystemEncumbranceHandler : MonoBehaviour, IMotionGraphDataOverride
    {
        [SerializeField, MotionGraphParameterKey(MotionGraphParameterType.Switch), Tooltip("The optional motion graph parameter to set when encumbered.")]
        private string m_EncumberedSwitchKey = string.Empty;
        [SerializeField, Tooltip("The normalised mass (mass / mass limit) of the object before the encumbered switch should be set to true.")]
        private float m_EncumberedThreshold = 0.5f;
        [SerializeField, Tooltip("The multiplier applied to movement speed when carrying something with a mass at the character's limit. For smaller objects, the multiplier will lerp to 1 as their mass approaches zero.")]
        private float m_MaxWeightSpeedMultiplier = 0.2f;
        [SerializeField, MotionGraphDataKey(MotionGraphDataType.Float), Tooltip("The motion data parameters to apply the multiplier to.")]
        private string[] m_GraphData = { };

        bool m_IsCarrying = false;
        float m_Multiplier = 1f;
        SwitchParameter m_EncumberedParam = null;
        CarrySystemBase m_CarrySystem = null;

        protected void Start()
        {
            // Get the motion controller
            var motionController = GetComponent<IMotionController>();
            if (motionController == null)
            {
                Debug.LogError("CarrySystemEncumbranceHandler requires a component on the object that implements IMotionController");
                return;
            }

            // Get the carry system
            m_CarrySystem = GetComponent<CarrySystemBase>();
            if (m_CarrySystem == null)
            {
                Debug.LogError("CarrySystemEncumbranceHandler requires a component on the object that implements CarrySystemBase");
                return;
            }

            // Apply this component as a data override to the motion graph
            motionController.motionGraph.AddDataOverrides(this);

            // Get the encumbered switch parameter
            if (!string.IsNullOrWhiteSpace(m_EncumberedSwitchKey))
                m_EncumberedParam = motionController.motionGraph.GetSwitchProperty(m_EncumberedSwitchKey);

            // Attach to the carry system events
            m_CarrySystem.onCarryStateChanged += OnCarryStateChanged;
        }

        public Func<bool, bool> GetBoolOverride(BoolData data)
        {
            return null;
        }

        public Func<float, float> GetFloatOverride(FloatData data)
        {
            // Iterate through list of data keys, and use override method for any that match
            for (int i = 0; i < m_GraphData.Length; ++i)
            {
                if (data.name == m_GraphData[i])
                    return GetModifiedSpeed;
            }
            return null;
        }

        public Func<int, int> GetIntOverride(IntData data)
        {
            return null;
        }

        float GetModifiedSpeed(float input)
        {
            return m_Multiplier * input;
        }

        private void OnCarryStateChanged(CarryState carryState)
        {
            if (carryState == CarryState.Carrying)
            {
                m_IsCarrying = true;

                // Get the normalised mass (mass / limit)
                float normalised = m_CarrySystem.carryTarget.mass / m_CarrySystem.massLimit;

                // Set encumbered parameter value
                if (m_EncumberedParam != null && normalised >= m_EncumberedThreshold)
                    m_EncumberedParam.on = true;

                // Set multiplier based on object's mass
                m_Multiplier = Mathf.Lerp(1f, m_MaxWeightSpeedMultiplier, normalised);
            }
            else
            {
                if (m_IsCarrying)
                {
                    m_IsCarrying = false;

                    // Set encumbered parameter value
                    if (m_EncumberedParam != null)
                        m_EncumberedParam.on = false;

                    // Reset the multiplier
                    m_Multiplier = 1f;
                }
            }
        }
    }
}
