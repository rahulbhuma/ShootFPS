using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    public interface ISpawnZoneSelector
    {
        Sprite mapSprite
        {
            get;
        }

        int numSpawnZones
        {
            get;
        }

        int currentSpawnZoneIndex
        {
            get;
            set;
        }

        ISpawnZoneInfo GetSpawnZoneInfo(int index);
    }

    public interface ISpawnZoneInfo
    {
        string displayName
        {
            get;
        }

        Vector2 mapPosition
        {
            get;
        }

        // What else might be used here?
    }
}
