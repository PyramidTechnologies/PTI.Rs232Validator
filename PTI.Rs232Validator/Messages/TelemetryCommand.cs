namespace PTI.Rs232Validator.Messages;

/// <summary>
/// The RS-232 telemetry commands.
/// </summary>
internal enum TelemetryCommand : byte
{
    // TODO: Document each command.
    Ping = 0x00,
    GetSerialNumber = 0x01,
    GetCashBoxMetrics = 0x02,
    // TODO: Add more commands.
}