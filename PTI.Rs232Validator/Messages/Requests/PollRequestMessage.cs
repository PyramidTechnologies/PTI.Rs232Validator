using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// An RS-232 poll message from host to acceptor.
/// </summary>
internal class PollRequestMessage : Rs232Message
{
    private byte _enableMask;
    private bool _isEscrowRequested;
    private bool _isStackRequested;
    private bool _isReturnRequested;

    /// <summary>
    /// Initializes a new instance of <see cref="PollRequestMessage"/>.
    /// </summary>
    /// <param name="ack"><see cref="Rs232Message.Ack"/></param>
    public PollRequestMessage(bool ack) : base(BuildPayload(ack))
    {
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return
            $"Ack: {Ack}, " +
            $"Enable Mask: {_enableMask.ConvertToBinary(true)}, " +
            $"Is Escrow Requested: {_isEscrowRequested}, " +
            $"Is Stack Requested: {_isStackRequested}, " +
            $"Is Return Requested: {_isReturnRequested}";
    }

    /// <summary>
    /// Sets the enable mask, which represents types of bills to accept.
    /// </summary>
    /// <param name="enableMask">The new enable mask.</param>
    /// <returns>This instance.</returns>
    /// <remarks>
    /// 0b00000001: only accept the 1st bill type (e.g. $1).
    /// 0b00000010: only accept the 2nd bill type (e.g. $2).
    /// 0b00000100: only accept the 3rd bill type (e.g. $5).
    /// 0b00001000: only accept the 4th bill type (e.g. $10).
    /// 0b00010000: only accept the 5th bill type (e.g. $20).
    /// 0b00100000: only accept the 6th bill type (e.g. $50).
    /// 0b01000000: only accept the 7th bill type (e.g. $100).
    /// </remarks>
    public PollRequestMessage SetEnableMask(byte enableMask)
    {
        _enableMask = enableMask;
        PayloadSource[3] = (byte)(enableMask & 0x7F);
        PayloadSource[^1] = CalculateChecksum();
        return this;
    }

    /// <summary>
    /// Sets whether to request a bill to be escrowed.
    /// </summary>
    /// <param name="isEscrowRequested">True to request a bill escrow.</param>
    /// <returns>This instance.</returns>
    public PollRequestMessage SetEscrowRequested(bool isEscrowRequested)
    {
        _isEscrowRequested = isEscrowRequested;
        PayloadSource[4] = isEscrowRequested ? PayloadSource[4].SetBit(4) : PayloadSource[4].ClearBit(4);
        PayloadSource[^1] = CalculateChecksum();
        return this;
    }

    /// <summary>
    /// Sets whether to request a bill to be stacked.
    /// </summary>
    /// <param name="isStackRequested">True to request a bill stack.</param>
    /// <returns>This instance.</returns>
    /// <remarks>This method is only relevant if a bill is in escrow.</remarks>
    public PollRequestMessage SetStackRequested(bool isStackRequested)
    {
        _isStackRequested = isStackRequested;
        PayloadSource[4] = isStackRequested ? PayloadSource[4].SetBit(5) : PayloadSource[4].ClearBit(5);
        PayloadSource[^1] = CalculateChecksum();
        return this;
    }

    /// <summary>
    /// Sets whether to request a bill to be returned.
    /// </summary>
    /// <param name="isReturnRequested">True to request a bill return.</param>
    /// <remarks>This method is only relevant if a bill is in escrow.</remarks>
    public PollRequestMessage SetReturnRequested(bool isReturnRequested)
    {
        _isReturnRequested = isReturnRequested;
        PayloadSource[4] = isReturnRequested ? PayloadSource[4].SetBit(6) : PayloadSource[4].ClearBit(6);
        PayloadSource[^1] = CalculateChecksum();
        return this;
    }

    private static ReadOnlyCollection<byte> BuildPayload(bool isAckNumberOne)
    {
        return new List<byte>
        {
            Stx,
            8,
            (byte)((byte)Rs232MessageType.HostToAcceptor | (isAckNumberOne ? 1 : 0)),
            0,
            0,
            0,
            Etx,
            0
        }.AsReadOnly();
    }
}