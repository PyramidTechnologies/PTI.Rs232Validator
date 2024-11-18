using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses;

/// <summary>
/// An RS-232 poll message from an acceptor to a host.
/// </summary>
internal class PollResponseMessage : Rs232ResponseMessage
{
    /// <summary>
    /// The expected payload size in bytes.
    /// </summary>
    public const byte PayloadByteSize = 11;

    /// <summary>
    /// A map of (byteIndex, bitIndex) pairs in <see cref="Rs232Message.PayloadSource"/> to <see cref="Rs232State"/>.
    /// </summary>
    private static readonly Dictionary<(byte, byte), Rs232State> StateMap = new()
    {
        { (3, 0), Rs232State.Idling },
        { (3, 1), Rs232State.Accepting },
        { (3, 2), Rs232State.Escrowed },
        { (3, 3), Rs232State.Stacking },
        { (3, 5), Rs232State.Returning },
        { (4, 2), Rs232State.BillJammed },
        { (4, 3), Rs232State.StackerFull },
        { (5, 2), Rs232State.Failure }
    };

    /// <summary>
    /// A map of (byteIndex, bitIndex) pairs in <see cref="Rs232Message.PayloadSource"/> to <see cref="Rs232Event"/>.
    /// </summary>
    private static readonly Dictionary<(byte, byte), Rs232Event> EventMap = new()
    {
        { (3, 4), Rs232Event.Stacked },
        { (3, 6), Rs232Event.Returned },
        { (4, 0), Rs232Event.Cheated },
        { (4, 1), Rs232Event.BillRejected },
        { (5, 0), Rs232Event.PowerUp },
        { (5, 1), Rs232Event.InvalidCommand }
    };

    /// <summary>
    /// A map of byte indices in d<see cref="Rs232Message.PayloadSource"/> to reserved bit indices.
    /// </summary>
    private static readonly Dictionary<byte, byte[]> ReservedBitIndices = new()
    {
        { 3, [7] },
        { 4, [5, 6, 7] },
        { 5, [6, 7] },
        { 6, [0, 1, 2, 3, 4, 5, 6, 7] },
        { 7, [7] },
        { 8, [7] }
    };

    /// <summary>
    /// Initializes a new instance of <see cref="PollResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    internal PollResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        Deserialize();
    }

    /// <summary>
    /// An enumerator of <see cref="Rs232State"/>.
    /// </summary>
    public Rs232State State { get; private set; }

    /// <summary>
    /// A collection of <see cref="Rs232Event"/> enumerators.
    /// </summary>
    public Rs232Event Event { get; private set; }

    /// <summary>
    /// Is the cashbox present?
    /// </summary>
    /// <remarks>
    /// For stackerless models, this will always be true.
    /// </remarks>
    public bool IsCashboxPresent { get; private set; }

    /// <summary>
    /// The bill type in escrow.
    /// </summary>
    public byte BillType { get; private set; }

    /// <summary>
    /// Model number.
    /// </summary>
    public byte Model { get; private set; }

    /// <summary>
    /// Firmware revision.
    /// </summary>
    /// <remarks>
    /// 1.17 returns 17.
    /// </remarks>
    public byte Revision { get; private set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return PayloadIssues.Count > 0
            ? "Invalid Poll Response"
            : $"State: {State}, " +
              $"Event(s): {Event}, " +
              $"Bill Type: {BillType}, " +
              $"Model: 0x{Model:X2}, " +
              $"Revision: 0x{Revision:X2}, " +
              $"Cashbox Present: {IsCashboxPresent}";
    }

    private void Deserialize()
    {
        if (PayloadIssues.Count > 0)
        {
            return;
        }

        if (MessageType != Rs232MessageType.AcceptorToHost)
        {
            PayloadIssues.Add($"The message type is {MessageType}, but {Rs232MessageType.AcceptorToHost} is expected.");
        }

        if (PayloadSource.Length != PayloadByteSize)
        {
            PayloadIssues.Add(
                $"The payload size is {PayloadSource.Length} bytes, but {PayloadByteSize} bytes are expected.");
            return;
        }

        var states = new List<Rs232State>();
        foreach (var pair in StateMap)
        {
            var byteIndex = pair.Key.Item1;
            var bitIndex = pair.Key.Item2;
            if (PayloadSource[byteIndex].IsBitSet(bitIndex))
            {
                states.Add(pair.Value);
            }
        }

        foreach (var pair in EventMap)
        {
            var byteIndex = pair.Key.Item1;
            var bitIndex = pair.Key.Item2;
            if (PayloadSource[byteIndex].IsBitSet(bitIndex))
            {
                Event |= pair.Value;
            }
        }

        IsCashboxPresent = PayloadSource[4].IsBitSet(4);
        Model = PayloadSource[7];
        Revision = PayloadSource[8];

        foreach (var pair in ReservedBitIndices)
        {
            var byteIndex = pair.Key;
            var bitIndices = pair.Value;

            var setBitIndices = bitIndices.Where(bitIndex => PayloadSource[byteIndex].IsBitSet(bitIndex)).ToArray();
            if (setBitIndices.Length == 0)
            {
                continue;
            }

            PayloadIssues.Add(
                $"The payload byte {byteIndex} has 1 or more reserved bits set: {string.Join(",", setBitIndices)}.");
        }

        switch (states.Count)
        {
            case 0:
                PayloadIssues.Add("The payload has no state set.");
                break;
            case > 1:
                PayloadIssues.Add(
                    $"The payload has more than 1 state set: {string.Join(",", states.Select(s => s.ToString()))}.");
                break;
        }

        BillType = (byte)(PayloadSource[5] >> 3);
    }
}