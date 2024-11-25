using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Models;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetServiceFlags"/>.
/// </summary>
internal class GetServiceFlagsResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 11;
    
    /// <summary>
    /// Initializes a new instance of <see cref="GetServiceFlagsResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public GetServiceFlagsResponseMessage(IReadOnlyList<byte> payload) : base(payload)
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
        ServiceFlags = new ServiceFlags
        {
            TachSensorServiceSuggestor = (ServiceSuggestor)data[0],
            BillPathServiceSuggestor = (ServiceSuggestor)data[1],
            CashboxBeltServiceSuggestor = (ServiceSuggestor)data[2],
            CashboxMechanismServiceSuggestor = (ServiceSuggestor)data[3],
            MasServiceSuggestor = (ServiceSuggestor)data[4],
            SpringRollersServiceSuggestor = (ServiceSuggestor)data[5]
        };
    }
    
    /// <inheritdoc cref="Models.ServiceFlags"/>
    public ServiceFlags ServiceFlags { get; } = new();
}