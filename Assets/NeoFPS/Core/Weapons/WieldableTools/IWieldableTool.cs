using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.WieldableTools
{
    public interface IWieldableTool : IWieldable, IMonoBehaviour
    {
        event UnityAction onPrimaryActionStart;
        event UnityAction onPrimaryActionEnd;
        event UnityAction onSecondaryActionStart;
        event UnityAction onSecondaryActionEnd;

        void PrimaryPress();
        void PrimaryRelease();
        void SecondaryPress();
        void SecondaryRelease();
        void Interrupt();
    }
}