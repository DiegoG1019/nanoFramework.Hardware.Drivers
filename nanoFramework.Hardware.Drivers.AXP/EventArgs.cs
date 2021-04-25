using System;

namespace nanoFramework.Hardware.Drivers
{
    public delegate void ChargingStatusChangedEventHandler(AXP sender, AXPChargingStatusChangedEventArgs eventArgs);
    public class AXPChargingStatusChangedEventArgs : EventArgs
    {
        public bool IsCharging { get; private set; }
        public AXPChargingStatusChangedEventArgs(bool isCharging) => IsCharging = isCharging;
    }

    public delegate void AXPWarningStatusEventHandler(AXP sender, AXPWarningStatusEventArgs eventArgs);
    public class AXPWarningStatusEventArgs : EventArgs
    {
        public AXPWarningStatus WarningStatus { get; private set; }

        public bool AcinOverVoltage => WarningStatus is AXPWarningStatus.AcinOverVoltage;
        public bool VbusOverVoltage => WarningStatus is AXPWarningStatus.VbusOverVoltage;
        public bool VbusLowVHold => WarningStatus is AXPWarningStatus.VbusLowVHold;
        public bool BatteryTempLow => WarningStatus is AXPWarningStatus.BatteryTempLow;
        public bool BatteryTempHigh => WarningStatus is AXPWarningStatus.BatteryTempHigh;

        public AXPWarningStatusEventArgs(AXPWarningStatus warningStatus) => WarningStatus = warningStatus;
    }

    public delegate void AXPPluggedChangedEventHandler(AXP sender, AXPPluggedChangedEventArgs eventArgs);

    public class AXPPluggedChangedEventArgs : EventArgs
    {
        public bool PluggedIn { get; private set; }
        public AXPPluggedChangedEventArgs(bool pluggedIn) => PluggedIn = pluggedIn;
    }

    public enum AXPWarningStatus
    {
        AcinOverVoltage = 0b1,
        VbusOverVoltage = 0b10,
        VbusLowVHold = 0b100,
        BatteryTempLow = 0b1000,
        BatteryTempHigh = 0b10000,
    }
}
