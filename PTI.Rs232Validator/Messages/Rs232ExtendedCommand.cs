namespace PTI.Rs232Validator.Messages;

/// <summary>
/// RS-232 extended commands.
/// </summary>
internal enum Rs232ExtendedCommand : byte
{
    // TODO: Document each command.
    Ping = 0x00,
    GetSerialNumber = 0x01,
    GetCashboxMetrics = 0x02,
    // TODO: Add more commands.
}