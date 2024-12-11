using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.CharacterMotion
{
    public interface ICharacterStepTracker
    {
        event UnityAction onStep;

        float stepCounter { get; }
        float strideLength { get; set; }
        float maxStepRate { get; set; }

        void SetWholeStep();
    }
}
