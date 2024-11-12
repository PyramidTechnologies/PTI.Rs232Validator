namespace PTI.Rs232Validator.Messages;

/// <summary>
/// The RS-232 extended commands.
/// </summary>
internal enum Rs232TelemetryCommand : byte
{
    // TODO: Document each command.
    Ping = 0x00,
    GetSerialNumber = 0x01,
    GetCashboxMetrics = 0x02,
    // TODO: Add more commands.
}