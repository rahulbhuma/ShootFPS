using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.ModularFirearms
{
    public interface IModularFirearmModeSwitcher
    {
        string currentMode { get; }

        event UnityAction onSwitchModes;

        void GetStartingMode();
        void SwitchModes();
    }
}