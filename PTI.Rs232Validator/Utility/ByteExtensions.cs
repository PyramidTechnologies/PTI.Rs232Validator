using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

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
    /// Converts the specified byte to a string representation of its binary value.
    /// </summary>
    /// <param name="b">The byte to convert.</param>
    /// <param name="shouldIncludePrefix">True to include the binary prefix "0b"; otherwise, false.</param>
    /// <returns>The binary string.</returns>
    /// <example>
    /// <code>
    ///     byte b = 0b00000001;
    ///     Console.WriteLine(b.ConvertToBinary(true)); // Output: 0b00000001
    ///     Console.WriteLine(b.ConvertToBinary(false)); // Output: 00000001
    /// </code>
    /// </example>
    public static string ConvertToBinaryString(this byte b, bool shouldIncludePrefix)
    {
        var prefix = shouldIncludePrefix ? "0b" : string.Empty;
        return prefix + Convert.ToString(b, 2).PadLeft(8, '0');
    }

    /// <summary>
    /// Converts the specified 4-byte collection to a 16-bit unsigned integer via 4-bit encoding under big-endian order. 
    /// </summary>
    /// <param name="bytes">The 4-byte collection to convert.</param>
    /// <returns>The 16-bit unsigned integer.</returns>
    public static ushort ConvertToUint16Via4BitEncoding(this IReadOnlyList<byte> bytes)
    {
        const byte expectedByteSize = 4;
        if (bytes.Count != expectedByteSize)
        {
            throw new ArgumentException($"The byte collection size is {bytes.Count}, but 4 is expected.", nameof(bytes));
        }

        ushort result = 0;
        byte j = 0;
        for (var i = 0; i < expectedByteSize; i += 2)
        {
            result |= (ushort)((bytes[i] << 4 | bytes[i + 1]) << (8 - 8 * j));
            j++;
        }

        return result;
    }

    /// <summary>
    /// Converts the specified 8-byte collection to a 32-bit unsigned integer via 4-bit encoding under big-endian order. 
    /// </summary>
    /// <param name="bytes">The 8-byte collection to convert.</param>
    /// <returns>The 32-bit unsigned integer.</returns>
    public static uint ConvertToUint32Via4BitEncoding(this IReadOnlyList<byte> bytes)
    {
        const byte expectedByteSize = 8;
        if (bytes.Count != expectedByteSize)
        {
            throw new ArgumentException($"The byte collection size is {bytes.Count}, but 8 is expected.", nameof(bytes));
        }

        uint result = 0;
        byte j = 0;
        for (var i = 0; i < expectedByteSize; i += 2)
        {
            result |= (uint)((bytes[i] << 4 | bytes[i + 1]) << (24 - 8 * j));
            j++;
        }

        return result;
    }
    
    /// <summary>
    /// Clears the 8th bit of each byte in the specified collection.
    /// </summary>
    /// <param name="bytes">The byte collection to mutate.</param>
    /// <returns>The mutated byte collection.</returns>
    public static byte[] ClearEighthBits(this byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = bytes[i].ClearBit(7);
        }

        return bytes;
    }
    
    /// <summary>
    /// Converts the specified byte collection to a hexadecimal string.
    /// </summary>
    /// <param name="bytes">The byte collection to convert.</param>
    /// <param name="shouldIncludeHexPrefix">True to include the hex prefix "0x"; otherwise, false.</param>
    /// <param name="shouldIncludeSpaces">True to include spaces between each byte; otherwise, false.</param>
    /// <returns>The hexadecimal string.</returns>
    /// <example>
    /// <code>
    ///     var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
    ///     Console.WriteLine(bytes.ConvertToHexString(true, true)); // Output: 0x01 0x02 0x03 0x04
    ///     Console.WriteLine(bytes.ConvertToHexString(true, false)); // Output: 0x01020304
    ///     Console.WriteLine(bytes.ConvertToHexString(false, true)); // Output: 01 02 03 04
    ///     Console.WriteLine(bytes.ConvertToHexString(false, false)); // Output: 01020304
    /// </code>
    /// </example>
    public static string ConvertToHexString(this IReadOnlyList<byte> bytes, bool shouldIncludeHexPrefix, bool shouldIncludeSpaces)
    {
        if (bytes.Count == 0)
        {
            return string.Empty;
        }

        var hexString = new StringBuilder(bytes.Count * 2);
        for (var i = 0; i < bytes.Count; i++)
        {
            if (shouldIncludeHexPrefix && (shouldIncludeSpaces || i == 0))
            {
                hexString.Append("0x");
            }
            
            hexString.Append(bytes[i].ToString("X2"));
            
            if (shouldIncludeSpaces && i < bytes.Count - 1)
            {
                hexString.Append(' ');
            }
        }
        
        return hexString.ToString();
    }
}