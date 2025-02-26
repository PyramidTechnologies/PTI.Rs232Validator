﻿namespace PTI.Rs232Validator.Messages.Commands;

/// <summary>
/// The RS-232 extended commands.
/// </summary>
public enum ExtendedCommand : byte
{
    /// <summary>
    /// A command to get the last barcode string.
    /// </summary>
    BarcodeDetected = 0x01
}