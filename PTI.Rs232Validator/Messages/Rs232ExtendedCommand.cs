namespace PTI.Rs232Validator.Messages;

internal enum Rs232ExtendedCommand : byte
{
    Ping = 0x00,
    GetSerialNumber = 0x01,
    GetCashboxMetrics = 0x02,
    // TODO: Add more commands.
}