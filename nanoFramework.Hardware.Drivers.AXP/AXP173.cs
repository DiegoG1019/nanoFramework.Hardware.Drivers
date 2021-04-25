using System;
using System.Device.I2c;
using System.Threading;
using nanoFramework.Hardware.Drivers.Shared;
using System.Runtime;

namespace nanoFramework.Hardware.Drivers
{
    public sealed class AXP173 : AXP
    {
        public AXP173(I2cDevice i2c) : base(i2c)
        {
            Initialize();
        }
        public AXP173(ByteTransferCallbackReg8 readCallback, ByteTransferCallbackReg8 writeCallback) : base(readCallback, writeCallback)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            throw new NotImplementedException("Driver class for AXP173 has not been fully implemented");
            base.Initialize();
            DCD1Enabled_Field = IsOpen(OutputRegister, AXP173_DCDC1);
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

            LDO4Voltage_Field = (ushort)(ReadByte(AXP173_LDO4_VOLTAGE) * 25u + 700u);
        }

        public override void LimitingOff()
            => ReadEmbedAFWrite(AXP202_IPS_SET, 0x02, byte.MinValue, 0);

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

        readonly byte[] ExtenEnabled_Dat = new byte[1];
        public override bool ExtenEnabled
        {
            get
            {
                var st = ReadBytes(AXP173_EXTEN_DC2_CTL, ExtenEnabled_Dat);
                return st == I2cTransferStatus.FullTransfer ?
                    IsOpen(ExtenEnabled_Dat[0], AXP173_CTL_EXTEN_BIT) : 
                    throw new DriverI2cException($"Unable to read AXP173_CTL_EXTEN_BIT: {st}");
            }
        }

        public override bool LDO2Enabled => IsOpenOREG(AXP173_LDO2);

        public override bool LDO3Enabled => IsOpenOREG(AXP173_LDO3);

        public bool LDO4Enabled => IsOpenOREG(AXP173_LDO4);

        public override bool DCDC2Enabled => IsOpenOREG(AXP173_DCDC2);

        public override void SetPowerOutput(byte ch, bool en)
        {
            byte val = 0, data = 0;

            ReadByte(AXP173_EXTEN_DC2_CTL, ref data);
            if ((ch & AXP173_DCDC2) > 0)
            {
                data = en ? (byte)(data | (uint)Bit.BitMask(AXP173_CTL_DC2_BIT)) : (byte)(data & (~(uint)Bit.BitMask(AXP173_CTL_DC2_BIT)));
                ch &= (byte)(~(uint)Bit.BitMask(AXP173_DCDC2));
                WriteByte(AXP173_EXTEN_DC2_CTL, data);
            }
            else if ((ch & (uint)AXP173_EXTEN) > 0)
            {
                data = en ? (byte)(data | (uint)Bit.BitMask(AXP173_CTL_EXTEN_BIT)) : (byte)(data & (~(uint)Bit.BitMask(AXP173_CTL_EXTEN_BIT)));
                ch &= (byte)(~(uint)Bit.BitMask(AXP173_EXTEN));
                WriteByte(AXP173_EXTEN_DC2_CTL, data);
            }

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

        public override bool DCDC3Enabled => IsOpen(ReadByte(AXP173_EXTEN_DC2_CTL), AXP173_CTL_DC2_BIT);

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

        public override ushort DCD3Voltage
        {
            get => (ushort)(ThrowNotSupported(nameof(DCD3Voltage)) ? 0u : 0u);
            set => ThrowNotSupported(nameof(DCD3Voltage));
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

        private bool ThrowNotSupported(/*[CallerMemberName]*/string name)
            => Initialized ? throw new NotSupportedException($"Method or Property {name} is not supported for AXP173") : true;

#warning Not defined
        public ushort DCDC1Voltage { get; set; }

        ushort LDO4Voltage_Field;
        public ushort LDO4Voltage
        {
            get => LDO4Voltage_Field;
            set
            {
                if (value == LDO4Voltage_Field)
                    return;
                if (value > 3500 || value < 700)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Expected a value between 700mV and 3500mV, got {value}mV");
                WriteByte(AXP173_LDO4_VOLTAGE, (byte)((value - 700u) / 25u));
                LDO4Voltage_Field = value;
            }
        }

#warning Not defined
        public AXP1xxChargeCurrent ChargeControlCurrent { get; set; }

        /// <summary>
        /// NOT SUPPORTED FOR AXP173
        /// </summary>
        public override void ClearIRQ() => ThrowNotSupported(nameof(ClearIRQ));

        /// <summary>
        /// NOT SUPPORTED FOR AXP173
        /// </summary>
        public override void ReadIRQ() => ThrowNotSupported(nameof(ReadIRQ));

        protected override void Probe()
        {
            //base.Probe(); //AXP173 Does not have a chip ID
            byte[] stat = new byte[1];
            ReadBytes(0x01, stat);
            if (stat[0] == 0 || stat[0] == 0XFF)
                throw new DriverI2cException("Attempted to read AXP173 status register, but invalid data was sent");
            ChipID = AXP173_CHIP_ID;
            ReadBytes(AXP202_LDO234_DC23_CTL, stat); //Effective recycling
            OutputRegister = stat[0];
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
