using System;
using System.Device.I2c;
using System.Threading;
using Windows.Devices.Gpio;
using nanoFramework.Hardware.Drivers.Shared;

//Based on https://github.com/lewisxhe/FocalTech_Library by Lewis He

namespace nanoFramework.Hardware.Drivers
{
    public sealed class GT9xx
    {
        public const ushort GT9XX_COORDINATE = 0x814E;
        public const ushort GT9XX_CLEARBUF = 0x814E;
        public const ushort GT9XX_CONFIG = 0x8047;
        public const ushort GT9XX_COMMAND = 0x8040;
        public const ushort GT9XX_PRODUCT_ID = 0x8140;
        public const ushort GT9XX_VENDOR_ID = 0x814A;
        public const ushort GT9XX_CONFIG_VERSION = 0x8047;
        public const ushort GT9XX_CONFIG_CHECKSUM = 0x80FF;
        public const ushort GT9XX_FIRMWARE_VERSION = 0x8144;

        private readonly bool Initialized;
        private readonly bool Callbacks;

        private readonly ByteTransferCallbackReg16 ReadCallback;
        private readonly ByteTransferCallbackReg16 WriteCallback;

        private readonly GpioPin Reset;
        private readonly GpioPin Interrupt;

        private readonly I2cDevice I2c;

        private GT9xx(int reset, int interrupt, GpioController gpio)
        {
            gpio = gpio ?? GpioController.GetDefault();

            if (reset < 0)
                throw new ArgumentOutOfRangeException(nameof(reset));
            if (interrupt < 0)
                throw new ArgumentOutOfRangeException(nameof(interrupt));
            
            Reset = gpio.OpenPin(reset, GpioSharingMode.Exclusive);
            Interrupt = gpio.OpenPin(interrupt, GpioSharingMode.Exclusive);
        }

        public GT9xx(I2cDevice device, int reset, int interrupt, GpioController gpio = null) : this(reset, interrupt, gpio)
        {
            if (device is null)
                throw new ArgumentNullException(nameof(device));

            I2c = device;
            Callbacks = false;
            ProbeAndConfigure();
        }

        public GT9xx(ByteTransferCallbackReg16 readCallback, ByteTransferCallbackReg16 writeCallback, int reset, int interrupt, GpioController gpio = null) : this(reset, interrupt, gpio)
        {
            if (readCallback is null)
                throw new ArgumentNullException(nameof(readCallback));
            if (writeCallback is null)
                throw new ArgumentNullException(nameof(writeCallback));

            ReadCallback = readCallback;
            WriteCallback = writeCallback;
            Callbacks = true;
            ProbeAndConfigure();
        }

        public void SoftReset() => WriteRegisterByte(GT9XX_COMMAND, 0x01);

        private struct GT9xxPoint
        {
            public ushort X;
            public ushort Y;
        }

        private readonly GT9xxPoint[] PointData = new GT9xxPoint[5];
        public byte ScanPoint()
        {
            byte point = 0;
            byte[] buffer = new byte[40];
            if (ReadBytes(GT9XX_COORDINATE, buffer) != I2cTransferStatus.FullTransfer)
                throw new InvalidOperationException("I2c Transfer was unsuccesful");
            WriteRegisterByte(GT9XX_CLEARBUF, 0);
            point = (byte)(buffer[0] & 0xFu);
            if (point == 0)
                return 0;

            PointData[0].X = (ushort)(((uint)buffer[3] << 8) + buffer[2]);
            PointData[0].Y = (ushort)(((uint)buffer[5] << 8) + buffer[4]);

            PointData[1].X = (ushort)(((uint)buffer[11] << 8) + buffer[10]);
            PointData[1].Y = (ushort)(((uint)buffer[13] << 8) + buffer[12]);

            PointData[2].X = (ushort)(((uint)buffer[19] << 8) + buffer[18]);
            PointData[2].Y = (ushort)(((uint)buffer[21] << 8) + buffer[20]);

            PointData[3].X = (ushort)(((uint)buffer[27] << 8) + buffer[26]);
            PointData[3].Y = (ushort)(((uint)buffer[29] << 8) + buffer[28]);

            PointData[4].X = (ushort)(((uint)buffer[35] << 8) + buffer[34]);
            PointData[4].Y = (ushort)(((uint)buffer[37] << 8) + buffer[36]);
            
            return point;
        }

        public void GetPoint(out ushort x, out ushort y, byte index)
        {
            if (index >= 4 || index < 0)
                throw new ArgumentOutOfRangeException("Index must be between 0 - 3");

            var p = PointData[index];
            x = p.X;
            y = p.Y;
        }

        private bool ProbeAndConfigure()
        {
            byte[] config0 =
            {
                0x5D, 0x40, 0x01, 0xE0, 0x01, 0x05, 0x35, 0x00, 0x01, 0x08,
                0x1E, 0x0F, 0x50, 0x32, 0x03, 0x05, 0x00, 0x00, 0x00, 0x00,
                0x22, 0x22, 0x00, 0x18, 0x1B, 0x1E, 0x14, 0x87, 0x27, 0x0A,
                0x3C, 0x3E, 0x0C, 0x08, 0x00, 0x00, 0x00, 0x9B, 0x02, 0x1C,
                0x00, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x09, 0x11, 0x00,
                0x00, 0x28, 0x6E, 0x94, 0xC5, 0x02, 0x00, 0x00, 0x00, 0x04,
                0xAB, 0x2C, 0x00, 0x8D, 0x36, 0x00, 0x75, 0x42, 0x00, 0x61,
                0x51, 0x00, 0x51, 0x63, 0x00, 0x51, 0x00, 0x00, 0x00, 0x00,
                0xF0, 0x4A, 0x3A, 0xFF, 0xFF, 0x27, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x14, 0x12, 0x10, 0x0E, 0x0C, 0x0A, 0x08, 0x06,
                0x04, 0x02, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x02, 0x04, 0x06, 0x08, 0x0A, 0x0C, 0x24,
                0x22, 0x21, 0x20, 0x1F, 0x1E, 0x1D, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0xC0, 0x01
            };

            //Reset.SetDriveMode(GpioPinDriveMode.Output);
            //Interrupt.SetDriveMode(GpioPinDriveMode.Output);
            //Reset.Write(GpioPinValue.Low);
            //Thread.Sleep(30);
            //Interrupt.Write(GpioPinValue.High);
            //Thread.Sleep(10);
            //Interrupt.SetDriveMode(GpioPinDriveMode.OutputOpenDrain);
            //Reset.Write(GpioPinValue.High);
            //I don't honestly know why this is commented out, but it's so on Lewis' library

            SoftReset();

            byte[] idbuffer = new byte[3];
            ReadBytes(GT9XX_PRODUCT_ID, idbuffer);
            if (idbuffer[0] != '9')
                return false;
            byte checksum = 0;
            for (int i = 0; i < config0.Length - 2; i++)
                checksum += config0[i];
            config0[184] = (byte)(~(uint)checksum + 1);
            return WriteBytes(GT9XX_CONFIG, config0) == I2cTransferStatus.FullTransfer;
        }

        //I think stackalloc would greatly increase the efficiency of this code, I feel like constant calls to this would undoubtedly put a lot of pressure on the garbage collector

        //Consider also the possibility of allocating the arrays once

        private byte ReadRegisterByte(byte register)
            => ReadRegisterByte(register, out byte result) != I2cTransferStatus.FullTransfer ?
            throw new SystemException($"Unable to read register {register}") :
            result;

        private I2cTransferStatus ReadRegisterByte(ushort register, out byte result)
        {
            var r = new byte[1];
            var stat = ReadBytes(register, r);
            result = r[0];
            return stat;
        }
        private I2cTransferStatus WriteRegisterByte(ushort register, byte value) => WriteBytes(register, new byte[] { value });

        private I2cTransferStatus ReadBytes(ushort register, byte[] data)
            => IsInit() && Callbacks
                ? ReadCallback(I2c, register, data)
                : I2c.WriteRead(new byte[] { (byte)(register >> 8), (byte)(register & 0xFF) }, data).Status;
        private I2cTransferStatus WriteBytes(ushort register, byte[] data)
        {
            IsInit();
            if (Callbacks)
                return WriteCallback(I2c, register, data);

            var buffer = new byte[data.Length + 2];
            buffer[0] = (byte)(register >> 8);
            buffer[1] = (byte)(register & 0xFF);
            data.CopyTo(buffer, 2);
            return I2c.Write(buffer).Status;
        }
        private bool IsInit() => Initialized ? true : throw new InvalidOperationException("Cannot Manipulate the object when it is not initialized");
    }
}
