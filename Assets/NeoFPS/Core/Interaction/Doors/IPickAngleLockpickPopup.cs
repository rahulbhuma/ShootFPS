using UnityEngine;

namespace NeoFPS
{
    public interface IPickAngleLockpickPopup
    {
        void ApplyInput(float pickRotation, bool tension);
        void Cancel();
    }
}
