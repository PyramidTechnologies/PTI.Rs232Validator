using PTI.Rs232Validator.Utility;
using System;
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
    /// The expected size of <see cref="Status"/>.
    /// </summary>
    protected const byte StatusByteSize = 6;

    /// <summary>
    /// A map of (byteIndex, bitIndex) pairs in <see cref="Status"/> to <see cref="Rs232State"/>.
    /// </summary>
    private static readonly Dictionary<(byte, byte), Rs232State> StateMap = new()
    {
        { (0, 0), Rs232State.Idling },
        { (0, 1), Rs232State.Accepting },
        { (0, 2), Rs232State.Escrowed },
        { (0, 3), Rs232State.Stacking },
        { (0, 5), Rs232State.Returning },
        { (1, 2), Rs232State.BillJammed },
        { (1, 3), Rs232State.StackerFull },
        { (2, 2), Rs232State.Failure }
    };

    /// <summary>
    /// A map of (byteIndex, bitIndex) pairs in <see cref="Status"/> to <see cref="Rs232Event"/>.
    /// </summary>
    private static readonly Dictionary<(byte, byte), Rs232Event> EventMap = new()
    {
        { (0, 4), Rs232Event.Stacked },
        { (0, 6), Rs232Event.Returned },
        { (1, 0), Rs232Event.Cheated },
        { (1, 1), Rs232Event.BillRejected },
        { (2, 0), Rs232Event.PowerUp },
        { (2, 1), Rs232Event.InvalidCommand }
    };

    /// <summary>
    /// A map of byte indices in d<see cref="Status"/> to reserved bit indices.
    /// </summary>
    private static readonly Dictionary<byte, byte[]> ReservedBitIndices = new()
    {
        { 0, [7] },
        { 1, [5, 6, 7] },
        { 2, [6, 7] },
        { 3, [0, 1, 2, 3, 4, 5, 6, 7] },
        { 4, [7] },
        { 5, [7] }
    };

    /// <summary>
    /// Initializes a new instance of <see cref="PollResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public PollResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (PayloadIssues.Count > 0)
        {
            return;
        }
        
        if (payload.Count != PayloadByteSize)
        {
            PayloadIssues.Add(
                $"The payload size is {payload.Count} bytes, but {PayloadByteSize} bytes are expected.");
            return;
        }
        
        if (MessageType != Rs232MessageType.AcceptorToHost)
        {
            PayloadIssues.Add($"The message type is {MessageType}, but {Rs232MessageType.AcceptorToHost} is expected.");
        }

        Status = payload
            .Skip(3)
            .Take(StatusByteSize)
            .ToList()
            .AsReadOnly();
        DeserializeStatus();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PollResponseMessage"/>.
    /// </summary>
    /// <param name="payload"><see cref="Rs232Message.Payload"/>.</param>
    /// <param name="status"><see cref="Status"/>.</param>
    protected PollResponseMessage(IReadOnlyList<byte> payload, IReadOnlyList<byte> status) : base(payload)
    {
        if (PayloadIssues.Count > 0 || payload.Count < PayloadByteSize || status.Count != 6)
        {
            return;
        }
        
        Status = status;
        DeserializeStatus();
    }
    
    /// <summary>
    /// A 6-byte collection representing the status of the acceptor.
    /// </summary>
    public IReadOnlyList<byte> Status { get; } = [];

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
            ? $"Invalid {nameof(PollResponseMessage).AddSpacesToCamelCase()}"
            : $"{nameof(State).AddSpacesToCamelCase()}: {State} | " +
              $"{nameof(Event).AddSpacesToCamelCase()}(s): {Event} | " +
              $"{nameof(BillType).AddSpacesToCamelCase()}: {BillType} | " +
              $"{nameof(Model).AddSpacesToCamelCase()}: {Model} | " +
              $"{nameof(Revision).AddSpacesToCamelCase()}: {Revision} | " +
              $"{nameof(IsCashboxPresent).AddSpacesToCamelCase()}: {IsCashboxPresent}";
    }

    private void DeserializeStatus()
    {
        var states = new List<Rs232State>();
        foreach (var pair in StateMap)
        {
            var byteIndex = pair.Key.Item1;
            var bitIndex = pair.Key.Item2;
            if (Status[byteIndex].IsBitSet(bitIndex))
            {
                states.Add(pair.Value);
            }
        }

        foreach (var pair in EventMap)
        {
            var byteIndex = pair.Key.Item1;
            var bitIndex = pair.Key.Item2;
            if (Status[byteIndex].IsBitSet(bitIndex))
            {
                Event |= pair.Value;
            }
        }

        IsCashboxPresent = Status[4].IsBitSet(4);
        Model = Status[7];
        Revision = Status[8];

        foreach (var pair in ReservedBitIndices)
        {
            var byteIndex = pair.Key;
            var bitIndices = pair.Value;

            var setBitIndices = bitIndices.Where(bitIndex => Status[byteIndex].IsBitSet(bitIndex)).ToArray();
            if (setBitIndices.Length == 0)
            {
                continue;
            }

            PayloadIssues.Add(
                $"The status byte {byteIndex} has 1 or more reserved bits set: {string.Join(",", setBitIndices)}.");
        }

        switch (states.Count)
        {
            case 0:
                PayloadIssues.Add("The status has no state set.");
                break;
            case > 1:
                PayloadIssues.Add(
                    $"The status has more than 1 state set: {string.Join(",", states.Select(s => s.ToString()))}.");
                break;
        }

        BillType = (byte)(Status[5] >> 3);
    }
}