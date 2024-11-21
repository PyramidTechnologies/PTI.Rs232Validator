namespace PTI.Rs232Validator.Messages;

/// <summary>
/// The RS-232 extended commands.
/// </summary>
public enum ExtendedCommand : byte
{
    /// <summary>
    /// A command to get the last barcode data.
    /// </summary>
    BarcodeDetected = 0x01
}