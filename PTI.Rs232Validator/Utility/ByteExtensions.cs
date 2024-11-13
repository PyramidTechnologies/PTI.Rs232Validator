using System;

namespace PTI.Rs232Validator.Utility;

/// <summary>
/// A container of extension methods for <see cref="byte"/>.
/// </summary>
public static class ByteExtensions
{
    /// <summary>
    /// Indicates whether the specified bit is set (i.e. 1).
    /// </summary>
    /// <param name="b">The byte to observe.</param>
    /// <param name="bitIndex">The 0-based index of the bit (e.g. 0 => 2^0).</param>
    /// <returns></returns>
    public static bool IsBitSet(this byte b, byte bitIndex)
    {
        return (b & (1 << bitIndex)) != 0;
    }

    /// <summary>
    /// Sets the specified bit.
    /// </summary>
    /// <param name="b">The byte to mutate.</param>
    /// <param name="bitIndex">The 0-based index of the bit to set (e.g. 0 => 2^0).</param>
    /// <returns>The mutated byte.</returns>
    public static byte SetBit(this byte b, byte bitIndex)
    {
        return (byte)(b | (1 << bitIndex));
    }

    /// <summary>
    /// Clears the specified bit.
    /// </summary>
    /// <param name="b">The byte to mutate.</param>
    /// <param name="bitIndex">The 0-based index of the bit to clear (e.g. 0 => 2^0).</param>
    /// <returns>The mutated byte.</returns>
    public static byte ClearBit(this byte b, byte bitIndex)
    {
        return (byte)(b & ~(1 << bitIndex));
    }

    /// <summary>
    /// Converts the 8-byte array to a 32-bit unsigned integer via 4-bit encoding under big-endian order. 
    /// </summary>
    /// <param name="bytes">The 8-byte array to convert.</param>
    /// <returns>The 32-bit unsigned integer.</returns>
    public static uint ConvertToUint32Via4BitEncoding(this byte[] bytes)
    {
        if (bytes.Length != 8)
        {
            throw new ArgumentException("The array does not have a length of 8.", nameof(bytes));
        }

        uint result = 0;
        var j = 0;
        for (var i = 0; i < 8; i += 2)
        {
            result |= (uint)((bytes[i] << 4 | bytes[i + 1]) << (24 - 8 * j));
            j++;
        }

        return result;
    }
}