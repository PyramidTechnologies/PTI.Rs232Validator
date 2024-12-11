using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetFirmwareMetrics"/>.
/// </summary>
public class GetFirmwareMetricsResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 69;

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
        FlashUpdateCount = data[..8].ConvertToUint32Via4BitEncoding();
        UsbFlashDriveFirmwareUpdateCount = data[8..16].ConvertToUint32Via4BitEncoding();
        TotalFlashDriveInsertCount = data[16..24].ConvertToUint32Via4BitEncoding();
        FirmwareCountryRevision = data[24..28].ConvertToUint16Via4BitEncoding();
        FirmwareCoreRevision = data[28..32].ConvertToUint16Via4BitEncoding();
        FirmwareBuildRevision = data[32..40].ConvertToUint32Via4BitEncoding();
        FirmwareCrc = data[40..48].ConvertToUint32Via4BitEncoding();
        BootloaderMajorRevision = data[48..52].ConvertToUint16Via4BitEncoding();
        BootloaderMinorRevision = data[52..56].ConvertToUint16Via4BitEncoding();
        BootloaderBuildRevision = data[56..64].ConvertToUint32Via4BitEncoding();
    }

    /// <summary>
    /// The total times an acceptor has had a firmware update.
    /// </summary>
    public uint FlashUpdateCount { get; init; }

    /// <summary>
    /// The total times an acceptor has had a firmware update via a flash drive.
    /// </summary>
    public uint UsbFlashDriveFirmwareUpdateCount { get; init; }

    /// <summary>
    /// The total times an acceptor has detected a flash drive insert.
    /// </summary>
    public uint TotalFlashDriveInsertCount { get; init; }

    /// <summary>
    /// The country revision of the firmware.
    /// </summary>
    public ushort FirmwareCountryRevision { get; init; }

    /// <summary>
    /// The core revision of the firmware.
    /// </summary>
    public ushort FirmwareCoreRevision { get; init; }

    /// <summary>
    /// The build revision of the firmware.
    /// </summary>
    public uint FirmwareBuildRevision { get; init; }

    /// <summary>
    /// The CRC of the firmware.
    /// </summary>
    public uint FirmwareCrc { get; init; }

    /// <summary>
    /// The major revision of the bootloader.
    /// </summary>
    public ushort BootloaderMajorRevision { get; init; }

    /// <summary>
    /// The minor revision of the bootloader.
    /// </summary>
    public ushort BootloaderMinorRevision { get; init; }

    /// <summary>
    /// The build revision of the bootloader.
    /// </summary>
    public uint BootloaderBuildRevision { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return IsValid
            ? $"{nameof(FlashUpdateCount).AddSpacesToCamelCase()}: {FlashUpdateCount} | " +
              $"{nameof(UsbFlashDriveFirmwareUpdateCount).AddSpacesToCamelCase()}: {UsbFlashDriveFirmwareUpdateCount} | " +
              $"{nameof(TotalFlashDriveInsertCount).AddSpacesToCamelCase()}: {TotalFlashDriveInsertCount} | " +
              $"{nameof(FirmwareCountryRevision).AddSpacesToCamelCase()}: {FirmwareCountryRevision} | " +
              $"{nameof(FirmwareCoreRevision).AddSpacesToCamelCase()}: {FirmwareCoreRevision} | " +
              $"{nameof(FirmwareBuildRevision).AddSpacesToCamelCase()}: {FirmwareBuildRevision} | " +
              $"{nameof(FirmwareCrc).AddSpacesToCamelCase()}: {FirmwareCrc} | " +
              $"{nameof(BootloaderMajorRevision).AddSpacesToCamelCase()}: {BootloaderMajorRevision} | " +
              $"{nameof(BootloaderMinorRevision).AddSpacesToCamelCase()}: {BootloaderMinorRevision} | " +
              $"{nameof(BootloaderBuildRevision).AddSpacesToCamelCase()}: {BootloaderBuildRevision}"
            : base.ToString();
    }
}