using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    public interface ILoadoutSelector
    {
        int numLoadouts
        {
            get;
        }

        int currentLoadoutIndex
        {
            get;
            set;
        }

        ILoadoutInfo GetLoadoutInfo(int index);
    }

    public interface ILoadoutInfo
    {
        string displayName
        {
            get;
        }

        string description
        {
            get;
        }

        Sprite sprite
        {
            get;
        }

        // What else might be used here?
    }
}
