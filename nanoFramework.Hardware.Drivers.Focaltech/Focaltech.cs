using System;
using System.Device.I2c;
using System.Diagnostics;
using nanoFramework.Hardware.Drivers.Shared;

namespace nanoFramework.Hardware.Drivers
{
    public sealed class Focaltech
    {
        public const byte FOCALTECH_AGENT_ADDRESS = 0x38;
        public const byte GT9XX_AGENT_ADDRESS = 0x14;

        public const byte FT5206_VENDID = 0x11;
        public const byte FT6206_CHIPID = 0x06;
        public const byte FT6236_CHIPID = 0x36;
        public const byte FT6236U_CHIPID = 0x64;
        public const byte FT5206U_CHIPID = 0x64;

        private const byte FOCALTECH_REGISTER_MODE = 0x00;
        private const byte FOCALTECH_REGISTER_GEST = 0x01;
        private const byte FOCALTECH_REGISTER_STATUS = 0x02;
        private const byte FOCALTECH_REGISTER_TOUCH1_XH = 0x03;
        private const byte FOCALTECH_REGISTER_TOUCH1_XL = 0x04;
        private const byte FOCALTECH_REGISTER_TOUCH1_YH = 0x05;
        private const byte FOCALTECH_REGISTER_TOUCH1_YL = 0x06;
        private const byte FOCALTECH_REGISTER_THRESHHOLD = 0x80;
        private const byte FOCALTECH_REGISTER_CONTROL = 0x86;
        private const byte FOCALTECH_REGISTER_MONITORTIME = 0x87;
        private const byte FOCALTECH_REGISTER_ACTIVEPERIOD = 0x88;
        private const byte FOCALTECH_REGISTER_MONITORPERIOD = 0x89;

        private const byte FOCALTECH_REGISTER_LIB_VERSIONH = 0xA1;
        private const byte FOCALTECH_REGISTER_LIB_VERSIONL = 0xA2;
        private const byte FOCALTECH_REGISTER_INT_STATUS = 0xA4;
        private const byte FOCALTECH_REGISTER_POWER_MODE = 0xA5;
        private const byte FOCALTECH_REGISTER_VENDOR_ID = 0xA3;
        private const byte FOCALTECH_REGISTER_VENDOR1_ID = 0xA8;
        private const byte FOCALTECH_REGISTER_ERROR_STATUS = 0xA9;

        public static Version LibraryVersion { get; } = new Version(0, 0, 0, 0);

        /// <summary>
        /// Initializes a driver for the Focaltech touchscreen
        /// </summary>
        /// <param name="device"></param>
        /// <exception cref="ApplicationException">Thrown when device probing fails</exception>
        public Focaltech(I2cDevice device)
        {
            I2c = device;
            Callbacks = false; 
            Initialize();
        }

        public Focaltech(ByteTransferCallbackReg8 readCallback, ByteTransferCallbackReg8 writeCallback)
        {
            ReadCallback = readCallback;
            WriteCallback = writeCallback;
            Callbacks = true;
            Initialize();
        }

        private void Initialize()
        {
            if(!Probe())
                throw new SystemException("Probing failed. Could not initialize Focaltech device");
            
            if (ReadRegisterByte(FOCALTECH_REGISTER_THRESHHOLD, out Threshold_Field) != I2cTransferStatus.FullTransfer)
                throw new SystemException("Could not read Threshold register of Focaltech device");

            if (ReadRegisterByte(FOCALTECH_REGISTER_MONITORTIME, out MonitorTime_Field) != I2cTransferStatus.FullTransfer)
                throw new SystemException("Could not read MonitorTime register of Focaltech device");

            if (ReadRegisterByte(FOCALTECH_REGISTER_ACTIVEPERIOD, out ActivePeriod_Field) != I2cTransferStatus.FullTransfer)
                throw new SystemException("Could not read ActivePeriod register of Focaltech device");

            if (ReadRegisterByte(FOCALTECH_REGISTER_MONITORPERIOD, out MonitorPeriod_Field) != I2cTransferStatus.FullTransfer)
                throw new SystemException("Could not read MonitorPeriod register of Focaltech device");

            if (ReadRegisterByte(FOCALTECH_REGISTER_INT_STATUS, out var intenable) != I2cTransferStatus.FullTransfer)
                throw new SystemException("Could not read INTStatus register of Focaltech device");
            EnableINT_Field = intenable == 1 || (intenable == 0 ? false : throw new SystemException("Invalid INT Status"));

            VendorID = ReadRegisterByte(FOCALTECH_REGISTER_VENDOR_ID);
            Vendor1ID = ReadRegisterByte(FOCALTECH_REGISTER_VENDOR1_ID);
            DeviceLibVersion = ReadRegisterByte(FOCALTECH_REGISTER_LIB_VERSIONH);

            Initialized = true;
        }

        private bool Initialized = false;
        private readonly bool Callbacks = false;

        private readonly ByteTransferCallbackReg8 ReadCallback;
        private readonly ByteTransferCallbackReg8 WriteCallback;

        private readonly I2cDevice I2c;

        /// <summary>
        /// Gets the latest error code of the Focaltech, if available
        /// </summary>
        /// <param name="error">The error code</param>
        /// <returns>True if an error code is available, false otherwise</returns>
        public bool GetErrorCode(out byte error)
        {
            ReadRegisterByte(FOCALTECH_REGISTER_ERROR_STATUS, out error);
            return error != 0;
        }

        public FocaltechReport GetReport() => new FocaltechReport(this);

        public bool GetPoint(out ushort x, out ushort y)
        {
            var buffer = new byte[5];
            x = 0; y = 0;
            if(ReadBytes(FOCALTECH_REGISTER_STATUS, buffer) == I2cTransferStatus.FullTransfer)
            {
                if (buffer[0] == 0 || buffer[0] > 2)
                    return false;
                Event = (FocaltechEventFlag)(buffer[1] & 0xC0);
                x = (ushort)((buffer[1] & 0x0Fu) << 8 | buffer[2]);
                y = (ushort)((buffer[3] & 0x0Fu) << 8 | buffer[4]);
                return true;
            }
            return false;
        }

        private byte MonitorTime_Field;
        /// <summary>
        /// Gets or sets the MonitorTime in seconds
        /// </summary>
        public byte MonitorTime
        {
            get => MonitorTime_Field;
            set
            {
                if (value == MonitorTime_Field)
                    return;
                WriteRegisterByte(FOCALTECH_REGISTER_MONITORTIME, value);
                MonitorTime_Field = value;
            }
        }


        private byte ActivePeriod_Field;
        /// <summary>
        /// Gets or Sets the Active Period rate
        /// </summary>
        public byte ActivePeriod
        {
            get => ActivePeriod_Field;
            set
            {
                if (value == ActivePeriod_Field)
                    return;
                WriteRegisterByte(FOCALTECH_REGISTER_ACTIVEPERIOD, value);
                ActivePeriod_Field = value;
            }
        }


        private byte MonitorPeriod_Field;
        /// <summary>
        /// Gets or Sets the Monitor Period rate
        /// </summary>
        public byte MonitorPeriod
        {
            get => MonitorPeriod_Field;
            set
            {
                if (value == MonitorPeriod_Field)
                    return;
                WriteRegisterByte(FOCALTECH_REGISTER_MONITORPERIOD, value);
                MonitorPeriod_Field = value;
            }
        }

        private byte Threshold_Field;
        public byte Threshold
        {
            get => Threshold_Field;
            set
            {
                if (value == Threshold_Field)
                    return;
                WriteRegisterByte(FOCALTECH_REGISTER_THRESHHOLD, value);
                Threshold_Field = value;
            }
        }
        private bool EnableINT_Field;
        public bool EnableINT
        {
            get => EnableINT_Field;
            set
            {
                if (value == EnableINT_Field)
                    return;
                WriteRegisterByte(FOCALTECH_REGISTER_INT_STATUS, (byte)(value ? 1 : 0));
                EnableINT_Field = value;
            }
        }
        public bool EnableAutoCalibration
        {
            get => MonitorTime == 0x00;
            set
            {
                if (value && MonitorTime == 0x00)
                    return;
                if (!value && MonitorTime == 0xFF)
                    return;
                MonitorTime = (byte)(value ? 0x00 : 0xFF);
            }
        }
        public byte VendorID { get; private set; }
        public byte Vendor1ID { get; private set; }
        public byte DeviceLibVersion { get; private set; }
        public byte Touched => ReadRegisterByte(FOCALTECH_REGISTER_STATUS);
        public byte Control => ReadRegisterByte(FOCALTECH_REGISTER_CONTROL);
        public byte DeviceMode => (byte)((ReadRegisterByte(FOCALTECH_REGISTER_MODE) >> 4) & 0x07); 
        public FocaltechGesture Gesture
        {
            get
            {
                //This would look soooo much cleaner with switch expression statement
                switch (ReadRegisterByte(FOCALTECH_REGISTER_GEST))
                {
                    case 0x10:
                        return FocaltechGesture.MoveUp;
                    case 0x14:
                        return FocaltechGesture.MoveRight;
                    case 0x18:
                        return FocaltechGesture.MoveDown;
                    case 0x1c:
                        return FocaltechGesture.MoveLeft;
                    case 0x48:

                        return FocaltechGesture.ZoomIn;
                    case 0x49:
                        return FocaltechGesture.ZoomOut;
                    default:
                        return FocaltechGesture.None;
                }
            }
        }
        public FocaltechEventFlag Event { get; private set; }

        private FocaltechPowerMode PowerMode_Field;
        public FocaltechPowerMode PowerMode
        {
            get => PowerMode_Field;
            set
            {
                if (value == PowerMode_Field)
                    return;
                WriteRegisterByte(FOCALTECH_REGISTER_INT_STATUS, (byte)value);
                PowerMode_Field = value;
            }
        }

        private bool Probe() => I2c.Read(SpanByte.Empty).Status == I2cTransferStatus.FullTransfer;

        //I think stackalloc would greatly increase the efficiency of this code, I feel like constant calls to this would undoubtedly put a lot of pressure on the garbage collector

        //Consider also the possibility of allocating the arrays once

        private byte ReadRegisterByte(byte register)
            => ReadRegisterByte(register, out byte result) != I2cTransferStatus.FullTransfer ? 
            throw new SystemException($"Unable to read register {register}") :
            result;

        private I2cTransferStatus ReadRegisterByte(byte register, out byte result)
        {
            var r = new byte[1];
            var stat = ReadBytes(register, r);
            result = r[0];
            return stat;
        }
        private I2cTransferStatus WriteRegisterByte(byte register, byte value) => WriteBytes(register, new byte[] { value });

        private I2cTransferStatus ReadBytes(byte register, byte[] data)
            => IsInit() && Callbacks
                ? ReadCallback(I2c, register, data)
                : I2c.WriteRead(new byte[] { register }, data).Status;
        private I2cTransferStatus WriteBytes(byte register, byte[] data)
        {
            IsInit();
            if (Callbacks)
                return WriteCallback(I2c, register, data);
            
            var buffer = new byte[data.Length + 1];
            buffer[0] = register;
            data.CopyTo(buffer, 1);
            return I2c.Write(buffer).Status;
        }
        private bool IsInit() => Initialized ? true : throw new InvalidOperationException("Cannot Manipulate the object when it is not initialized");
    }

    public struct FocaltechReport
    {
        public DateTime SnapshotDate { get; private set; }
        public Focaltech Device { get; private set; }
        public ushort TouchX { get; private set; }
        public ushort TouchY { get; private set; }
        public bool Touched { get; private set; }
        public FocaltechGesture Gesture { get; private set; }
        public FocaltechEventFlag EventFlag { get; private set; }

        readonly bool IsErrored;
        readonly byte Error;
        public bool GetError(out byte error)
        {
            error = Error;
            return IsErrored;
        }

        public bool GetTouch(out ushort x, out ushort y)
        {
            x = TouchX;
            y = TouchY;
            return Touched;
        }

        internal FocaltechReport(Focaltech device)
        {
            SnapshotDate = DateTime.UtcNow;
            Device = device;
            Touched = device.Touched > 1;

            IsErrored = device.GetErrorCode(out Error);

            if (Touched)
                if (device.GetPoint(out ushort x, out ushort y))
                {
                    TouchX = x;
                    TouchY = y;
                }
                else
                    throw new InvalidOperationException(IsErrored ? 
                        $"Could not read Point of Touch from Focaltech device. Error: {Error}" : 
                        "Could not read Point of Touch from Focaltech device.");
            else
            {
                TouchX = 0;
                TouchY = 0;
            }
            Gesture = device.Gesture;
            EventFlag = device.Event;
        }
    }
}
