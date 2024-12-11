using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    public interface ILoadoutBuilder
    {
        int numLoadoutBuilderSlots
        {
            get;
        }

        ILoadoutBuilderSlot GetLoadoutBuilderSlotInfo(int index);

        FpsInventoryLoadout GetLoadout();
    }

    public interface ILoadoutBuilderSlot
    {
        string displayName
        {
            get;
        }

        int numOptions
        {
            get;
        }

        int currentOption
        {
            get;
            set;
        }

        FpsInventoryItemBase GetOption(int index);
    }
}
