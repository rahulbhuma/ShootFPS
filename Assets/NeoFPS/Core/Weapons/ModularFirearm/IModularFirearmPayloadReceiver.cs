using System;
using NeoFPS;

namespace NeoFPS.ModularFirearms
{
    public interface IModularFirearmPayloadReceiver
    {
        void SetStartupPayload(ModularFirearmPayload payload);
    }
}
