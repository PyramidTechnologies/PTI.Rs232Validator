using PTI.Rs232Validator.Models;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetServiceUsageCounters"/>.
/// </summary>
internal class GetServiceUsageCountersResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 53;
    
    /// <summary>
    /// Initializes a new instance of <see cref="GetServiceUsageCountersResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public GetServiceUsageCountersResponseMessage(IReadOnlyList<byte> payload) : base(payload)
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
        ServiceUsageCounters = new ServiceUsageCounters
        {
            DistancedMovedSinceLastTachSensorService = data[..8].ConvertToUint32Via4BitEncoding(),
            DistanceMovedSinceLastBillPathService = data[8..16].ConvertToUint32Via4BitEncoding(),
            DistancedMoveSinceLastBeltService = data[16..24].ConvertToUint32Via4BitEncoding(),
            BillsStackedSinceLastCashboxService = data[24..32].ConvertToUint32Via4BitEncoding(),
            DistanceMovedSinceLastMasService = data[32..40].ConvertToUint32Via4BitEncoding(),
            DistanceMovedSinceLastSpringRollerService = data[40..48].ConvertToUint32Via4BitEncoding()
        };
    }
    
    /// <inheritdoc cref="Models.ServiceUsageCounters"/>
    public ServiceUsageCounters ServiceUsageCounters { get; } = new();
}