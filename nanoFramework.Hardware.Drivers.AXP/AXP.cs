using System;
using System.Threading;
using System.Device.I2c;
using nanoFramework.Hardware.Drivers.Shared;
using System.Runtime.CompilerServices;
using static nanoFramework.Hardware.Drivers.Shared.StringFormats;

namespace nanoFramework.Hardware.Drivers
{
    /// <summary>
    /// An abstract class to drive an X-Power AXP chip.
    /// </summary>
    public abstract partial class AXP
    {

        /*                    Chip resource table
        | CHIP      | AXP173             | AXP192             | AXP202             |
        | --------- | ------------------ | ------------------ | ------------------ |
        | DC1       | 0v7~3v5   @ 1200mA | 0v7~3v5   @ 1200mA | X           X      |
        | DC2       | 0v7~2v275 @ 1600mA | 0v7~2v275 @ 1600mA | 0v7~2v275 @ 1600mA |
        | DC3       | X           X      | 0v7~3v5   @ 700mA  | 0v7~3v5   @ 1200mA |
        | LDO1      | 3v3       @ 30mA   | 3v3       @ 30mA   | 3v3       @ 30mA   |
        | LDO2      | 1v8~3v3   @ 200mA  | 1v8~3v3   @ 200mA  | 1v8~3v3   @ 200mA  |
        | LDO3      | 1v8~3v3   @ 200mA  | 1v8~3v3   @ 200mA  | 0v7~3v3   @ 200mA  |
        | LDO4      | 0v7~3v5   @ 500mA  | X           X      | 1v8~3v3   @ 200mA  |
        | LDO5/IO0  | X           X      | 1v8~3v3   @ 50mA   | 1v8~3v3   @ 50mA   |
                                                                                  */

        protected readonly I2cDevice I2c;
        protected readonly ByteTransferCallbackReg8 ReadCallback;
        protected readonly ByteTransferCallbackReg8 WriteCallback;
        protected readonly bool Callbacks;
        protected readonly byte[] StartupParams = { 0x00, 0x40, 0x80, 0xC0 };
        protected readonly byte[] LongPressParams = { 0x00, 0x10, 0x20, 0x30 };
        protected readonly byte[] ShutDownParams = { 0x00, 0x01, 0x02, 0x03 };
        protected readonly byte[] TargetVolParams = { 0x00, 0x20, 0x40, 0x60 };
        protected readonly byte[] Irq = new byte[5], GPIO = new byte[4];

        protected byte OutputRegister, Address;

        private readonly Thread WatcherThread;

        public event EventHandler PEKShortPressed;
        public event EventHandler PEKLongPressed;
        public event EventHandler TimerTimedOut;
        public event EventHandler ChargingDone;
        /// <summary>
        /// This will be invoked every time the status is checked and a warning should be issued, even if it has already warned.
        /// </summary>
        public event AXPWarningStatusEventHandler StatusWarning;
        public event ChargingStatusChangedEventHandler ChargingStatusChanged;
        public event AXPPluggedChangedEventHandler VBusPluggedChanged;
        public event AXPPluggedChangedEventHandler BatteryPluggedChanged;
        public event AXPPluggedChangedEventHandler AcinPluggedChanged;

        private AXP()
        {
            WatcherThread = new Thread(() =>
            {
                int i = 0;
                while (true)
                {
                    for (; i < 3; i++)
                    {
                        Thread.Sleep(100);
                        QuickWatcher();
                    }
                    SlowWatcher();
                }
            });
        }

        protected internal AXP(I2cDevice i2c) : this()
        {
            Callbacks = false;
            I2c = i2c;
        }

        protected internal AXP(ByteTransferCallbackReg8 readCallback, ByteTransferCallbackReg8 writeCallback) : this()
        {
            Callbacks = true;
            ReadCallback = readCallback;
            WriteCallback = writeCallback;
        }

        protected struct IRQSTATUS_REPORT
        {
            public readonly bool AcinOverVoltage;
            public readonly bool AcinPluggedIn;
            public readonly bool AcinRemoved;
            public readonly bool VBusOverVoltage;
            public readonly bool VBusPluggedIn;
            public readonly bool VBusRemoved;
            public readonly bool VBusLowVHold;
            public readonly bool BatteryPluggedIn;
            public readonly bool BatteryRemoved;
            public readonly bool BatteryEnterActivate;
            public readonly bool BatteryExitActivate;
            public readonly bool Charging;
            public readonly bool ChargingDone;
            public readonly bool BatteryTemperatureLow;
            public readonly bool BatteryTemperatureHigh;
            public readonly bool PEKShortPress;
            public readonly bool PEKLongPress;
            public readonly bool TimerTimeout;

            public static IRQSTATUS_REPORT Previous { get; private set; }

            static IRQSTATUS_REPORT Latest_Field;
            public static IRQSTATUS_REPORT Latest
            {
                get => Latest_Field;
                set
                {
                    Previous = Latest_Field;
                    Latest_Field = value;
                }
            }

            public IRQSTATUS_REPORT(byte[] irq)
            {
                if (irq.Length != 5)
                    throw new ArgumentException($"Argument invalid, expected byte array of length 5, got length of {irq.Length}", nameof(irq));

                AcinOverVoltage = IRQCheckBit(0, 7);
                AcinPluggedIn = IRQCheckBit(0, 6);
                AcinRemoved = IRQCheckBit(0, 5);
                VBusOverVoltage = IRQCheckBit(0, 4);
                VBusPluggedIn = IRQCheckBit(0, 3);
                VBusRemoved = IRQCheckBit(0, 2);
                VBusLowVHold = IRQCheckBit(0, 1);
                BatteryPluggedIn = IRQCheckBit(1, 7);
                BatteryRemoved = IRQCheckBit(1, 6);
                BatteryEnterActivate = IRQCheckBit(1, 5);
                BatteryExitActivate = IRQCheckBit(1, 4);
                Charging = IRQCheckBit(1, 3);
                ChargingDone = IRQCheckBit(1, 2);
                BatteryTemperatureLow = IRQCheckBit(1, 1);
                BatteryTemperatureHigh = IRQCheckBit(1, 0);
                PEKShortPress = IRQCheckBit(2, 1);
                PEKLongPress = IRQCheckBit(2, 0);
                TimerTimeout = IRQCheckBit(4, 7);

                bool IRQCheckBit(int index, byte mask)
                    => ((uint)irq[index] & Bit.BitMask(mask)) > 0;
            }
        }

        bool WasCharging = false;
        /// <summary>
        /// Check status and raise relevant events. Sleeps for 250ms. <b>Make sure to call base.SlowWatcher() if you override this</b>
        /// </summary>
        protected virtual void SlowWatcher()
        {
            var IRQS_prev = IRQSTATUS_REPORT.Previous;
            var IRQS = IRQSTATUS_REPORT.Latest;

            {
                var ischrg = IsCharging;
                if (ischrg && !WasCharging || !ischrg && WasCharging)
                {
                    ChargingStatusChanged?.Invoke(this, new AXPChargingStatusChangedEventArgs(ischrg));
                    WasCharging = ischrg;
                }
            }
            AXPWarningStatus warningStatus =
            (IRQS.AcinOverVoltage ? AXPWarningStatus.AcinOverVoltage : 0) |
            (IRQS.VBusOverVoltage ? AXPWarningStatus.VbusOverVoltage : 0) |
            (IRQS.VBusLowVHold ? AXPWarningStatus.VbusLowVHold : 0) |
            (IRQS.BatteryTemperatureLow ? AXPWarningStatus.BatteryTempLow : 0) |
            (IRQS.BatteryTemperatureHigh ? AXPWarningStatus.BatteryTempHigh : 0);

            if (warningStatus != 0)
                StatusWarning?.Invoke(this, new AXPWarningStatusEventArgs(warningStatus));

            if (IRQS.ChargingDone)
                ChargingDone?.Invoke(this, null);

            if (IRQS.VBusPluggedIn)
                VBusPluggedChanged?.Invoke(this, new AXPPluggedChangedEventArgs(true));
            else if (IRQS.VBusRemoved)
                VBusPluggedChanged?.Invoke(this, new AXPPluggedChangedEventArgs(false));

            if (IRQS.BatteryPluggedIn)
                BatteryPluggedChanged?.Invoke(this, new AXPPluggedChangedEventArgs(true));
            else if (IRQS.BatteryRemoved)
                BatteryPluggedChanged?.Invoke(this, new AXPPluggedChangedEventArgs(false));

            if(IRQS.AcinPluggedIn)
                AcinPluggedChanged?.Invoke(this, new AXPPluggedChangedEventArgs(true));
            else if (IRQS.AcinRemoved)
                AcinPluggedChanged?.Invoke(this, new AXPPluggedChangedEventArgs(false));
        }

        /// <summary>
        /// Check status and raise relevant events. Sleeps for 100ms. <b>Make sure to call base.QuickWatcher() if you override this</b>
        /// </summary>
        protected virtual void QuickWatcher()
        {
            var IRQS = IRQSTATUS_REPORT.Latest = new IRQSTATUS_REPORT(Irq);

            if (IRQS.PEKLongPress)
                PEKLongPressed?.Invoke(this, null);

            if (IRQS.PEKShortPress)
                PEKShortPressed?.Invoke(this, null);

            if (IRQS.TimerTimeout)
                TimerTimedOut?.Invoke(this, null);
        }

        protected bool Initialized { get; private set; }
        protected virtual void Initialize()
        {
            if (Initialized)
                throw new InvalidOperationException("Cannot initialize twice");

            Probe();

            if (ReadByte(AXP202_COULOMB_CTL, ref CoulombRegister_Field) != I2cTransferStatus.FullTransfer)
                throw new DriverI2cException($"Unable to read AXP202_COULOMB_CTL {AXP202_COULOMB_CTL.ToString(HexByteFormat)}");

            if (ReadByte(AXP202_ADC_SPEED, ref TSCache) != I2cTransferStatus.FullTransfer)
                throw new DriverI2cException($"Unable to read AXP202_ADC_SPEED {AXP202_ADC_SPEED.ToString(HexByteFormat)}");

            byte dcdv = 0;
            if (ReadByte(AXP202_DC2OUT_VOL, ref dcdv) != I2cTransferStatus.FullTransfer)
                throw new DriverI2cException($"Unable to read AXP202_DC2OUT_VOL {AXP202_DC2OUT_VOL.ToString(HexByteFormat)}");
            DCD2Voltage_Field = (ushort)(dcdv * 25u + 700u);

            if(ReadByte(AXP202_DC3OUT_VOL, ref dcdv) != I2cTransferStatus.FullTransfer)
                throw new DriverI2cException($"Unable to read AXP202_DC3OUT_VOL {AXP202_DC3OUT_VOL.ToString(HexByteFormat)}");
            DCD3Voltage_Field = (ushort)(dcdv * 25u + 700u);

            if (ReadByte(AXP202_POK_SET, ref dcdv) != I2cTransferStatus.FullTransfer)
                throw new DriverI2cException($"Unable to read AXP202_POK_SET {AXP202_POK_SET.ToString(HexByteFormat)}");
            LongPressTime_Field = (AXPLongPressTime)((byte)((byte)(dcdv << 2) >> 6));
            ShutdownTime_Field = (AXPPowerOffTime)((byte)((byte)(dcdv << 6) >> 6));
            TimeOutShutdown_Field = dcdv.IsBitSet(3);

            if (ReadByte(AXP202_CHARGE1, ref dcdv) != I2cTransferStatus.FullTransfer)
                throw new DriverI2cException($"Unable to read AXP202_POK_SET {AXP202_CHARGE1.ToString(HexByteFormat)}");
            IsChargingEnabled_Field = dcdv.IsBitSet(7);
            ChargeControlCurrent_Field = (AXP1xxChargeCurrent)(byte)(dcdv & 0x0F);

            InitializeRegisters();

            WatcherThread.Start();

            Initialized = true;
        }

        protected abstract void InitializeRegisters();

        public bool IsVBusPlugged => IsOpen(ReadByte(AXP202_STATUS), 5);

        public bool IsCharging => IsOpen(ReadByte(AXP202_MODE_CHGSTATUS), 6);

        public bool IsBatteryConnected => IsOpen(ReadByte(AXP202_MODE_CHGSTATUS), 5);

        bool IsChargingEnabled_Field;
        public bool IsChargingEnabled
        {
            get => IsChargingEnabled_Field;
            set
            {
                if (value == IsChargingEnabled_Field)
                    return;
                if (value)
                {
                    ReadSetWrite(AXP202_CHARGE1, 7);
                    return;
                }
                ReadClearWrite(AXP202_CHARGE1, 7);
            }
        }

        private byte TSCache;

        public AXPSamplingRateADC ADCSamplingRate
        {
            get
            {
                uint val = ((uint)TSCache & 0xC0) >> 6;
                return (AXPSamplingRateADC)(25 * (int)((float)val * val));
            }
            set => EmbedTSValue(0x3F, (uint)value, 6);
        }

        public void SetTSPinCurrent(AXP_TSPinCurrent current) => EmbedTSValue(0xCF, (uint)current, 4);

        public void SetTSPinFunction(AXP_TSPinFunction func) => EmbedTSValue(0xFA, (uint)func, 2);

        public void DisableADC1(AXP202FunctionsADC1 parameters) => WriteByte(AXP202_ADC_EN1, DisableFlag(AXP202_ADC_EN1, (byte)parameters));
        public void EnableADC1(AXP202FunctionsADC1 parameters) => WriteByte(AXP202_ADC_EN1, EnableFlag(AXP202_ADC_EN1, (byte)parameters));

        public void DisableADC2(AXP202FunctionsADC2 parameters) => WriteByte(AXP202_ADC_EN2, DisableFlag(AXP202_ADC_EN2, (byte)parameters));
        public void EnableADC2(AXP202FunctionsADC2 parameters) => WriteByte(AXP202_ADC_EN2, EnableFlag(AXP202_ADC_EN2, (byte)parameters));

        public void EnableIRQ(AXPIRQ irq)
        {
            byte param = 0;
            if (check(0xFFul, 0))
                WriteByte(AXP202_INTEN1, EnableFlag(AXP202_INTEN1, (byte)(((uint)irq) & 0xFFu)));
            if (check(0xFF00ul, 8))
                WriteByte(AXP202_INTEN2, EnableFlag(AXP202_INTEN2, param));
            if (check(0xFF0000ul, 16))
                WriteByte(AXP202_INTEN3, EnableFlag(AXP202_INTEN3, param));
            if (check(0xFF000000ul, 24))
                WriteByte(AXP202_INTEN4, EnableFlag(AXP202_INTEN4, param));
            if (check(0xFF00000000ul, 32))
                WriteByte(AXP202_INTEN5, EnableFlag(AXP202_INTEN5, param));

            bool check(ulong mask, int shift)
            {
                param = (byte)((uint)irq >> shift);
                return ((ulong)irq & mask) > 0;
            }
        }

        public void DisableIRQ(AXPIRQ irq)
        {
            byte param = 0;
            if (check(0xFFul, 0))
                WriteByte(AXP202_INTEN1, DisableFlag(AXP202_INTEN1, (byte)(((uint)irq) & 0xFFu)));
            if (check(0xFF00ul, 8))
                WriteByte(AXP202_INTEN2, DisableFlag(AXP202_INTEN2, param));
            if (check(0xFF0000ul, 16))
                WriteByte(AXP202_INTEN3, DisableFlag(AXP202_INTEN3, param));
            if (check(0xFF000000ul, 24))
                WriteByte(AXP202_INTEN4, DisableFlag(AXP202_INTEN4, param));
            if (check(0xFF00000000ul, 32))
                WriteByte(AXP202_INTEN5, DisableFlag(AXP202_INTEN5, param));

            bool check(ulong mask, int shift)
            {
                param = (byte)((uint)irq >> shift);
                return ((ulong)irq & mask) > 0;
            }
        }

        public virtual void ClearIRQ()
        {
            for (int i = 0; i < Irq.Length; i++)
                Irq[0] = 0;
        }

        public abstract void ReadIRQ();

        AXPLongPressTime LongPressTime_Field;
        public AXPLongPressTime LongPressTime
        {
            get => LongPressTime_Field;
            set
            {
                if (value == LongPressTime_Field)
                    return;
                if (value > AXPLongPressTime.Time2s5 || value < AXPLongPressTime.Time1s)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Expected a value between {nameof(AXPLongPressTime.Time1s)}({(int)AXPLongPressTime.Time1s}) and {nameof(AXPLongPressTime.Time2s5)}({(int)AXPLongPressTime.Time2s5}). Got: {value}");
                ReadEmbedAFWrite(AXP202_POK_SET, 0xCF, LongPressParams[(int)value], 0);
                LongPressTime_Field = value;
            }
        }

        AXPPowerOffTime ShutdownTime_Field;
        public AXPPowerOffTime ShutdownTime
        {
            get => ShutdownTime_Field;
            set
            {
                if (value == ShutdownTime_Field)
                    return;
                if (value > AXPPowerOffTime.Time16s || value < AXPPowerOffTime.Time4s)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Expected a value between {nameof(AXPPowerOffTime.Time4s)}({(int)AXPPowerOffTime.Time4s}) and {nameof(AXPPowerOffTime.Time16s)}({(int)AXPPowerOffTime.Time16s}). Got: {value}");
                ReadEmbedAFWrite(AXP202_POK_SET, 0xFC, ShutDownParams[(int)value], 0);
                ShutdownTime_Field = value;
            }
        }

        bool TimeOutShutdown_Field;
        public bool TimeOutShutdown
        {
            get => TimeOutShutdown_Field;
            set
            {
                if (value == TimeOutShutdown_Field)
                    return;
                var b = ReadByte(AXP202_POK_SET);
                if (value)
                    b.Set(3);
                else
                    b.Clear(3);
                WriteByte(AXP202_POK_SET, b);
                TimeOutShutdown_Field = value;
            }
        }

        AXP1xxChargeCurrent ChargeControlCurrent_Field;
        /// <summary>
        /// Described in mA, ranging between 300mA - 1800mA
        /// </summary>
        public virtual AXP1xxChargeCurrent ChargeControlCurrent
        {
            get => ChargeControlCurrent_Field;
            set
            {
                if (value == ChargeControlCurrent_Field)
                    return;
                if (value > AXP1xxChargeCurrent.Current_1320MA)
                    value = AXP1xxChargeCurrent.Current_1320MA;
                
                ReadEmbedAFWrite(AXP202_CHARGE1, 0xF0, (byte)value, 0);
                ChargeControlCurrent_Field = value;
            }
        }

        ushort DCD2Voltage_Field;
        /// <summary>
        /// Represented in milliVolts (mV); must be a value between 700mV and 2275mV
        /// </summary>
        public ushort DCDC2Voltage
        {
            get => DCD2Voltage_Field;
            set
            {
                if (value == DCD2Voltage_Field)
                    return;
                if (value > 2275 || value < 700)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Expected a value between 700mV and 2275mV, got {value}mV");
                WriteByte(AXP202_DC2OUT_VOL, (byte)((value - 700u) / 25u));
                DCD2Voltage_Field = value;
            }
        }

        ushort DCD3Voltage_Field;
        /// <summary>
        /// Represented in milliVolts (mV); must be a value between 700mV and 3500mV
        /// </summary>
        public virtual ushort DCD3Voltage
        {
            get => DCD3Voltage_Field;
            set
            {
                if (value == DCD3Voltage_Field)
                    return;
                if(value > 3500 || value < 700)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Expected a value between 700mV and 3500mV, got {value}mV");
                WriteByte(AXP202_DC3OUT_VOL, (byte)((value - 700u) / 25u));
                DCD3Voltage_Field = value;
            }
        }

        protected ushort LDO2Voltage_Field;
        /// <summary>
        /// Represented in milliVolts (mV); must be a value between 1800mV and 3300mV
        /// </summary>
        public virtual ushort LDO2Voltage
        {
            get => LDO2Voltage_Field;
            set
            {
                if (value == LDO2Voltage_Field)
                    return;
                if (value > 3300 || value < 1800)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Expected a value between 1800mV and 3300mV, got {value}mV");
            }
        }

        protected ushort LDO3Voltage_Field;
        public abstract ushort LDO3Voltage { get; set; }

        public void SetChargeLEDMode(AXPChargeLEDMode mode)
        {
            byte val = ReadByte(AXP202_OFF_CTL);
            val &= 0b11001111;
            val |= Bit.BitMask(3);
            switch (mode)
            {
                case AXPChargeLEDMode.Off:
                    break;
                case AXPChargeLEDMode.Blink_1Hz:
                    val.Set(4);
                    break;
                case AXPChargeLEDMode.Blink_4Hz:
                    val.Set(5);
                    break;
                case AXPChargeLEDMode.LowLevel:
                    val |= 0b00110000;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), $"Expected an enum value between {nameof(AXPChargeLEDMode.Off)}({(int)AXPChargeLEDMode.Off}) and {nameof(AXPChargeLEDMode.LowLevel)}({(int)AXPChargeLEDMode.LowLevel}), Got: {mode}");
            }
            WriteByte(AXP202_OFF_CTL, val);
        }

        byte CoulombRegister_Field;
        public byte CoulombRegister
        {
            get => CoulombRegister_Field;
            set
            {
                if (value == CoulombRegister_Field)
                    return;
                WriteByte(AXP202_COULOMB_CTL, value);
                CoulombRegister_Field = value;
            }
        }

        public void EnableCoulombCounter()
            => CoulombRegister = 0x80;

        public void DisableCoulombCounter()
            => CoulombRegister = 0x00;

        public void StopCoulombCounter()
            => CoulombRegister = 0xB8;

        public void ClearCoulombCounter()
            => CoulombRegister = 0xA0;

        public abstract void LimitingOff();

        public byte ChipID { get; protected set; }

        public abstract bool LDO2Enabled { get; }

        public abstract bool LDO3Enabled { get; }

        public abstract bool DCDC3Enabled { get; }

        public abstract bool DCDC2Enabled { get; }

        public abstract bool ExtenEnabled { get; }

        public float AcinVoltage => GetRegisterResult(AXP202_ACIN_VOL_H8, AXP202_ACIN_VOL_L4) * AXP202_ACIN_VOLTAGE_STEP;

        public float AcinCurrent => GetRegisterResult(AXP202_ACIN_CUR_H8, AXP202_ACIN_CUR_L4) * AXP202_ACIN_CUR_STEP;

        public float VbusVoltage => GetRegisterResult(AXP202_VBUS_VOL_H8, AXP202_VBUS_VOL_L4) * AXP202_VBUS_VOLTAGE_STEP;

        public float VbusCurrent => GetRegisterResult(AXP202_VBUS_CUR_H8, AXP202_VBUS_CUR_L4) * AXP202_VBUS_CUR_STEP;

        public float Temperature => GetRegisterResult(AXP202_INTERNAL_TEMP_H8, AXP202_INTERNAL_TEMP_L4) * AXP202_INTERNAL_TEMP_STEP + AXP202_INTERNAL_TEMP_MIN;

        public float TSTemperature => GetRegisterResult(AXP202_TS_IN_H8, AXP202_TS_IN_L4) * AXP202_TS_PIN_OUT_STEP;

        public float GPIO0Voltage => GetRegisterResult(AXP202_GPIO0_VOL_ADC_H8, AXP202_GPIO0_VOL_ADC_L4) * AXP202_GPIO0_STEP;

        public float GPIO1Voltage => GetRegisterResult(AXP202_GPIO1_VOL_ADC_H8, AXP202_GPIO1_VOL_ADC_L4) * AXP202_GPIO1_STEP;

        public float BatteryInPower
            => 2 * ((ReadByte(AXP202_BAT_POWERH8) << 16) | (ReadByte(AXP202_BAT_POWERM8) << 8) | ReadByte(AXP202_BAT_POWERL8)) * 1.1f * .5f / 1000;

        public float BatteryVoltage => GetRegisterResult(AXP202_BAT_AVERVOL_H8, AXP202_BAT_AVERVOL_L4) * AXP202_BATT_VOLTAGE_STEP;

        public float BatteryDischargeCurrent => GetRegisterH8L5(AXP202_BAT_AVERDISCHGCUR_H8, AXP202_BAT_AVERDISCHGCUR_L5) * AXP202_BATT_DISCHARGE_CUR_STEP;

        public float SysIPSOUTVoltage => GetRegisterH8L5(AXP202_APS_AVERVOL_H8, AXP202_APS_AVERVOL_L4);

        readonly byte[] BattCChargeBuffer = new byte[4];
        public uint BatteryChargeCoulombs
        {
            get
            {
                lock (BattCChargeBuffer)
                {
                    ReadBytes(AXP202_BAT_CHGCOULOMB3, BattCChargeBuffer);
                    return ((uint)BattCChargeBuffer[0] << 24) + ((uint)BattCChargeBuffer[1] << 16) + ((uint)BattCChargeBuffer[2] << 8) + BattCChargeBuffer[3];
                }
            }
        }

        readonly byte[] BattCDischargeBuffer = new byte[4];
        public uint BatteryDischargeCoulombs
        {
            get
            {
                lock (BattCDischargeBuffer)
                {
                    ReadBytes(AXP202_BAT_DISCHGCOULOMB3, BattCDischargeBuffer);
                    return ((uint)BattCDischargeBuffer[0] << 24) + ((uint)BattCDischargeBuffer[1] << 16) + ((uint)BattCDischargeBuffer[2] << 8) + BattCDischargeBuffer[3];
                }
            }
        }

        public ushort SettingChargeCurrent => (ushort)((ReadByte(AXP202_CHARGE1) & 0b00000111u) + 300u * 100u);

        public float CoulombData
            => 65536f * .5f * (BatteryChargeCoulombs - BatteryDischargeCoulombs) / 3600f / (byte)ADCSamplingRate;

        public void SetTSMode(AXP_TSPinMode mode)
        {
            if (mode > AXP_TSPinMode.Enable)
                throw new ArgumentOutOfRangeException($"Expected an AXP_TSPinMode enum value between 0 and 3, Got: {mode}");
            ReadEmbedAFWrite(AXP202_ADC_SPEED, 0xFC, (byte)mode, 0);
            if (mode == AXP_TSPinMode.Disable)
            {
                DisableADC1(AXP202FunctionsADC1.TSPin);
                return;
            }
            EnableADC1(AXP202FunctionsADC1.TSPin);
        }

        public void Shutdown() => ReadSetWrite(AXP202_OFF_CTL, 7);

        public abstract void SetPowerOutput(byte ch, bool en);

        public abstract void SetGPIOMode(AXP_GPIO gpio, AXP_GPIOMode mode);

        public abstract void GPIOWrite(AXP_GPIO gpio, byte vol);

        public abstract byte GPIORead(AXP_GPIO gpio);

        protected ushort GetRegisterH8L5(byte regh8, byte regl5)
        {
            byte[] hv = { 0 }, lv = { 0 };
            ReadBytes(regh8, hv);
            ReadBytes(regl5, lv);
            return (ushort)(((uint)hv[0] << 5) | ((uint)lv[0] & 0x1F));
        }

        private readonly byte[] Hv = { 0 };
        private readonly byte[] Lv = { 0 };
        protected ushort GetRegisterResult(byte regh8, byte regl4)
        {
            lock(Hv)
                lock (Lv)
                {
                    byte[] hv = { 0 }, lv = { 0 };
                    if (ReadBytes(regh8, hv) == I2cTransferStatus.FullTransfer)
                        if (ReadBytes(regl4, lv) == I2cTransferStatus.FullTransfer)
                            return (ushort)(((uint)hv[0] << 4) | ((uint)lv[0] & 0x0F));
                        else
                            throw new DriverI2cException($"Couldn't succesfully read register Regl4 ({regl4.ToString(HexByteFormat)})");
                    throw new DriverI2cException($"Couldn't succesfully read register Regh8 ({regh8.ToString(HexByteFormat)})");
                }
        }

        readonly byte[] ReadByteDat = new byte[1];
        protected byte ReadByte(byte register)
        {
            lock (ReadByteDat)
            {
                var st = ReadBytes(register, ReadByteDat);
                return st == I2cTransferStatus.FullTransfer ? ReadByteDat[0] :
                throw new DriverI2cException($"Unable to read requested byte. I2cTransferStatus: {st}");
            }
        }

        protected I2cTransferStatus ReadByte(byte register, ref byte data)
        {
            lock (ReadByteDat)
            {
                var st = ReadBytes(register, ReadByteDat);
                data = ReadByteDat[0];
                return st;
            }
        }

        protected I2cTransferStatus ReadBytes(byte register, byte[] data)
        {
            IsInit();
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            if (Callbacks)
                return ReadCallback(I2c, register, data);

            var st = I2c.WriteByte(register).Status;
            return st == I2cTransferStatus.FullTransfer ? I2c.Read(data).Status : st;
        }

        readonly byte[] WriteByteDat = new byte[1];
        protected I2cTransferStatus WriteByte(byte register, byte data)
        {
            lock (WriteByteDat)
            {
                WriteByteDat[0] = data;
                return WriteBytes(register, WriteByteDat);
            }
        }

        protected I2cTransferStatus WriteBytes(byte register, byte[] data)
        {
            IsInit();
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            if (Callbacks)
                return WriteCallback(I2c, register, data);

            var st = I2c.WriteByte(register).Status;
            return st == I2cTransferStatus.FullTransfer ? I2c.Write(data).Status : st;
        }

        protected virtual void Probe()
        {
            byte[] id = new byte[1];
            var st = ReadBytes(AXP202_IC_TYPE, id);
            if (st == I2cTransferStatus.FullTransfer)
                ChipID = id[0];
            throw new DriverI2cException($"Error probing AXP device. I2c Transfer Status: {st}");
        }

        protected byte IRQMask(AXP_GPIO_IRQ irq)
        {
            switch (irq)
            {
                case AXP_GPIO_IRQ.None:
                    return 0;
                case AXP_GPIO_IRQ.Rising:
                    return Bit.BitMask(7);
                case AXP_GPIO_IRQ.Falling:
                    return Bit.BitMask(6);
                case AXP_GPIO_IRQ.DoubleEdge:
                    return 0b1100000;
                default:
                    break;
            }
            throw new NotSupportedException($"Mask {irq} is not supportedfor IRQ");
        }

        protected void EmbedTSValue(byte mask, uint rw, int shift)
        {
            byte val = EmbedValueAF(TSCache, mask, rw, shift);
            WriteByte(AXP202_ADC_SPEED, val);
            TSCache = val;
        }

        protected void ReadEmbedAFWrite(byte register, byte mask, uint rw, int shift)
            => WriteByte(register, EmbedValueAF(ReadByte(register), mask, rw, shift));

        protected byte EmbedValueAF(byte val, byte mask, uint rw, int shift)
        {
            val &= mask;
            val |= (byte)(rw << shift);
            return val;
        }

        protected byte EmbedValueOF(byte val, byte mask, uint rw, int shift)
        {
            val |= (byte)(rw << shift);
            val &= mask;
            return val;
        }

        protected void ReadSetWrite(byte register, int bit)
            => WriteByte(register, Bit.Set(ReadByte(register), bit));

        protected void ReadClearWrite(byte register, int bit)
            => WriteByte(register, Bit.Clear(ReadByte(register), bit));

        protected byte EnableFlag(byte register, byte mask)
            => (byte)(ReadByte(register) | (uint)mask);

        protected byte DisableFlag(byte register, byte mask)
            => (byte)(ReadByte(register) & (~(uint)mask));

        protected bool IsInit() => Initialized ? true : throw new InvalidOperationException("Cannot Manipulate the object when it is not initialized");
    }
}
