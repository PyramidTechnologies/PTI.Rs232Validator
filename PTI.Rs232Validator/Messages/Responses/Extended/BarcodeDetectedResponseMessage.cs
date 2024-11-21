using System.Collections.Generic;

namespace PTI.Rs232Validator.Messages.Responses.Extended;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="ExtendedCommand.BarcodeDetected"/>.
/// </summary>
internal class BarcodeDetectedResponseMessage : ExtendedResponseMessage
{
    /// <summary>
    /// The expected payload size in bytes.
    /// </summary>
    public new const byte PayloadByteSize = 40;

    /// <summary>
    /// Initializes a new instance of <see cref="BarcodeDetectedResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public BarcodeDetectedResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (PayloadIssues.Count > 0)
        {
            return;
        }
        
        if (payload.Count < PayloadByteSize)
        {
            PayloadIssues.Add(
                $"The payload size is {payload.Count} bytes, but {PayloadByteSize} bytes are expected.");
        }
    }

    /// <summary>
    /// The last barcode.
    /// </summary>
    public IReadOnlyList<byte> Barcode => Data;
}