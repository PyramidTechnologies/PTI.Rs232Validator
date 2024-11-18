using PTI.Rs232Validator.Utility;
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
    /// The start of a message.
    /// </summary>
    protected const byte Stx = 0x02;
    
    /// <summary>
    /// The end of a message.
    /// </summary>
    protected const byte Etx = 0x03;
    
    /// <summary>
    /// Initializes a new instance of <see cref="Rs232Message"/>.
    /// </summary>
    /// <param name="payload"><see cref="Payload"/></param>
    protected Rs232Message(IReadOnlyList<byte> payload)
    {
        if (payload.Count < 3)
        {
            return;
        }

        MessageType = (Rs232MessageType)(payload[2] & 0b11110000);
        Ack = payload[2].IsBitSet(0);
        PayloadSource = payload.ToArray();
    }

    /// <summary>
    /// An enumerator of <see cref="Rs232MessageType"/>.
    /// </summary>
    public Rs232MessageType MessageType { get; }

    /// <summary>
    /// The ACK number.
    /// </summary>
    /// <remarks>False = 0; True = 1.</remarks>
    public bool Ack { get; }

    /// <summary>
    /// The byte collection representing this instance.
    /// </summary>
    public IReadOnlyList<byte> Payload => PayloadSource.AsReadOnly();

    /// <summary>
    /// The mutable source of <see cref="Payload"/>.
    /// </summary>
    protected byte[] PayloadSource { get; } = [];

    /// <summary>
    /// Calculates and returns the 1-byte XOR checksum of <see cref="PayloadSource"/>.
    /// </summary>
    protected byte CalculateChecksum()
    {
        if (PayloadSource.Length < 5)
        {
            return 0;
        }

        byte checksum = 0;
        for (var i = 1; i < PayloadSource.Length - 2; ++i)
        {
            checksum ^= PayloadSource[i];
        }

        return checksum;
    }
}