using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Utility;

public static class ByteExtensions
{
    /// <summary>
    /// Indicates whether the specified bit is set (i.e. 1).
    /// </summary>
    /// <param name="b">The byte to observe.</param>
    /// <param name="bitIndex">The 0-based index of the bit.</param>
    /// <returns></returns>
    public static bool IsBitSet(this byte b, byte bitIndex)
    {
        return (b & (1 << bitIndex)) != 0;
    }
    
    /// <summary>
    /// Indicates whether any of the specified bits are set (i.e. 1).
    /// </summary>
    /// <param name="b">The byte to check.</param>
    /// <param name="bitIndices">The 0-based indices of the bits.</param>
    /// <returns></returns>
    public static bool AreAnyBitsSet(this byte b, IEnumerable<byte> bitIndices)
    {
        return bitIndices.Any(bitIndex => b.IsBitSet(bitIndex));
    }
    
    /// <summary>
    /// Sets the specified bit.
    /// </summary>
    /// <param name="b">The byte to mutate.</param>
    /// <param name="bitIndex">The 0-based index of the bit to set.</param>
    /// <returns>The mutated byte.</returns>
    public static byte SetBit(this byte b, byte bitIndex)
    {
        return (byte)(b | (1 << bitIndex));
    }
    
    /// <summary>
    /// Clears the specified bit.
    /// </summary>
    /// <param name="b">The byte to mutate.</param>
    /// <param name="bitIndex">The 0-based index of the bit to clear.</param>
    /// <returns>The mutated byte.</returns>
    public static byte ClearBit(this byte b, byte bitIndex)
    {
        return (byte)(b & ~(1 << bitIndex));
    }
}