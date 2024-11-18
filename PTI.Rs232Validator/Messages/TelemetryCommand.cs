namespace PTI.Rs232Validator.Messages;

/// <summary>
/// The RS-232 telemetry commands.
/// </summary>
internal enum TelemetryCommand : byte
{
    /// <summary>
    /// A command to verify communications are working.
    /// </summary>
    Ping = 0x00,
    
    /// <summary>
    /// A command to get the 9-character serial number assigned to the acceptor.
    /// </summary>
    GetSerialNumber = 0x01,
    
    /// <summary>
    /// A command to get the telemetry metrics about the cashbox.
    /// </summary>
    GetCashboxMetrics = 0x02,
    
    // TODO: Add more commands.
}