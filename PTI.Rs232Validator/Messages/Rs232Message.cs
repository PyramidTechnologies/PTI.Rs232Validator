using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages;

/// <summary>
/// Base RS-232 message.
/// All messages contain a message type and ACK number.
/// </summary>
internal abstract class Rs232Message
{
    /// <summary>
    /// Initializes a new instance of <see cref="Rs232Message"/>.
    /// </summary>
    /// <param name="payload">Bytes representing an <see cref="Rs232Message"/> instance.</param>
    protected Rs232Message(IReadOnlyList<byte> payload)
    {
        if (payload.Count < 3)
        {
            return;
        }
        
        MessageType = (Rs232MessageType)(payload[2] >> 4 & 0b111);
        IsAckNumberOne = (payload[2] & 1) == 1;
        Payload = payload.ToArray();
    }
    
    /// <inheritdoc cref="Rs232MessageType"/>
    public Rs232MessageType MessageType { get; }
    
    /// <summary>
    /// Is the ACK number set to 1?
    /// </summary>
    public bool IsAckNumberOne { get; }
    
    /// <summary>
    /// Editable payload.
    /// </summary>
    protected byte[] Payload { get; } = [];
    
    /// <summary>
    /// Returns a copy of <see cref="Payload"/>.
    /// </summary>
    public byte[] Serialize()
    {
        return Payload.ToArray();
    }
    
    /// <summary>
    /// Is the bit set to 1 in value?
    /// </summary>
    /// <param name="bitIndex">0-based bit index (e.g. index of 1 => 2^1).</param>
    /// <param name="value">Value containing bit.</param>
    protected static bool IsBitSet(int bitIndex, byte value)
    {
        return (value & (1 << bitIndex)) == 1 << bitIndex;
    }
    
    /// <summary>
    /// Are any bits set to 1 in value?
    /// </summary>
    /// <param name="bitIndices">0-based bit indices (e.g. index of 1 => 2^1).</param>
    /// <param name="value">Value containing bits.</param>
    protected static bool AreAnyBitsSet(IEnumerable<int> bitIndices, byte value)
    {
        return bitIndices.Any(b => IsBitSet(b, value));
    }
    
    /// <summary>
    /// Are all bits set to 1 in value?
    /// </summary>
    /// <param name="bitIndices">0-based bit indices (e.g. index of 1 => 2^1).</param>
    /// <param name="value">Value containing bits.</param>
    protected static bool AreAllBitsSet(IEnumerable<int> bitIndices, byte value)
    {
        return bitIndices.All(b => IsBitSet(b, value));
    }
    
    /// <summary>
    /// Sets the bit to 1 in value.
    /// </summary>
    /// <param name="bitIndex">0-based index of bit to set (e.g. index of 1 => 2^1).</param>
    /// <param name="value">Value containing bit.</param>
    /// <returns>Value with bit set.</returns>
    protected static byte SetBit(int bitIndex, byte value)
    {
        return (byte)(value | (1 << bitIndex));
    }
    
    /// <summary>
    /// Sets the bit to 0 in value.
    /// </summary>
    /// <param name="bitIndex">0-based index of bit to clear (e.g. index of 1 => 2^1).</param>
    /// <param name="value">Value containing bit.</param>
    /// <returns>Value with bit cleared.</returns>
    protected static byte ClearBit(int bitIndex, byte value)
    {
        return (byte)(value & ~(1 << bitIndex));
    }
    
    /// <summary>
    /// Calculates and returns the 1-byte XOR checksum of this instance.
    /// </summary>
    protected byte CalculateChecksum()
    {
        if (Payload.Length < 5)
        {
            return 0;
        }
        
        byte checksum = 0;
        for (var i = 1; i < Payload.Length - 2; ++i)
        {
            checksum ^= Payload[i];
        }
        
        return checksum;
    }
}