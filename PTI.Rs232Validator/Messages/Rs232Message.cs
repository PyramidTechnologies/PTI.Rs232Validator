using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages;

/// <summary>
/// An RS-232 message.
/// Each message contains a message type and an ACK number.
/// </summary>
internal abstract class Rs232Message
{
    /// <summary>
    /// Initializes a new instance of <see cref="Rs232Message"/>.
    /// </summary>
    /// <param name="payload">The byte collection representing an <see cref="Rs232Message"/> instance.</param>
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

    /// <summary>
    /// An enumerator of <see cref="Rs232MessageType"/>.
    /// </summary>
    public Rs232MessageType MessageType { get; }

    /// <summary>
    /// Is the ACK number set to 1?
    /// </summary>
    public bool IsAckNumberOne { get; }

    /// <summary>
    /// The editable payload.
    /// </summary>
    protected byte[] Payload { get; } = [];

    /// <summary>
    /// Serializes this instance to a byte collection.
    /// </summary>
    public byte[] Serialize()
    {
        return Payload.ToArray();
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