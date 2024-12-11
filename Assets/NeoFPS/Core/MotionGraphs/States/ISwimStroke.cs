
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.CharacterMotion.States
{
    public interface ISwimStroke
    {
        event UnityAction<float> onStroke;
    }
}