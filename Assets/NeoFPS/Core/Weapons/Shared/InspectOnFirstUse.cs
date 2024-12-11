using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [RequireComponent(typeof(IWieldable))]
    [RequireComponent(typeof(IInventoryItem))]
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-inspectonfirstuse.html")]
    public class InspectOnFirstUse : MonoBehaviour
    {
        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Trigger, true, false), Tooltip("The name of the trigger parameter to fire on the weapon's animator when picking up the item for the first time.")]
        private string m_FirstUseTrigger = "FirstUse";
        [SerializeField, Tooltip("The time the inspcet animation takes, to prevent shooting and reloading during this time.")]
        private float m_AnimationDuration = 3f;

        private ItemRecognitionTracker m_Recog = null;
        private bool m_Inspecting = false;
        private float m_Timer = 0f;
        private int m_ItemID = -1;

        public IWieldable wieldable
        {
            get;
            private set;
        }

        protected void Start()
        {
            wieldable = GetComponent<IWieldable>();
            if (wieldable != null)
            {
                var item = GetComponent<IInventoryItem>();
                if (item != null)
                    m_ItemID = item.itemIdentifier;
                else
                    enabled = false;
            }
            else
                enabled = false;
        }

        private void OnDisable()
        {
            if (m_Inspecting)
            {
                m_Inspecting = false;
                wieldable.RemoveBlocker(this);
                enabled = false;
            }
        }

        void StartInspecting()
        {
            var wielder = wieldable.wielder;
            if (wielder != null)
            {
                m_Recog = wielder.controller.gameObject.GetComponent<ItemRecognitionTracker>();
                if (m_Recog == null)
                    m_Recog = wielder.GetComponent<ItemRecognitionTracker>();

                if (m_Recog != null && !m_Recog.IsItemRecognised(m_ItemID))
                {
                    m_Recog.RecordItem(m_ItemID);
                    wieldable.animationHandler.SetTrigger(m_FirstUseTrigger);
                    wieldable.AddBlocker(this);
                    m_Inspecting = true;
                }
                else
                    enabled = false;
            }
            else
                enabled = false;
        }

        protected void Update()
        {
            if (!m_Inspecting)
                StartInspecting();
            else
            {
                m_Timer += Time.deltaTime;
                if (m_Timer > m_AnimationDuration)
                {
                    m_Inspecting = false;
                    wieldable.RemoveBlocker(this);
                    enabled = false;
                }
            }
        }
    }
}