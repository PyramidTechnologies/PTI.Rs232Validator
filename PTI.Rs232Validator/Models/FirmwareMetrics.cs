namespace PTI.Rs232Validator.Models;

/// <summary>
/// The telemetry data and metrics that pertain to an acceptor's firmware.
/// </summary>
public class FirmwareMetrics
{
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
    public ushort FirmwareBuildRevision { get; init; }
    
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
    public ushort BootloaderBuildRevision { get; init; }
}