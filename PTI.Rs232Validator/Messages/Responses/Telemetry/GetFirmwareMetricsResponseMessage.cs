using PTI.Rs232Validator.Models;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetFirmwareMetrics"/>.
/// </summary>
internal class GetFirmwareMetricsResponseMessage : TelemetryResponseMessage
{
    /// <summary>
    /// The payload size in bytes.
    /// </summary>
    public const byte PayloadByteSize = 69;

    /// <summary>
    /// Initializes a new instance of <see cref="GetFirmwareMetricsResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public GetFirmwareMetricsResponseMessage(IReadOnlyList<byte> payload) : base(payload)
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
        FirmwareMetrics = new FirmwareMetrics
        {
            FlashUpdateCount = data[..8].ConvertToUint32Via4BitEncoding(),
            UsbFlashDriveFirmwareUpdateCount = data[8..16].ConvertToUint32Via4BitEncoding(),
            TotalFlashDriveInsertCount = data[16..24].ConvertToUint32Via4BitEncoding(),
            FirmwareCountryRevision = data[24..28].ConvertToUint16Via4BitEncoding(),
            FirmwareCoreRevision = data[28..32].ConvertToUint16Via4BitEncoding(),
            FirmwareBuildRevision = data[32..36].ConvertToUint16Via4BitEncoding(),
            FirmwareCrc = data[36..44].ConvertToUint32Via4BitEncoding(),
            BootloaderMajorRevision = data[44..48].ConvertToUint16Via4BitEncoding(),
            BootloaderMinorRevision = data[48..52].ConvertToUint16Via4BitEncoding(),
            BootloaderBuildRevision = data[52..56].ConvertToUint16Via4BitEncoding()
        };
    }
    
    /// <inheritdoc cref="Models.FirmwareMetrics"/>
    public FirmwareMetrics FirmwareMetrics { get; } = new();
}