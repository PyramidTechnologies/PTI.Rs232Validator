namespace PTI.Rs232Validator.Messages.Commands;

/// <summary>
/// The RS-232 telemetry commands.
/// </summary>
public enum TelemetryCommand : byte
{
    /// <summary>
    /// A command to verify communications are working.
    /// </summary>
    Ping = 0x00,
    
    /// <summary>
    /// A command to get the 9-character serial number assigned to an acceptor.
    /// </summary>
    GetSerialNumber = 0x01,
    
    /// <summary>
    /// A command to get the telemetry metrics about the cashbox.
    /// </summary>
    GetCashboxMetrics = 0x02,
    
    /// <summary>
    /// A command to clear the count of bills in the cashbox.
    /// </summary>
    ClearCashboxCount = 0x03,
    
    /// <summary>
    /// A command to get the general telemetry metrics for an acceptor.
    /// </summary>
    GetUnitMetrics = 0x04,
    
    /// <summary>
    /// A command to get the telemetry metrics since the last time an acceptor was serviced.
    /// </summary>
    GetServiceUsageCounters = 0x05,
    
    /// <summary>
    /// A command to get the flags about what needs to be serviced.
    /// </summary>
    GetServiceFlags = 0x06,
    
    /// <summary>
    /// A command to clear 1 or more service flags.
    /// </summary>
    ClearServiceFlags = 0x07,
    
    /// <summary>
    /// A command to get the info that was attached to the last service.
    /// </summary>
    GetServiceInfo = 0x08,
    
    /// <summary>
    /// A command to get the telemetry metrics that pertain to an acceptor's firmware.
    /// </summary>
    GetFirmwareMetrics = 0x09
}