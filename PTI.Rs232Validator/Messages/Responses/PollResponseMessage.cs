using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses;

/// <summary>
/// RS-232 poll message from acceptor to host.
/// </summary>
internal class PollResponseMessage : Rs232ResponseMessage
{
    private const int CashBoxBit = 4;
    private static readonly int[] CreditBits = [3, 4, 5];
    
    /// <summary>
    /// Map of (byteIndex, bitIndex) in <see cref="_data"/> to <see cref="Rs232State"/>.
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
    /// Map of (byteIndex, bitIndex) in <see cref="_data"/> to <see cref="Rs232Event"/>.
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
    /// Map of byte index in <see cref="_data"/> to reserved bit indices.
    /// </summary>
    private static readonly Dictionary<int, int[]> ReservedBits = new()
    {
        { 0, [7] },
        { 1, [5, 6, 7] },
        { 2, [6, 7] },
        { 3, [0, 1, 2, 3, 4, 5, 6, 7] },
        { 4, [7] },
        { 5, [7] }
    };
    
    /// <summary>
    /// Data fields.
    /// </summary>
    private readonly byte[] _data;
    
    /// <summary>
    /// Initializes a new instance of <see cref="PollResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    internal PollResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        _data = Payload.Length == 11 ? Payload.Skip(3).Take(6).ToArray() : [];
        Parse();
    }
    
    /// <summary>
    /// Is <see cref="Rs232Message.Payload"/> the correct length with a valid checksum,
    /// but one or more reserved bits are set, OR
    /// more than one state is set, OR
    /// a state is missing an accompanying bit (e.g. stack + credit). 
    /// </summary>
    public override bool HasProtocolViolation { get; protected set; }
    
    /// <summary>
    /// Index of credit being processed, if any.
    /// </summary>
    /// <remarks>
    /// Is null if there are issues with <see cref="Rs232Message.Payload"/>.
    /// </remarks>
    public int? CreditIndex { get; private set; }
    
    /// <inheritdoc cref="Rs232State"/>
    public Rs232State State { get; private set; }
    
    /// <inheritdoc cref="Rs232Event"/>
    public Rs232Event Event { get; private set; }
    
    /// <summary>
    /// Is the cash box present?
    /// </summary>
    /// <remarks>
    /// For stackerless models, this will always be true.
    /// </remarks>
    public bool IsCashBoxPresent { get; private set; }
    
    /// <summary>
    /// Model number.
    /// </summary>
    public int Model { get; private set; }
    
    /// <summary>
    /// Firmware revision.
    /// </summary>
    /// <remarks>
    /// 1.17 returns 17.
    /// </remarks>
    public int Revision { get; private set; }
    
    private void Parse()
    {
        if (Payload.Length != 11)
        {
            PayloadIssues.Add($"Payload length is {Payload.Length} bytes, expected 11");
            return;
        }
        
        var states = new List<Rs232State>(8);
        foreach (var pair in StateMap)
        {
            var byteIndex = pair.Key.Item1;
            var bitIndex = pair.Key.Item2;
            if (!IsBitSet(bitIndex, _data[byteIndex]))
            {
                continue;
            }
            
            State = pair.Value;
            states.Add(pair.Value);
        }
        
        foreach (var pair in EventMap)
        {
            var byteIndex = pair.Key.Item1;
            var bitIndex = pair.Key.Item2;
            if (IsBitSet(bitIndex, _data[byteIndex]))
            {
                Event |= pair.Value;
            }
        }
        
        IsCashBoxPresent = IsBitSet(CashBoxBit, _data[1]);
        Model = _data[4];
        Revision = _data[5];
        
        foreach (var pair in ReservedBits)
        {
            var byteIndex = pair.Key;
            var bitIndices = pair.Value;
            if (!AreAnyBitsSet(bitIndices, _data[byteIndex]))
            {
                continue;
            }
            
            PayloadIssues.Add($"Byte {byteIndex} has one or more reserved bits set: {string.Join(",", bitIndices)}.");
            HasProtocolViolation = true;
        }
        
        if (states.Count == 0)
        {
            PayloadIssues.Add("No state bit is set.");
            HasProtocolViolation = true;
        }
        else if (states.Count > 1)
        {
            PayloadIssues.Add($"More than one state bit is set: {string.Join(",", states.Select(x => x.ToString()))}.");
            HasProtocolViolation = true;
        }
        
        if (!HasProtocolViolation)
        {
            CreditIndex = AreAnyBitsSet(CreditBits, _data[2]) ? _data[2] >> 3 : null;
        }
    }
    
    /// <inheritdoc/>
    public override string ToString()
    {
        return PayloadIssues.Count > 0
            ? "Invalid Poll Response"
            : $"State: {State,12}, Event(s): {Event,-24}, Credit: {CreditIndex,4}, Model: 0x{Model:X2}, Rev.: 0x{Revision:X2}, CB Present: {IsCashBoxPresent,5}";
    }
}