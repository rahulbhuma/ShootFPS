using UnityEngine.Events;

namespace NeoFPS
{
    public interface ILaserPointer
    {
        event UnityAction onToggleOn;
        event UnityAction onToggleOff;
    }
}
