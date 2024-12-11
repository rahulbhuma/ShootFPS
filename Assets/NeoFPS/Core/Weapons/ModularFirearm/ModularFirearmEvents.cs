using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.ModularFirearms
{
    [Serializable]
    public class AttachmentChangedEvent : UnityEvent<ModularFirearmAttachment> { }
}