using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetServiceInfo"/>.
/// </summary>
public class GetServiceInfoResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 17;

    /// <summary>
    /// Initializes a new instance of <see cref="GetServiceInfoResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    internal GetServiceInfoResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (PayloadIssues.Count > 0)
        {
            return;
        }

        if (payload.Count != PayloadByteSize)
        {
            PayloadIssues.Add($"The payload size is {payload.Count} bytes, but {PayloadByteSize} bytes are expected.");
            return;
        }

        var data = Data.ToArray();
        LastCustomerService = data[..4].ClearEighthBits();
        LastServiceCenterService = data[4..8].ClearEighthBits();
        LastOemService = data[8..12].ClearEighthBits();
    }

    /// <summary>
    /// The 4 bytes of custom data that a customer wrote to an acceptor on the last service.
    /// </summary>
    public byte[] LastCustomerService { get; } = new byte[4];

    /// <summary>
    /// The 4 bytes of custom data that a service center wrote to an acceptor on the last service.
    /// </summary>
    public byte[] LastServiceCenterService { get; } = new byte[4];

    /// <summary>
    /// The 4 bytes of custom data that an OEM wrote to an acceptor on the last service.
    /// </summary>
    public byte[] LastOemService { get; } = new byte[4];

    /// <inheritdoc />
    public override string ToString()
    {
        return IsValid
            ? $"{nameof(LastCustomerService).AddSpacesToCamelCase()}: {LastCustomerService.ConvertToHexString(true, false)} | " +
              $"{nameof(LastServiceCenterService).AddSpacesToCamelCase()}: {LastServiceCenterService.ConvertToHexString(true, false)} | " +
              $"{nameof(LastOemService).AddSpacesToCamelCase()}: {LastOemService.ConvertToHexString(true, false)}"
            : base.ToString();
    }
}