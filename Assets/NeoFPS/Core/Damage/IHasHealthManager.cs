using UnityEngine;
using System;

namespace NeoFPS
{
    public interface IHasHealthManager
    {
        IHealthManager healthManager { get; }
        Transform healthTransform { get; }
    }
}