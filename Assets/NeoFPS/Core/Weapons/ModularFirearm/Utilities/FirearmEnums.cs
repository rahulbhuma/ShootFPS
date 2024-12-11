using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    public enum SprintFireAction
    {
        CannotFire,
        StopSprinting,
        StopAnimation
    }

    public enum SprintAimAction
    {
        StopSprinting,
        StopAnimation,
        CannotAim
    }

    public enum SprintInterruptAction
    {
        StopSprinting,
        StopAnimation
    }
}
