using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    public interface IAimable
    {
        Transform aimRelativeTransform { get; }
        Vector3 aimRelativePosition { get; }
        Quaternion aimRelativeRotation { get; }
    }
}