using System.Device.I2c;

namespace nanoFramework.Hardware.Drivers
{
    public enum FocaltechGesture : byte
    {
        None,
        MoveUp,
        MoveLeft,
        MoveDown,
        MoveRight,
        ZoomIn,
        ZoomOut
    }

    public enum FocaltechEventFlag : byte
    {
        PutDown,
        PutUp,
        Contact,
        None
    }

    public enum FocaltechPowerMode : byte
    {
        /// <summary>
        /// Active mode. ~4mA
        /// </summary>
        Active = 0,
        /// <summary>
        /// Monitoring mode. ~3mA
        /// </summary>
        Monitor = 1,
        /// <summary>
        /// Deep Sleep mode. ~100uA. The reset pin must be pulled low to wake
        /// </summary>
        DeepSleep = 3
    }
}
