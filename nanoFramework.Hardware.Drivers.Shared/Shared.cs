using System.Device.I2c;

namespace nanoFramework.Hardware.Drivers.Shared
{
    public delegate I2cTransferStatus ByteTransferCallbackReg16(I2cDevice dev, ushort regAddr, byte[] data);
    public delegate I2cTransferStatus ByteTransferCallbackReg8(I2cDevice dev, byte regAddr, byte[] data);

    public static class StringFormats
    {
        public const string HexByteFormat = "#0:X";
    }

    public static class Bit
    {
        /// <summary>
        /// Flips the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static void Flip(this ref byte val, int bit) => val ^= (byte)(1u << bit);
        
        /// <summary>
        /// Flips the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static void Flip(this ref ushort val, int bit) => val ^= (ushort)(1u << bit);

        /// <summary>
        /// Flips the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static byte Flip(byte val, int bit) => (byte)(val ^ (1u << bit));

        /// <summary>
        /// Flips the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static ushort Flip(ushort val, int bit) => (ushort)(val ^ (1u << bit));

        /// <summary>
        /// Sets the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static void Set(this ref byte val, int bit) => val |= (byte)(1u << bit);

        /// <summary>
        /// Sets the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static void Set(this ref ushort val, int bit) => val |= (ushort)(1u << bit);

        /// <summary>
        /// Sets the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static byte Set(byte val, int bit) => val |= (byte)(1u << bit);

        /// <summary>
        /// Sets the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static ushort Set(ushort val, int bit) => val |= (ushort)(1u << bit);

        /// <summary>
        /// Sets the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static void Clear(this ref byte val, int bit) => val &= (byte)~(1u << bit);

        /// <summary>
        /// Clears the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static void Clear(this ref ushort val, int bit) => val &= (ushort)~(1u << bit);

        /// <summary>
        /// Clears the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static byte Clear(byte val, int bit) => val &= (byte)~(1u << bit);

        /// <summary>
        /// Clears the given bit counting from right to left
        /// </summary>
        /// <param name="val"></param>
        /// <param name="bit"></param>
        public static ushort Clear(ushort val, int bit) => val &= (ushort)~(1u << bit);

        public static bool IsBitSet(this uint val, int bit) => (val & (1u << bit)) > 0;
        public static bool IsBitSet(this ushort val, int bit) => (val & (1u << bit)) > 0;
        public static bool IsBitSet(this byte val, int bit) => (val & (1u << bit)) > 0;

        public static byte BitMask(byte x) => (byte)(1 << x);
        public static byte InvertByte(byte x) => ((byte)((((~(uint)x) << 24) >> 24)));
    }

    [System.Serializable]
    public class DriverI2cException : System.Exception
    {
        public DriverI2cException() { }
        public DriverI2cException(string message) : base(message) { }
        public DriverI2cException(string message, System.Exception inner) : base(message, inner) { }
    }
}
