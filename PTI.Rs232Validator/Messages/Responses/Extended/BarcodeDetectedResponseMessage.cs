using PTI.Rs232Validator.Messages.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PTI.Rs232Validator.Messages.Responses.Extended;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="ExtendedCommand.BarcodeDetected"/>.
/// </summary>
internal class BarcodeDetectedResponseMessage : ExtendedResponseMessage
{
    /// <summary>
    /// The expected payload size in bytes.
    /// </summary>
    private const byte PayloadByteSize = 40;

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
        
        Barcode = Encoding.ASCII.GetString(Data.ToArray()).Trim('\0');
    }

    /// <summary>
    /// The last barcode string.
    /// </summary>
    public string Barcode { get; } = string.Empty;
}