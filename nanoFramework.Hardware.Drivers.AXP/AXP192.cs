using System;
using System.Device.I2c;
using System.Threading;
using nanoFramework.Hardware.Drivers.Shared;

namespace nanoFramework.Hardware.Drivers
{
    public sealed class AXP192 : AXP
    {
        private static void ForcedOpenDCD3(ref byte x) => x |= (1 << AXP192_DCDC3);

        bool DCD1Enabled_Field = false;
        public bool DCD1Enabled
        {
            get => DCD1Enabled_Field;
            set
            {
                if (value == DCD1Enabled_Field)
                    return;
#warning Not Defined
            }
        }

        public AXP192(I2cDevice i2c) : base(i2c)
        {
            Initialize();
        }
        public AXP192(ByteTransferCallbackReg8 readCallback, ByteTransferCallbackReg8 writeCallback) : base(readCallback, writeCallback)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            throw new NotImplementedException("Driver class for AXP192 has not been fully implemented");
            base.Initialize();
            DCD1Enabled_Field = IsOpen(OutputRegister, AXP192_DCDC1);
        }

        protected override void InitializeRegisters()
        {
            byte ldo23read = ReadByte(AXP192_LDO23OUT_VOL);
            byte regval = ldo23read;
            regval &= 0xF0;
            regval >>= 4;
            LDO2Voltage_Field = (ushort)(regval * 100u + 1800u);

            regval = ldo23read;
            LDO3Voltage_Field = (ushort)((regval & 0x0Fu) * 100u + 1800u);
        }

    /// <summary>
    /// Represented in milliVolts (mV); must be a value between 1800mV and 3300mV
    /// </summary>
    public override ushort LDO3Voltage
        {
            get => LDO3Voltage_Field;
            set
            {
                if (value == LDO3Voltage_Field)
                    return;
                if (value > 3300 || value < 1800)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Expected a value between 1800mV and 3300mV, got {value}mV");
                ReadEmbedAFWrite(AXP192_LDO23OUT_VOL, 0xF0, (value - 1800u) / 100u, 0);
                LDO3Voltage_Field = value;
            }
        }

        public override bool ExtenEnabled => IsOpenOREG(AXP192_EXTEN);

        public override bool LDO2Enabled => IsOpenOREG(AXP192_LDO2);

        public override bool LDO3Enabled => IsOpenOREG(AXP192_LDO3);

        public override bool DCDC2Enabled => IsOpenOREG(AXP192_DCDC2);

        public override bool DCDC3Enabled => IsOpenOREG(AXP192_DCDC2);

        public float BatteryChargeCurrent => GetRegisterH8L5(AXP202_BAT_AVERCHGCUR_H8, AXP202_BAT_AVERCHGCUR_L5) * AXP202_BATT_CHARGE_CUR_STEP;

        public override void ReadIRQ()
        {
            for (int i = 0; i < 4; i++)
                ReadByte((byte)(AXP192_INTSTS1 + (uint)i), ref Irq[i]);
            ReadByte(AXP192_INTSTS1, ref Irq[4]);
        }


        public override void LimitingOff()
            => ReadEmbedAFWrite(AXP202_IPS_SET, 0x02, byte.MinValue, 0);

        public override void SetPowerOutput(byte ch, bool en)
        {
            byte val = 0, data = 0;

            ReadByte(AXP202_LDO234_DC23_CTL, ref data);
            if (en)
                data |= (byte)(1u << ch);
            else
                data &= (byte)(~(1u << ch));

            WriteByte(AXP202_LDO234_DC23_CTL, data);
            Thread.Sleep(1);
            ReadByte(AXP202_LDO234_DC23_CTL, ref val);
            if (data != val)
                throw new DriverI2cException("Data sent does not equal data read");
            OutputRegister = val;
        }

        public override void ClearIRQ()
        {
            for (int i = 0; i < 4; i++)
                WriteByte(AXP192_INTSTS1, 0xFF);
            WriteByte(AXP192_INTSTS5, 0xFF);
            base.ClearIRQ();
        }

        public override ushort LDO2Voltage
        {
            get => base.LDO2Voltage;
            set
            {
                base.LDO2Voltage = value;
                ReadEmbedAFWrite(AXP192_LDO23OUT_VOL, 0x0F, (byte)((value - 1800u) / 100u), 4);
                LDO2Voltage_Field = value;
            }
        }

#warning Not defined
        public AXP192StartupTime StartupTime { get; set; }

#warning Not defined
        public ushort DCDC1Voltage { get; set; }

#warning Not defined
        public AXP1xxChargeCurrent ChargeControlCurrent { get; set; }

#warning Not defined
        private void SetGPIO(AXP_GPIO gpio, AXP_GPIOMode mode) { }

        private byte SelectGPIO0(AXP_GPIOMode mode)
        {
            switch (mode)
            {
                case AXP_GPIOMode.OutputLow:
                    return 0b101;
                case AXP_GPIOMode.Input:
                    return 0b001;
                case AXP_GPIOMode.LDO:
                    return 0b010;
                case AXP_GPIOMode.ADC:
                    return 0b100;
                case AXP_GPIOMode.Floating:
                    return 0b111;
                case AXP_GPIOMode.OpenDrainOutput:
                    return 0;
                case AXP_GPIOMode.OutputHigh:
                case AXP_GPIOMode.PWMOutput:
                default:
                    throw new NotSupportedException($"GPIO {mode} is not supported for GPIO0");
            }
        }

        private int SelectGPIO1(AXP_GPIOMode mode)
        {
            switch (mode)
            {
                case AXP_GPIOMode.OutputLow:
                    return 0b101;
                case AXP_GPIOMode.Input:
                    return 0b001;
                case AXP_GPIOMode.ADC:
                    return 0b100;
                case AXP_GPIOMode.Floating:
                    return 0b111;
                case AXP_GPIOMode.OpenDrainOutput:
                    return 0;
                case AXP_GPIOMode.PWMOutput:
                    return 0b010;
                case AXP_GPIOMode.LDO:
                case AXP_GPIOMode.OutputHigh:
                default:
                    throw new NotSupportedException($"GPIO {mode} is not supported for GPIO0");
            }
        }

        private int SelectGPIO3(AXP_GPIOMode mode)
        {
            switch (mode)
            {
                case AXP_GPIOMode.ExternChargingCtrl:
                    return 0;
                case AXP_GPIOMode.OpenDrainOutput:
                    return 1;
                case AXP_GPIOMode.Input:
                    return 2;
                default:
                    throw new NotSupportedException($"GPIO {mode} is not supported for GPIO0");
            }
        }

        private int SelectGPIO4(AXP_GPIOMode mode)
        {
            switch (mode)
            {
                case AXP_GPIOMode.ExternChargingCtrl:
                    return 0;
                case AXP_GPIOMode.OpenDrainOutput:
                    return 1;
                case AXP_GPIOMode.Input:
                    return 2;
                case AXP_GPIOMode.ADC:
                    return 3;
                default:
                    throw new NotSupportedException($"GPIO {mode} is not supported for GPIO0");
            }
        }

        protected override void Probe()
        {
            base.Probe();

            byte[] reg = new byte[1];
            if (ChipID == AXP192_CHIP_ID)
            {
                ReadBytes(AXP202_LDO234_DC23_CTL, reg);
                OutputRegister = reg[0];
            }
            else
                throw new InvalidOperationException($"Detected ChipID: {ChipID}. Expected ChipID AXP192: {AXP192_CHIP_ID:#0:X}");
        }

        public override void SetGPIOMode(AXP_GPIO gpio, AXP_GPIOMode mode)
        {
            throw new NotImplementedException();
        }

        public override byte GPIORead(AXP_GPIO gpio)
        {
            throw new NotImplementedException();
        }

        public override void GPIOWrite(AXP_GPIO gpio, byte vol)
        {
            throw new NotImplementedException();
        }
    }
}
