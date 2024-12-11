using NeoFPS.Constants;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.WieldableTools
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-wieldabletool.html")]
    public class WieldableTool : BaseWieldableItem, IWieldableTool, ICrosshairDriver
    {
        [SerializeField, Tooltip("Does tapping the primary fire button have the effect of holding it until interrupted (useful for timed sequences)")]
        private bool m_PrimaryAutoHold = false;

        [SerializeField, Tooltip("Actions to fire immediately when the tool primary is used (in sequence).")]
        private List<BaseWieldableToolModule> m_PrimaryModules = new List<BaseWieldableToolModule>();

        [SerializeField, Tooltip("Does tapping the secondary fire button have the effect of holding it until interrupted (useful for timed sequences)")]
        private bool m_SecondaryAutoHold = false;

        [SerializeField, Tooltip("Actions to fire immediately when the tool primary is used (in sequence).")]
        private List<BaseWieldableToolModule> m_SecondaryModules = new List<BaseWieldableToolModule>();

        [Header("Misc")]
        
        [SerializeField, Tooltip("The crosshair to show when the weapon is drawn.")]
        private FpsCrosshair m_Crosshair = FpsCrosshair.Default;

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private List<BaseWieldableToolModule> m_UnmappedModules = new List<BaseWieldableToolModule>();
#endif

        public event UnityAction onPrimaryActionStart;
        public event UnityAction onPrimaryActionEnd;
        public event UnityAction onSecondaryActionStart;
        public event UnityAction onSecondaryActionEnd;

        private InputState m_PrimaryState = InputState.None;
        private InputState m_SecondaryState = InputState.None;
        private bool m_PrimaryPressed = false;
        private bool m_SecondaryPressed = false;
        private bool m_ContinuousSuccess = true;
        private bool m_Interrupted = false;

        enum InputState
        {
            None,
            Down,
            Held,
            Released
        }

        private InputState primaryState
        { 
            get { return m_PrimaryState; }
            set
            {
                if (m_PrimaryState != value)
                {
                    m_PrimaryState = value;
                    switch (m_PrimaryState)
                    {
                        case InputState.None:
                            if (onPrimaryActionEnd != null)
                                onPrimaryActionEnd();
                            break;
                        case InputState.Down:
                            if (onPrimaryActionStart != null)
                                onPrimaryActionStart();
                            break;
                    }
                }
            }
        }

        private InputState secondaryState
        {
            get { return m_SecondaryState; }
            set
            {
                if (m_SecondaryState != value)
                {
                    m_SecondaryState = value;
                    switch (m_SecondaryState)
                    {
                        case InputState.None:
                            if (onSecondaryActionEnd != null)
                                onSecondaryActionEnd();
                            break;
                        case InputState.Down:
                            if (onSecondaryActionStart != null)
                                onSecondaryActionStart();
                            break;
                    }
                }
            }
        }

#if UNITY_EDITOR

        private static List<BaseWieldableToolModule> s_AvailableActions = new List<BaseWieldableToolModule>();

        public void CheckAvailableActions()
        {
            // Check unmapped actions
            for (int i = m_UnmappedModules.Count - 1; i >= 0; --i)
            {
                if (m_UnmappedModules[i] == null || CheckActionIsReferenced(m_UnmappedModules[i]))
                    m_UnmappedModules.RemoveAt(i);
            }

            // Skim through available actions
            GetComponents(s_AvailableActions);
            foreach (var available in s_AvailableActions)
            {
                if (!CheckActionIsReferenced(available) && !m_UnmappedModules.Contains(available))
                    m_UnmappedModules.Add(available);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            // Check mapped actions are valid
            for (int i = m_PrimaryModules.Count - 1; i >= 0; --i)
            {
                if (m_PrimaryModules[i] == null)
                    m_PrimaryModules.RemoveAt(i);
            }
            for (int i = m_SecondaryModules.Count - 1; i >= 0; --i)
            {
                if (m_SecondaryModules[i] == null)
                    m_SecondaryModules.RemoveAt(i);
            }

            CheckAvailableActions();
        }

        bool CheckActionIsReferenced(BaseWieldableToolModule action)
        {
            foreach (var a in m_PrimaryModules)
                if (a == action)
                    return true;
            foreach (var a in m_SecondaryModules)
                if (a == action)
                    return true;

            return false;
        }

#endif

        public bool CheckCanUse()
        {
            return (!CheckModulesBlocked(m_PrimaryModules) || !CheckModulesBlocked(m_SecondaryModules));
        }

        protected void Start()
        {
            if (Application.isPlaying)
            {
                // Initialise actions
                for (int i = 0; i < m_PrimaryModules.Count; ++i)
                {
                    if (m_PrimaryModules[i] != null)
                        m_PrimaryModules[i].Initialise(this);
                }
                for (int i = 0; i < m_SecondaryModules.Count; ++i)
                {
                    if (m_SecondaryModules[i] != null)
                        m_SecondaryModules[i].Initialise(this);
                }
            }
        }

        protected override void OnDisable()
        {
            Interrupt();
            base.OnDisable();
        }

        bool CheckModulesBlocked(List<BaseWieldableToolModule> modules)
        {
            for (int i = 0; i < modules.Count; ++i)
            {
                if (modules[i].blocking)
                    return true;
            }
            return false;
        }

        bool CheckModulesBusy(List<BaseWieldableToolModule> modules)
        {
            for (int i = 0; i < modules.Count; ++i)
            {
                if (modules[i].busy)
                    return true;
            }
            return false;
        }

        public void PrimaryPress()
        {
            // Track trigger pressed
            m_PrimaryPressed = true;

            // Set new state if currently valid
            if (primaryState == InputState.None && secondaryState == InputState.None)
            {
                if (!isBlocked && !CheckModulesBlocked(m_PrimaryModules))
                    primaryState = InputState.Down;
            }
        }

        public void PrimaryRelease()
        {
            // Reset state if currently valid
            if (primaryState == InputState.Held && m_PrimaryAutoHold == false)
                primaryState = InputState.Released;

            // Track trigger released
            m_PrimaryPressed = false;

            // Set alternative state if waiting
            if (m_SecondaryPressed && secondaryState == InputState.None)
                secondaryState = InputState.Down;
        }

        public void SecondaryPress()
        {
            // Track trigger pressed
            m_SecondaryPressed = true;

            // Set new state if currently valid
            if (primaryState == InputState.None && secondaryState == InputState.None)
            {
                if (!isBlocked && !CheckModulesBlocked(m_SecondaryModules))
                    secondaryState = InputState.Down;
            }
        }

        public void SecondaryRelease()
        {
            // Reset state if currently valid
            if (secondaryState == InputState.Held && m_SecondaryAutoHold == false)
                secondaryState = InputState.Released;

            // Track trigger released
            m_SecondaryPressed = false;

            // Set alternative state if waiting
            if (m_PrimaryPressed && primaryState == InputState.None)
                primaryState = InputState.Down;
        }

        public void Interrupt()
        {
            // Track triggers released
            m_PrimaryPressed = false;
            m_SecondaryPressed = false;

            // Reset states if required
            if (primaryState == InputState.Held)
            {
                // Interrupt modules
                for (int i = 0; i < m_PrimaryModules.Count; ++i)
                    m_PrimaryModules[i].Interrupt();
                // Reset state
                primaryState = InputState.Released;
            }
            if (secondaryState == InputState.Held)
            {
                // Interrupt modules
                for (int i = 0; i < m_SecondaryModules.Count; ++i)
                    m_SecondaryModules[i].Interrupt();
                // Reset state
                secondaryState = InputState.Released;
            }

            m_Interrupted = true;
        }

        public override bool isBusy
        {
            get { return CheckModulesBusy(m_PrimaryModules) || CheckModulesBusy(m_SecondaryModules); }
        }

        public override bool isBlocked
        {
            get { return base.isBlocked || isBusy; }
        }

        protected override void OnIsBlockedChanged(bool blocked)
        {
            base.OnIsBlockedChanged(blocked);

            if (blocked)
                Interrupt();
        }

        protected void FixedUpdate()
        {
            if (Application.isPlaying)
            {
                if (primaryState != InputState.None)
                    primaryState = TickActions(m_PrimaryModules, primaryState, m_PrimaryPressed, secondaryState);
                if (secondaryState != InputState.None)
                    secondaryState = TickActions(m_SecondaryModules, secondaryState, m_SecondaryPressed, primaryState);
            }
        }

        InputState TickActions(List<BaseWieldableToolModule> actions, InputState state, bool pressed, InputState otherState)
        {
            bool blocked = CheckModulesBlocked(actions);

            // Check if just unblocked
            if (!blocked && pressed && state == InputState.None && otherState == InputState.None)
                state = InputState.Down;

            // Initial press
            if (state == InputState.Down && !isBusy)
            {
                if (blocked)
                    state = InputState.None;
                else
                {
                    // Fire actions
                    for (int i = 0; i < actions.Count; ++i)
                    {
                        if (actions[i] != null && actions[i].enabled && actions[i].isValid && (actions[i].timing & WieldableToolActionTiming.Start) == WieldableToolActionTiming.Start)
                                actions[i].FireStart();
                    }

                    state = InputState.Held;
                }
            }

            // Held
            if (state == InputState.Held)
            {
                if (blocked)
                    state = InputState.Released;
                else
                {
                    // Reset success check
                    m_ContinuousSuccess = true;

                    // Reset interrupted
                    m_Interrupted = false;

                    // Tick continuous actions
                    for (int i = 0; i < actions.Count; ++i)
                    {
                        if (actions[i] != null && actions[i].enabled && actions[i].isValid && (actions[i].timing & WieldableToolActionTiming.Continuous) == WieldableToolActionTiming.Continuous)
                            m_ContinuousSuccess &= actions[i].TickContinuous();
                    }

                    // Check if interrupted during actions
                    if (m_Interrupted)
                        state = InputState.Released;
                }
            }

            // Released
            if (state == InputState.Released)
            {
                // Fire end actions (if can)
                for (int i = 0; i < actions.Count; ++i)
                {
                    if (actions[i] != null && actions[i].enabled && actions[i].isValid && (actions[i].timing & WieldableToolActionTiming.End) == WieldableToolActionTiming.End)
                        actions[i].FireEnd(m_ContinuousSuccess);
                }

                // Set input state
                state = InputState.None;
            }

            return state;
        }
        
        #region ICrosshairDriver IMPLEMENTATION

        private bool m_HideCrosshair = false;

        public FpsCrosshair crosshair
        {
            get { return m_Crosshair; }
        }

        private float m_Accuracy = 1f;
        public float accuracy
        {
            get { return m_Accuracy; }
            private set
            {
                m_Accuracy = value;
                if (onAccuracyChanged != null)
                    onAccuracyChanged(m_Accuracy);
            }
        }

        public event UnityAction<FpsCrosshair> onCrosshairChanged;
        public event UnityAction<float> onAccuracyChanged;

        public void HideCrosshair()
        {
            if (!m_HideCrosshair)
            {
                bool triggerEvent = (onCrosshairChanged != null && crosshair == FpsCrosshair.None);

                m_HideCrosshair = true;

                if (triggerEvent)
                    onCrosshairChanged(FpsCrosshair.None);
            }
        }

        public void ShowCrosshair()
        {
            if (m_HideCrosshair)
            {
                // Reset
                m_HideCrosshair = false;

                // Fire event
                if (onCrosshairChanged != null && crosshair != FpsCrosshair.None)
                    onCrosshairChanged(crosshair);
            }
        }

        #endregion

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_PrimaryStateKey = new NeoSerializationKey("pState");
        private static readonly NeoSerializationKey k_SecondaryStateKey = new NeoSerializationKey("sState");
        private static readonly NeoSerializationKey k_SuccessKey = new NeoSerializationKey("success");

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            writer.WriteValue(k_PrimaryStateKey, (int)m_PrimaryState);
            writer.WriteValue(k_SecondaryStateKey, (int)m_SecondaryState);
            writer.WriteValue(k_SuccessKey, m_ContinuousSuccess);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            int intValue;
            reader.TryReadValue(k_SuccessKey, out intValue, (int)m_PrimaryState);
            m_PrimaryState = (InputState)intValue;
            reader.TryReadValue(k_SecondaryStateKey, out intValue, (int)m_SecondaryState);
            m_SecondaryState = (InputState)intValue;

            reader.TryReadValue(k_SuccessKey, out m_ContinuousSuccess, m_ContinuousSuccess);
        }

        #endregion
    }
}