using System;
using System.Device.I2c;
using System.Threading;
using nanoFramework.Hardware.Drivers.Shared;

namespace nanoFramework.Hardware.Drivers
{
    public sealed class AXP202 : AXP
    {
        private static void ForceOpenDCD3(ref byte x) => x |= (1 << AXP202_DCDC3);

        public AXP202(I2cDevice i2c) : base(i2c)
        {
            Initialize();
        }
        public AXP202(ByteTransferCallbackReg8 readCallback, ByteTransferCallbackReg8 writeCallback) : base(readCallback, writeCallback)
        {
            Initialize();
        }

        protected override void InitializeRegisters()
        {
            byte regval = 0;
            ReadByte(AXP202_LDO24OUT_VOL, ref regval);
            regval &= 0xF0;
            regval >>= 4;
            LDO2Voltage_Field = (ushort)(regval * 100u + 1800u);

            byte ldo3out = ReadByte(AXP202_LDO3OUT_VOL);
            regval = ldo3out;
            LDO3Voltage_Field = (regval & 0x80) > 0 ? (ushort)(VbusVoltage * 1000) : (ushort)((regval & 0x7Fu) * 25u + 700u);

            LDO3Mode_Field = ldo3out.IsBitSet(7) ? AXP202LDO3Mode.DCIN : AXP202LDO3Mode.LDO;

            regval = ReadByte(AXP202_POK_SET);
            StartupTime_Field = (AXP202StartupTime)(regval >> 6);
        }

        public override bool ExtenEnabled => IsOpenOREG(AXP202_EXTEN);

        public override bool LDO2Enabled => IsOpenOREG(AXP202_LDO2);

        public override bool LDO3Enabled => IsOpenOREG(AXP202_LDO3);

        public bool LDO4Enabled => IsOpenOREG(AXP202_LDO4);

        public override bool DCDC2Enabled => IsOpenOREG(AXP202_DCDC2);

        public override bool DCDC3Enabled => IsOpenOREG(AXP202_DCDC2);

        public float BatteryChargeCurrent => GetRegisterResult(AXP202_BAT_AVERCHGCUR_H8, AXP202_BAT_AVERCHGCUR_L4) * AXP202_BATT_CHARGE_CUR_STEP;

        /// <summary>
        /// Represented in milliVolts (mV); must be a value between 700mV and 1800mV
        /// </summary>
        public override ushort LDO3Voltage
        {
            get => LDO3Voltage_Field;
            set
            {
                if (value == LDO3Voltage_Field)
                    return;
                if (value > 1800 || value < 700)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Expected a value between 700mV and 1800mV, got {value}mV");
                ReadEmbedAFWrite(AXP202_LDO3OUT_VOL, 0x80, (value - 700u) / 25u, 0);
                LDO3Voltage_Field = value;
            }
        }

        /// <summary>
        /// Sets the AXP Timer
        /// </summary>
        /// <param name="minutes">Cannot exceed 63</param>
        public void SetTimer(byte minutes)
        {
            if (minutes > 63)
                throw new ArgumentOutOfRangeException(nameof(minutes), $"Expected a value between 0 and 63, got: {minutes}");
            WriteByte(AXP202_TIMER_CTL, minutes);
        }

        public void OffTimer() => WriteByte(AXP202_TIMER_CTL, 0x80);

        public void ClearTimer() => ReadEmbedAFWrite(AXP202_TIMER_CTL, byte.MaxValue, 0x80, 0);

        public override void ReadIRQ()
        {
            for (int i = 0; i < 5; i++)
                ReadByte((byte)(AXP202_INTSTS1 + (uint)i), ref Irq[i]);
        }

        public override void ClearIRQ()
        {
            for (int i = 0; i < 5; i++)
                WriteByte(AXP202_INTSTS1, 0xFF);
            base.ClearIRQ();
        }

        public int BatteryPercentage
        {
            get
            {
                if (!IsBatteryConnected)
                    return 0;
                var val = ReadByte(AXP202_BATT_PERCENTAGE);
                return !((val & Bit.BitMask(7)) > 0) ? val & (Bit.InvertByte(Bit.BitMask(7))) : 0;
            }
        }

        public override ushort LDO2Voltage 
        {
            get => base.LDO2Voltage;
            set
            {
                base.LDO2Voltage = value;
                ReadEmbedAFWrite(AXP202_LDO24OUT_VOL, 0x0F, (byte)((value - 1800u) / 100u), 4);
                LDO2Voltage_Field = value;
            }
        }

        public void SetTargetChargingVoltage(AXP202ChargingVoltage targetv)
        {
            if (targetv > AXP202ChargingVoltage.Voltage_4_1V || targetv < AXP202ChargingVoltage.Voltage_4_36V)
                throw new ArgumentOutOfRangeException(nameof(targetv), $"Expected a value between {nameof(AXP202ChargingVoltage.Voltage_4_1V)}({(int)AXP202ChargingVoltage.Voltage_4_1V}) and {nameof(AXP202ChargingVoltage.Voltage_4_36V)}({(int)AXP202ChargingVoltage.Voltage_4_36V}). Got: {targetv}");
            ReadEmbedAFWrite(AXP202_CHARGE1, 0xCF, TargetVolParams[(int)targetv], 0);
        }

        AXP202StartupTime StartupTime_Field;
        public AXP202StartupTime StartupTime
        {
            get => StartupTime_Field;
            set
            {
                if (value == StartupTime_Field)
                    return;
                if (value < 0 || value > AXP202StartupTime.Time128Ms)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Enum member out of range. {value}");
                ReadEmbedAFWrite(AXP202_POK_SET, 0x3F, StartupParams[(int)value], 0);
                StartupTime_Field = value;
            }
        }

        AXP202LDO3Mode LDO3Mode_Field;
        public AXP202LDO3Mode LDO3Mode
        {
            get => LDO3Mode_Field;
            set
            {
                if (value == LDO3Mode_Field)
                    return;
                byte val = 0;
                switch (value)
                {
                    case AXP202LDO3Mode.DCIN:
                        val.Set(7);
                        break;
                    case AXP202LDO3Mode.LDO:
                        val.Clear(7);
                        break;
                }
                WriteByte(AXP202_LDO3OUT_VOL, val);
                LDO3Mode_Field = value;
            }
        }

        AXPTableLDO4 LDO4Voltage_Field;
        public AXPTableLDO4 LDO4Voltage
        {
            get => LDO4Voltage_Field;
            set
            {
                if (value == LDO4Voltage_Field)
                    return;
                if (value > AXPTableLDO4.LDO4_MAX || value < AXPTableLDO4.LDO4_1250MV)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Expected a value between {nameof(AXPTableLDO4.LDO4_1250MV)}({(int)AXPTableLDO4.LDO4_1250MV}) and {nameof(AXPTableLDO4.LDO4_MAX)}({(int)AXPTableLDO4.LDO4_MAX}). Got: {value}");
                ReadEmbedAFWrite(AXP202_LDO24OUT_VOL, 0xF0, (byte)value, 0);
                LDO4Voltage_Field = value;
            }
        }


        static readonly byte[] LDO5Params = new byte[]
        {
            0b11111000, //1.8V
            0b11111001, //2.5V
            0b11111010, //2.8V
            0b11111011, //3.0V
            0b11111100, //3.1V
            0b11111101, //3.3V
            0b11111110, //3.4V
            0b11111111, //3.5V
        };
        AXPVoltageTableLDO5 LDO5Voltage_Field = (AXPVoltageTableLDO5)(255);

        public AXPVoltageTableLDO5 LDO5Voltage
        {
            get => LDO5Voltage_Field;
            set
            {
                if (value == LDO5Voltage_Field)
                    return;
                ReadEmbedAFWrite(AXP202_GPIO0_VOL, 0b11111000, LDO5Params[(int)value], 0);
                LDO5Voltage_Field = value;
            }
        }

        public override void SetGPIOMode(AXP_GPIO gpio, AXP_GPIOMode mode)
        {
            byte val;
            byte rslt;
            switch (gpio)
            {
                case AXP_GPIO.GPIO0:
                    rslt = SelectGPIO0(mode);
                    val = ReadByte(AXP202_GPIO0_CTL);
                    val &= 0b11111000;
                    val |= rslt;
                    WriteByte(AXP202_GPIO0_CTL, val);
                    break;

                case AXP_GPIO.GPIO1:
                    rslt = SelectGPIO1(mode);
                    val = ReadByte(AXP202_GPIO1_CTL);
                    val &= 0b11111000;
                    val |= rslt;
                    WriteByte(AXP202_GPIO1_CTL, val);
                    break;

                case AXP_GPIO.GPIO2:
                    rslt = SelectGPIO2(mode);
                    val = ReadByte(AXP202_GPIO2_CTL);
                    val &= 0b11111000;
                    val |= rslt;
                    WriteByte(AXP202_GPIO2_CTL, val);
                    break;

                case AXP_GPIO.GPIO3:
                    rslt = SelectGPIO3(mode);
                    val = ReadByte(AXP202_GPIO3_CTL);
                    val = rslt > 0 ? (byte)(val | Bit.BitMask(2)) : (byte)(val & (Bit.InvertByte(Bit.BitMask(2))));
                    WriteByte(AXP202_GPIO3_CTL, val);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(gpio), $"Expected a value between GPIO0 and GPIO3, Got: {gpio}");
            }
        }

        private byte SelectGPIO0(AXP_GPIOMode mode)
        {
            switch (mode)
            {
                case AXP_GPIOMode.OutputLow:
                    return 0;
                case AXP_GPIOMode.OutputHigh:
                    return 1;
                case AXP_GPIOMode.Input:
                    return 2;
                case AXP_GPIOMode.LDO:
                    return 3;
                case AXP_GPIOMode.ADC:
                    return 4;
                default:
                    break;
            }
            throw new NotSupportedException($"Mode {mode} is not supportedfor GPIO0");
        }

        private byte SelectGPIO1(AXP_GPIOMode mode)
        {
            switch (mode)
            {
                case AXP_GPIOMode.OutputLow:
                    return 0;
                case AXP_GPIOMode.OutputHigh:
                    return 1;
                case AXP_GPIOMode.Input:
                    return 2;
                case AXP_GPIOMode.ADC:
                    return 4;
                default:
                    break;
            }
            throw new NotSupportedException($"Mode {mode} is not supportedfor GPIO1");
        }

        private byte SelectGPIO2(AXP_GPIOMode mode)
        {
            switch (mode)
            {
                case AXP_GPIOMode.OutputLow:
                    return 0;
                case AXP_GPIOMode.Input:
                    return 2;
                case AXP_GPIOMode.Floating:
                    return 1;
                default:
                    break;
            }
            throw new NotSupportedException($"Mode {mode} is not supportedfor GPIO2");
        }

        private byte SelectGPIO3(AXP_GPIOMode mode)
        {
            switch (mode)
            {
                case AXP_GPIOMode.Input:
                    return 1;
                case AXP_GPIOMode.OpenDrainOutput:
                    return 0;
                default:
                    break;
            }
            throw new NotSupportedException($"Mode {mode} is not supportedfor GPIO3");
        }

        public void SetGPIOIrq(AXP_GPIO gpio, AXP_GPIO_IRQ irq)
        {
            byte reg;
            uint mask = IRQMask(irq);

            switch (gpio)
            {
                case AXP_GPIO.GPIO0:
                    reg = AXP202_GPIO0_CTL;
                    break;
                case AXP_GPIO.GPIO1:
                    reg = AXP202_GPIO1_CTL;
                    break;
                case AXP_GPIO.GPIO2:
                    reg = AXP202_GPIO2_CTL;
                    break;
                case AXP_GPIO.GPIO3:
                    reg = AXP202_GPIO3_CTL;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gpio), $"Expected a value between GPIO0 (0) and GPIO3 (3). Got: {gpio} ((int)gpio)");
            }
            WriteByte(reg, (byte)(mask == 0 ? (ReadByte(reg) & 0b00111111u) : (ReadByte(reg) | mask)));
        }


        public override void GPIOWrite(AXP_GPIO gpio, byte val)
        {
            byte reg;
            byte wVal = 0;
            switch (gpio)
            {
                case AXP_GPIO.GPIO0:
                    reg = AXP202_GPIO0_CTL;
                    break;
                case AXP_GPIO.GPIO1:
                    reg = AXP202_GPIO1_CTL;
                    break;
                case AXP_GPIO.GPIO2:
                    reg = AXP202_GPIO2_CTL;
                    if (val > 0)
                        throw new ArgumentOutOfRangeException(nameof(val), "val must not be greater than 0 for GPIO2");
                    break;
                case AXP_GPIO.GPIO3:
                    if (val > 0)
                        throw new ArgumentOutOfRangeException(nameof(val), "val must not be greater than 0 for GPIO3");
                    ReadByte(AXP202_GPIO3_CTL, ref wVal);
                    wVal &= 0b11111101;
                    WriteByte(AXP202_GPIO3_CTL, wVal);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gpio), $"GPIO Argument out of range. Expected a value between GPIO0 and GPIO3, Got: {gpio}");
            }
            ReadByte(reg, ref wVal);
            wVal = (byte)(val > 0 ? (wVal | 1u) : (wVal & 0b11111000u));
            WriteByte(reg, wVal);
        }

        public override byte GPIORead(AXP_GPIO gpio)
        {

            byte val = 0;
            byte reg = AXP202_GPIO012_SIGNAL;
            byte offset;
            switch (gpio)
            {
                case AXP_GPIO.GPIO0:
                    offset = 4;
                    break;
                case AXP_GPIO.GPIO1:
                    offset = 5;
                    break;
                case AXP_GPIO.GPIO2:
                    offset = 6;
                    break;
                case AXP_GPIO.GPIO3:
                    reg = AXP202_GPIO3_CTL;
                    offset = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gpio), $"GPIO Argument out of range. Expected a value between GPIO0 and GPIO3, Got: {gpio}");
            }
            ReadByte(reg, ref val);
            return (byte)((val & Bit.BitMask(offset)) > 0 ? 1 : 0);
        }

        public override void SetPowerOutput(byte ch, bool en)
        {
            byte val = 0, data = 0;

            ReadByte(AXP202_LDO234_DC23_CTL, ref data);
            if (en)
                data |= (byte)(1u << ch);
            else
                data &= (byte)(~(1u << ch));

            ForceOpenDCD3(ref data);

            WriteByte(AXP202_LDO234_DC23_CTL, data);
            Thread.Sleep(1);
            ReadByte(AXP202_LDO234_DC23_CTL, ref val);
            if (data != val)
                throw new DriverI2cException("Data sent does not equal data read");
            OutputRegister = val;
        }

        public override void LimitingOff()
            => ReadEmbedAFWrite(AXP202_IPS_SET, byte.MaxValue, 0x03, 0);

        protected override void Probe()
        {
            base.Probe();
            byte[] reg = new byte[1];
            if (ChipID == AXP202_CHIP_ID)
            {
                ReadBytes(AXP202_LDO234_DC23_CTL, reg);
                OutputRegister = reg[0];
            }
            else
                throw new InvalidOperationException($"Detected ChipID: {ChipID}. Expected ChipID AXP202: {AXP202_CHIP_ID:#0:X}");
        }
    }
}
