using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// An RS-232 message from a host to an acceptor.
/// </summary>
internal abstract class Rs232RequestMessage : Rs232Message
{
    private readonly byte[] _payloadSource;
    
    /// <summary>
    /// Initializes a new instance of <see cref="Rs232RequestMessage"/>.
    /// </summary>
    /// <param name="payload"><see cref="Rs232Message.Payload"/>.</param>
    protected Rs232RequestMessage(IReadOnlyList<byte> payload)
    {
        _payloadSource = payload.ToArray();
        _payloadSource[^1] = CalculateChecksum(_payloadSource);
    }

    /// <inheritdoc/>
    public override IReadOnlyList<byte> Payload => _payloadSource.AsReadOnly();
    
    /// <summary>
    /// Mutates <see cref="Payload"/> at the specified index and calculates the checksum for the last byte.
    /// </summary>
    /// <param name="index">The index to mutate.</param>
    /// <param name="value">The value to set at the specified index.</param>
    protected void MutatePayload(byte index, byte value)
    {
        if (index >= _payloadSource.Length)
        {
            return;
        }
        
        _payloadSource[index] = value;
        _payloadSource[^1] = CalculateChecksum(_payloadSource);
    }
}