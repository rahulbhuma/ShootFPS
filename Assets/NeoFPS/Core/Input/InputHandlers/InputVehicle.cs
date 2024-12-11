
namespace NeoFPS
{
    public class InputVehicle : FpsInput
    {
        public override FpsInputContext inputContext
        {
            get { return FpsInputContext.Vehicle; }
        }

        protected override void OnGainFocus()
        {
            base.OnGainFocus();

            // Capture mouse cursor
            NeoFpsInputManagerBase.captureMouseCursor = true;
        }

        protected override void OnEnable()
        {
            //base.OnEnable();

            // Attach this component to the vehicle
            // Call PushContext manually (or via unity events) when entering a vehicle
            // and PopContext when exiting the vehicle
        }

        protected override void OnLoseFocus()
        {
            // Release mouse cursor
            NeoFpsInputManagerBase.captureMouseCursor = false;
        }

        protected override void UpdateInput()
        {
            // Inherit from this class and override this function to add vehicle inputs
        }
    }
}
