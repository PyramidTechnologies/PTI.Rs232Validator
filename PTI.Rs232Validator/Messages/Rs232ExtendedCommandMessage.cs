namespace PTI.Rs232Validator.Messages;

using System;
using System.Linq;

internal abstract class Rs232ExtendedCommandMessage : Rs232Message
{
    protected Rs232ExtendedCommandMessage(byte[] payload) : base(payload)
    {
        Command = payload[3];

        if (payload.Length < 6)
        {
            throw new ArgumentException($"{nameof(payload)} is not at least 6 bytes long.", nameof(payload));
        }

        Data = payload
            .Skip(4)
            .Take(payload.Length - 6)
            .ToArray();
    }

    /// <summary>
    ///     TODO: Add description.
    /// </summary>
    public byte Command { get; }

    /// <summary>
    ///     TODO: Add description.
    /// </summary>
    public byte[] Data { get; }
}