using System.Collections.ObjectModel;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// An RS-232 poll message from host to acceptor.
/// </summary>
internal class PollRequestMessage : Rs232Message
{
    /// <summary>
    /// A base payload with <see cref="Rs232Message.IsAckNumberOne"/> set to false.
    /// </summary>
    private static readonly ReadOnlyCollection<byte> ZeroAckBaseMessage = new(
        [0x02, 0x08, 0x10, 0x00, 0x00, 0x00, 0x03, 0x00]);

    /// <summary>
    /// A base payload with <see cref="Rs232Message.IsAckNumberOne"/> set to true.
    /// </summary>
    private static readonly ReadOnlyCollection<byte> OneAckBaseMessage = new(
        [0x02, 0x08, 0x11, 0x00, 0x00, 0x00, 0x03, 0x00]);

    private byte _enableMask;
    private bool _isEscrowRequested;
    private bool _isStackRequested;
    private bool _isReturnRequested;

    /// <summary>
    /// Initializes a new instance of <see cref="PollRequestMessage"/>.
    /// </summary>
    /// <param name="isAckNumberOne"><see cref="Rs232Message.IsAckNumberOne"/></param>
    public PollRequestMessage(bool isAckNumberOne) : base(isAckNumberOne ? OneAckBaseMessage : ZeroAckBaseMessage)
    {
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return
            $"Ack Number: {IsAckNumberOne}, " +
            $"Enable Mask: 0b{_enableMask.ToBinary()}, " +
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
    /// 0b00000001: only accept the 1st bill type (i.e. $1).
    /// 0b00000010: only accept the 2nd bill type (i.e. $2).
    /// 0b00000100: only accept the 3rd bill type (i.e. $5).
    /// 0b00001000: only accept the 4th bill type (i.e. $10).
    /// 0b00010000: only accept the 5th bill type (i.e. $20).
    /// 0b00100000: only accept the 6th bill type (i.e. $50).
    /// 0b01000000: only accept the 7th bill type (i.e. $100).
    /// </remarks>
    public PollRequestMessage SetEnableMask(byte enableMask)
    {
        _enableMask = enableMask;
        Payload[3] = (byte)(enableMask & 0x7F);
        Payload[^1] = CalculateChecksum();
        return this;
    }

    /// <summary>
    /// Sets whether to request a bill to be placed in escrow.
    /// </summary>
    /// <param name="isEscrowRequested">True to request escrow.</param>
    /// <returns>This instance.</returns>
    public PollRequestMessage SetEscrowRequested(bool isEscrowRequested)
    {
        _isEscrowRequested = isEscrowRequested;
        Payload[4] = (byte)(isEscrowRequested ? Payload[4] | 0x10 : Payload[4] & ~0x10);
        Payload[^1] = CalculateChecksum();
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
        Payload[4] = (byte)(isStackRequested ? Payload[4] | 0x20 : Payload[4] & ~0x20);
        Payload[^1] = CalculateChecksum();
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
        Payload[4] = (byte)(isReturnRequested ? Payload[4] | 0x40 : Payload[4] & ~0x40);
        Payload[^1] = CalculateChecksum();
        return this;
    }
}