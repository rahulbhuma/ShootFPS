using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    public interface ICharacterSelector
    {
        int numCharacters
        {
            get;
        }

        int currentCharacterIndex
        {
            get;
            set;
        }

        ICharacterInfo GetCharacterInfo(int index);
    }

    public interface ICharacterInfo
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
