using UnityEngine;

namespace NeoFPS
{
    public interface ISurfaceFootstepAudio
    {
        SurfaceAudioData footstepAudio { get; }
        float minimumSpeed { get; }
        float castRange { get; }
        Vector3 castDirection { get; }
        Space castSpace { get; }
    }
}
