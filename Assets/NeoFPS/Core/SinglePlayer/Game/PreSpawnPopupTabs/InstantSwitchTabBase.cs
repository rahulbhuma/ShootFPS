using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using NeoFPS.Samples;

namespace NeoFPS.SinglePlayer
{
    public abstract class InstantSwitchTabBase : PreSpawnPopupTab
    {
        protected override void OnStateChanged()
        {
            base.OnStateChanged();
            if (state == PopupTabState.Hiding)
                state = PopupTabState.Hidden;
            if (state == PopupTabState.Showing)
                state = PopupTabState.Visible;
        }
    }
}
