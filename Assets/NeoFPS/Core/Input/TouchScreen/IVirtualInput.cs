
namespace NeoFPS
{
    public interface IVirtualInput
    {
        bool GetVirtualButton(FpsInputButton button);
        float GetVirtualAxis(FpsInputAxis axis);
    }
}
