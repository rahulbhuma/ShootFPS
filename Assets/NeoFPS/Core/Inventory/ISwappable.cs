using NeoFPS.Constants;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
	public interface ISwappable : IMonoBehaviour
    {
        FpsSwappableCategory category { get; }
    }
}