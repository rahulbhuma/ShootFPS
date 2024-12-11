using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-neofpstouchinventorybutton.html")]
    public class NeoFpsTouchInventoryButton : BaseTouchControl
    {
        HudInventoryItemBase m_HudInventoryItem = null;

        protected override void Awake()
        {
            base.Awake();

            m_HudInventoryItem = GetComponent<HudInventoryItemBase>();
            if (m_HudInventoryItem == null)
                gameObject.SetActive(false);
        }

        public override bool HandleTouch(Touch touch)
        {
            controller.buttons[FpsInputButton.Quickslot1 + m_HudInventoryItem.slotIndex] = true;

            return consume;
        }

        protected override void OnTouchStarted() { }
        protected override void OnTouchEnded() { }
    }
}