using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses;

/// <summary>
/// An RS-232 poll message from an acceptor to a host.
/// </summary>
public class PollResponseMessage : Rs232ResponseMessage
{
    private const byte PayloadByteSize = 11;

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
    /// Initializes a new instance of <see cref="PollResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public PollResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (!IsValid)
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
    public byte ModelNumber { get; private set; }

    /// <summary>
    /// Firmware revision.
    /// </summary>
    /// <remarks>
    /// 1.17 returns 17.
    /// </remarks>
    public byte FirmwareRevision { get; private set; }
    
    /// <summary>
    /// A 6-byte collection representing the status of the acceptor.
    /// </summary>
    internal IReadOnlyList<byte> Status { get; } = [];

    /// <inheritdoc/>
    public override string ToString()
    {
        return IsValid
            ? $"{nameof(State).AddSpacesToCamelCase()}: {State} | " +
              $"{nameof(Event).AddSpacesToCamelCase()}(s): {Event} | " +
              $"{nameof(BillType).AddSpacesToCamelCase()}: {BillType} | " +
              $"{nameof(ModelNumber).AddSpacesToCamelCase()}: {ModelNumber} | " +
              $"{nameof(FirmwareRevision).AddSpacesToCamelCase()}: {FirmwareRevision} | " +
              $"{nameof(IsCashboxPresent).AddSpacesToCamelCase()}: {IsCashboxPresent}"
            : base.ToString();
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

        IsCashboxPresent = Status[1].IsBitSet(4);
        ModelNumber = Status[4];
        FirmwareRevision = Status[5];

        switch (states.Count)
        {
            case 0:
                PayloadIssues.Add("The status has no state set.");
                break;
            case > 1:
                PayloadIssues.Add(
                    $"The status has more than 1 state set: {string.Join(",", states.Select(s => s.ToString()))}.");
                break;
            default:
                State = states[0];
                break;
        }

        BillType = (byte)(Status[2] >> 3);
    }
}