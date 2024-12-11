using System;

namespace PTI.Rs232Validator.SerialProviders;

/// <summary>
/// A provider of serial communication to an external device.
/// </summary>
public interface ISerialProvider : IDisposable
{
    /// <summary>
    /// Is there an open connection to the external device?
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Tries to open a connection to the external device.
    /// </summary>
    /// <returns>True if successful; otherwise, false.</returns>
    bool TryOpen();

    /// <summary>
    /// Closes the connection to the external device.
    /// </summary>
    void Close();

    /// <summary>
    /// Reads data from the external device.
    /// </summary>
    /// <param name="count">The count of bytes to read.</param>
    /// <returns>
    /// If successful, an array with the requested <paramref name="count"/> of bytes;
    /// otherwise, an array with less than the requested <paramref name="count"/> of bytes.
    /// </returns>
    byte[] Read(uint count);

    /// <summary>
    /// Writes data to the external device.
    /// </summary>
    /// <param name="data">The data to write.</param>
    void Write(byte[] data);
}