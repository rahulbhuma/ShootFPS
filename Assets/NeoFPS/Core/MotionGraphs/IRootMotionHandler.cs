using System;
using UnityEngine.Events;
using UnityEngine;
using NeoCC;

namespace NeoFPS.CharacterMotion
{
    public interface IRootMotionHandler
    {
        Vector3 GetRootMotionPositionOffset();
        Quaternion GetRootMotionRotationOffset();
    }
}
