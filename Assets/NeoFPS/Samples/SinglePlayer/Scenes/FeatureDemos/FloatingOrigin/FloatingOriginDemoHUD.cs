using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeoFPS.Samples
{
    public class FloatingOriginDemoHUD : MonoBehaviour, IOriginShiftSubscriber
    {
        [SerializeField, Tooltip("")]
        private Text m_OffsetText = null;

        void Start()
        {
            if (OriginShift.system != null)
            {
                OriginShift.system.AddSubscriber(this);
                ApplyOffset(OriginShift.system.currentOffset);
            }
        }

        void OnDestroy()
        {
            if (OriginShift.system != null)
                OriginShift.system.RemoveSubscriber(this);
        }

        public void ApplyOffset(Vector3 offset)
        {
            Vector3 steps = OriginShift.system.currentOffset / OriginShift.system.threshold;
            int x = Mathf.RoundToInt(steps.x);
            int z = Mathf.RoundToInt(steps.z);
            m_OffsetText.text = string.Format("[{0}, {1}]", x, z);
        }
    }
}