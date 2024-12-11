using System;
using UnityEngine;


namespace NeoFPS
{
    [Serializable]
    public struct FpsInventoryInstantUseActionCollection
    {
        [Tooltip("The actions to perform when the instant use item is triggered.")]
        public FpsInventoryInstantUseAction[] actions;

        public bool canUse
        {
            get
            {
                for (int i = 0; i < actions.Length; ++i)
                {
                    if (actions[i] != null && !actions[i].canUse)
                        return false;
                }
                return true;
            }
        }

        public void Initialise(FpsInventoryItemBase item)
        {
            for (int i = 0; i < actions.Length; ++i)
                actions[i]?.Initialise(item);
        }

        public void PerformActions()
        {
            for (int i = 0; i < actions.Length; ++i)
                actions[i]?.PerformAction();
        }
    }
}