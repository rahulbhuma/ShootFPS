using System;
using UnityEngine;

namespace NeoFPS
{
    public interface IOriginShiftSubscriber
    {
        void ApplyOffset(Vector3 offset);
    }
}
