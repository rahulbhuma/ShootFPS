using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System;

namespace NeoFPS
{
    public abstract class FpsInventoryInstantUseAction : MonoBehaviour
    {
        protected FpsInventoryItemBase item
        {
            get;
            private set;
        }

        public virtual bool canUse
        {
            get { return true; }
        }

        public abstract void PerformAction();

        public virtual void Initialise(FpsInventoryItemBase item)
        {
            this.item = item;
        }
    }
}