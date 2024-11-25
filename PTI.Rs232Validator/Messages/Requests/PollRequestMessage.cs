using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// An implementation of <see cref="Rs232RequestMessage"/> for polling an acceptor.
/// </summary>
internal class PollRequestMessage : Rs232RequestMessage
{
    /// <summary>
    /// Initializes a new instance of <see cref="PollRequestMessage"/>.
    /// </summary>
    /// <param name="ack"><see cref="Rs232Message.Ack"/></param>
    public PollRequestMessage(bool ack) : base(BuildPayload(ack))
    {
    }
    
    private byte AcceptanceMask { get; set; }
    private bool IsEscrowRequested { get; set; }
    private bool IsStackRequested { get; set; }
    private bool IsReturnRequested { get; set; }
    private bool IsBarcodeDetectionRequested { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return
            $"{nameof(Ack).AddSpacesToCamelCase()}: {Ack}, " +
            $"{nameof(AcceptanceMask).AddSpacesToCamelCase()}: {AcceptanceMask.ConvertToBinaryString(true)}, " +
            $"{nameof(IsEscrowRequested).AddSpacesToCamelCase()}: {IsEscrowRequested}, " +
            $"{nameof(IsStackRequested).AddSpacesToCamelCase()}: {IsStackRequested}, " +
            $"{nameof(IsReturnRequested).AddSpacesToCamelCase()}: {IsReturnRequested}, " +
            $"{nameof(IsBarcodeDetectionRequested).AddSpacesToCamelCase()}: {IsBarcodeDetectionRequested}";
    }

    /// <summary>
    /// Sets the enable mask, which represents types of bills to accept.
    /// </summary>
    /// <param name="acceptanceMask">The new acceptance mask.</param>
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
    public PollRequestMessage SetEnableMask(byte acceptanceMask)
    {
        AcceptanceMask = acceptanceMask;
        MutatePayload(3, (byte)(acceptanceMask & 0x7F));
        return this;
    }

    /// <summary>
    /// Sets whether to request a bill to be escrowed.
    /// </summary>
    /// <param name="isEscrowRequested">True to request a bill escrow.</param>
    /// <returns>This instance.</returns>
    public PollRequestMessage SetEscrowRequested(bool isEscrowRequested)
    {
        IsEscrowRequested = isEscrowRequested;
        MutatePayload(4, isEscrowRequested ? Payload[4].SetBit(4) : Payload[4].ClearBit(4));
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
        IsStackRequested = isStackRequested;
        MutatePayload(4, isStackRequested ? Payload[4].SetBit(5) : Payload[4].ClearBit(5));
        return this;
    }

    /// <summary>
    /// Sets whether to request a bill to be returned.
    /// </summary>
    /// <param name="isReturnRequested">True to request a bill return.</param>
    /// <returns>This instance.</returns>
    /// <remarks>This method is only relevant if a bill is in escrow.</remarks>
    public PollRequestMessage SetReturnRequested(bool isReturnRequested)
    {
        IsReturnRequested = isReturnRequested;
        MutatePayload(4, isReturnRequested ? Payload[4].SetBit(6) : Payload[4].ClearBit(6));
        return this;
    }

    /// <summary>
    /// Sets whether to request barcode detection.
    /// </summary>
    /// <param name="isBarcodeDetectionRequested">True to request barcode detection.</param>
    /// <returns>This instance.</returns>
    public PollRequestMessage SetBarcodeDetectionRequested(bool isBarcodeDetectionRequested)
    {
        IsBarcodeDetectionRequested = isBarcodeDetectionRequested;
        MutatePayload(5, isBarcodeDetectionRequested ? Payload[5].SetBit(1) : Payload[5].ClearBit(1));
        return this;
    }

    private static ReadOnlyCollection<byte> BuildPayload(bool ack)
    {
        return new List<byte>
        {
            Stx,
            8,
            (byte)((byte)Rs232MessageType.HostToAcceptor | (ack ? 1 : 0)),
            0,
            0,
            0,
            Etx,
            0
        }.AsReadOnly();
    }
}