using PTI.Rs232Validator.Utility;
using System.Collections.Generic;

namespace PTI.Rs232Validator.Messages;

/// <summary>
/// An RS-232 message.
/// Each message contains a message type and an ACK number.
/// </summary>
public abstract class Rs232Message
{
    /// <summary>
    /// The minimum payload size in bytes.
    /// </summary>
    protected const byte MinPayloadByteSize = 5;

    /// <summary>
    /// The start of a message payload.
    /// </summary>
    protected const byte Stx = 0x02;

    /// <summary>
    /// The end of a message payload.
    /// </summary>
    protected const byte Etx = 0x03;

    /// <summary>
    /// The byte collection representing this instance.
    /// </summary>
    public abstract IReadOnlyList<byte> Payload { get; }

    /// <summary>
    /// The ACK number.
    /// </summary>
    /// <remarks>False = 0; True = 1.</remarks>
    public bool Ack => Payload.Count >= 3 && Payload[2].IsBitSet(0);

    /// <summary>
    /// An enumerator of <see cref="Rs232MessageType"/>.
    /// </summary>
    public Rs232MessageType MessageType =>
        Payload.Count >= 3 ? (Rs232MessageType)(Payload[2] & 0b11110000) : Rs232MessageType.Unknown;

    /// <summary>
    /// Calculates the 1-byte XOR checksum of the specified payload.
    /// </summary>
    /// <param name="payload">The payload to calculate the checksum of.</param>
    /// <returns>The checksum.</returns>
    protected static byte CalculateChecksum(IReadOnlyList<byte> payload)
    {
        if (payload.Count < MinPayloadByteSize)
        {
            return 0;
        }

        byte checksum = 0;
        for (var i = 1; i < payload.Count - 2; i++)
        {
            checksum ^= payload[i];
        }

        return checksum;
    }
}