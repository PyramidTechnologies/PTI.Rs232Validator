using System.Collections.ObjectModel;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// RS-232 poll message from host to acceptor.
/// </summary>
internal class PollRequestMessage : Rs232Message
{
    /// <summary>
    /// Base payload with <see cref="Rs232Message.IsAckNumberOne"/> set to 0.
    /// </summary>
    private static readonly ReadOnlyCollection<byte> ZeroAckBaseMessage = new(
        [0x02, 0x08, 0x10, 0x00, 0x00, 0x00, 0x03, 0x00]);

    /// <summary>
    /// Base payload with <see cref="Rs232Message.IsAckNumberOne"/> set to 1.
    /// </summary>
    private static readonly ReadOnlyCollection<byte> OneAckBaseMessage = new(
        [0x02, 0x08, 0x11, 0x00, 0x00, 0x00, 0x03, 0x00]);

    private byte _enableMask;
    private bool _shouldEnableEscrowMode;
    private bool _shouldStack;
    private bool _shouldReturn;

    /// <summary>
    /// Initializes a new instance of <see cref="PollRequestMessage"/>.
    /// </summary>
    /// <param name="isAckNumberOne">True to set ACK number to 1; otherwise, false to set it to 0.</param>
    public PollRequestMessage(bool isAckNumberOne) : base(isAckNumberOne ? OneAckBaseMessage : ZeroAckBaseMessage)
    {
    }

    /// <summary>
    /// Sets enable bit mask representing which bills to accept.
    /// </summary>
    /// <param name="mask">Enable bit mask.</param>
    /// <remarks>
    /// 0b00000001: $1 or first note.
    /// 0b00000010: $2 or second note.
    /// 0b00000100: $5 or third note.
    /// 0b00001000: $10 or fourth note.
    /// 0b00010000: $20 or fifth note.
    /// 0b00100000: $50 or sixth note.
    /// 0b01000000: $100 of seventh note.
    /// </remarks>
    public PollRequestMessage SetEnableMask(byte mask)
    {
        _enableMask = mask;
        Payload[3] = (byte)(mask & 0x7F);
        Payload[Payload.Length - 1] = CalculateChecksum();
        return this;
    }

    /// <summary>
    /// Sets escrow mode bit. 
    /// </summary>
    /// <param name="shouldEnableEscrowMode">True to enable escrow mode; otherwise, false to disable escrow mode.</param>
    public PollRequestMessage SetEscrowMode(bool shouldEnableEscrowMode)
    {
        _shouldEnableEscrowMode = shouldEnableEscrowMode;
        Payload[4] = (byte)(shouldEnableEscrowMode ? Payload[4] | 0x10 : Payload[4] & ~0x10);
        Payload[Payload.Length - 1] = CalculateChecksum();
        return this;
    }

    /// <summary>
    /// Sets stack bit.
    /// </summary>
    /// <param name="shouldStack">True to perform bill stack.</param>
    /// <remarks>Only used in escrow mode.</remarks>
    public PollRequestMessage SetStack(bool shouldStack)
    {
        _shouldStack = shouldStack;
        Payload[4] = (byte)(shouldStack ? Payload[4] | 0x20 : Payload[4] & ~0x20);
        Payload[Payload.Length - 1] = CalculateChecksum();
        return this;
    }

    /// <summary>
    /// Sets return bit.
    /// </summary>
    /// <param name="shouldReturn">True to perform bill return.</param>
    /// <remarks>Only used in escrow mode.</remarks>
    public PollRequestMessage SetReturn(bool shouldReturn)
    {
        _shouldReturn = shouldReturn;
        Payload[4] = (byte)(shouldReturn ? Payload[4] | 0x40 : Payload[4] & ~0x40);
        Payload[Payload.Length - 1] = CalculateChecksum();
        return this;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return
            $"Ack Number: {IsAckNumberOne,5}, Enabled: 0b{_enableMask.ToBinary()}, Escrow: {_shouldEnableEscrowMode,5}, Stack: {_shouldStack,5}, Return: {_shouldReturn,5}";
    }
}