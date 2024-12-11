using System.Collections;
using System.Collections.Generic;
using NeoFPS.Constants;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.WorkInProgress
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-compoundcrosshairdriver.html")]
    public class CompoundCrosshairDriver : MonoBehaviour, ICrosshairDriver
    {
        [SerializeField, Tooltip("The default crosshair to show.")]
        private FpsCrosshair m_Crosshair = FpsCrosshair.Default;

        public event UnityAction<FpsCrosshair> onCrosshairChanged;
        public event UnityAction<float> onAccuracyChanged;

        private bool m_HideCrosshair = false;
        private ICrosshairDriver[] m_Drivers = null;

        public FpsCrosshair crosshair
        {
            get
            {
                if (m_HideCrosshair)
                    return FpsCrosshair.None;
                else
                    return m_Crosshair;
            }
        }

        public float accuracy
        {
            get;
            private set;
        }

        protected void Awake()
        {
            // Get the attached child drivers
            var drivers = GetComponentsInChildren<ICrosshairDriver>();
            m_Drivers = new ICrosshairDriver[drivers.Length - 1];
            for (int i = 1; i < drivers.Length; ++i)
            {
                m_Drivers[i - 1] = drivers[i];
                drivers[i].onAccuracyChanged += OnSubDriverAccuracyChanged;
            }

            // Reset the current accuracy
            RefreshAccuracy();
        }

        bool RefreshAccuracy()
        {
            // Record old accuracy
            float old = accuracy;

            // Get new accuracy (min of drivers)
            accuracy = 1f;
            for (int i = 0; i < m_Drivers.Length; ++i)
                accuracy = Mathf.Min(accuracy, m_Drivers[i].accuracy);

            // Check if changed
            return old != accuracy;
        }

        void OnSubDriverAccuracyChanged(float to)
        {
            if (RefreshAccuracy() && onAccuracyChanged != null)
                onAccuracyChanged(to);
        }

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
    }
}