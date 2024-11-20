using PTI.Rs232Validator.Models;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetServiceInfo"/>.
/// </summary>
internal class GetServiceInfoResponseMessage : TelemetryResponseMessage
{
    /// <summary>
    /// The payload size in bytes.
    /// </summary>
    public const byte PayloadByteSize = 17;
    
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
        ServiceInfo = new ServiceInfo
        {
            LastCustomerService = data[..4].ClearEighthBits(),
            LastServiceCenterService = data[4..8].ClearEighthBits(),
            LastOemService = data[8..12].ClearEighthBits(),
        };
    }
    
    /// <inheritdoc cref="Models.ServiceInfo"/>
    public ServiceInfo ServiceInfo { get; } = new();
}