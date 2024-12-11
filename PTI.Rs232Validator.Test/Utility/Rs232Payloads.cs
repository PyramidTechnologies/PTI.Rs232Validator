namespace PTI.Rs232Validator.Test.Utility;

public static class Rs232Payloads
{
    /// <remarks>
    /// This payload contains the <see cref="Rs232State.Idling"/> enumerator
    /// and no <see cref="Rs232Event"/> enumerators.
    /// </remarks>
    public static byte[] ZeroAckValidPollResponsePayload =>
        [0x02, 0x0B, 0x20, 0b00000001, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x39];

    /// <remarks>
    /// This payload contains the <see cref="Rs232State.Idling"/> enumerator
    /// and no <see cref="Rs232Event"/> enumerators.
    /// </remarks>
    public static byte[] OneAckValidPollResponsePayload =>
        [0x02, 0x0B, 0x21, 0b00000001, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x38];

    /// <remarks>
    /// These payloads contain no <see cref="Rs232Event"/> enumerators.
    /// </remarks>
    public static object[] PollResponsePayloadAndStatePairs =>
    [
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000001, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x38 },
            Rs232State.Idling
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000010, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x3B },
            Rs232State.Accepting
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000100, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x3D },
            Rs232State.Escrowed
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00001000, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x31 },
            Rs232State.Stacking
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00100000, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x19 },
            Rs232State.Returning
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000000, 0b00010100, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x3D },
            Rs232State.BillJammed
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000000, 0b00011000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x31 },
            Rs232State.StackerFull
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000000, 0b00010000, 0b00000100, 0x00, 0x01, 0x02, 0x03, 0x3D },
            Rs232State.Failure
        }
    ];

    /// <remarks>
    /// These payloads contain the <see cref="Rs232State.Idling"/> enumerator.
    /// </remarks>
    public static object[] PollResponsePayloadAndEventPairs =>
    [
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00010001, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x28 },
            Rs232Event.Stacked
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b01000001, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x78 },
            Rs232Event.Returned
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000001, 0b00010001, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x39 },
            Rs232Event.Cheated
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000001, 0b00010010, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x3A },
            Rs232Event.BillRejected
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000001, 0b00010000, 0b00000010, 0x00, 0x01, 0x02, 0x03, 0x3A },
            Rs232Event.InvalidCommand
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000001, 0b00010000, 0b00000001, 0x00, 0x01, 0x02, 0x03, 0x39 },
            Rs232Event.PowerUp
        }
    ];

    /// <summary>
    /// This payload contains the <see cref="Rs232State.Idling"/> enumerator.
    /// </summary>
    public static byte[] PollResponsePayloadWithEveryEvent =>
        [0x02, 0x0B, 0x21, 0b01010001, 0b00010011, 0b00000011, 0x00, 0x01, 0x02, 0x03, 0x68];

    /// <remarks>
    /// These payloads contain the <see cref="Rs232State.Idling"/> enumerator
    /// and no <see cref="Rs232Event"/> enumerators.
    /// </remarks>
    public static object[] PollResponsePayloadAndCashboxPresencePairs { get; } =
    [
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000001, 0b00000000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x28 },
            false
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000001, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x38 },
            true
        }
    ];

    /// <remarks>
    /// These payloads contain the <see cref="Rs232State.Idling"/> enumerator
    /// and the <see cref="Rs232Event.Stacked"/> enumerator.
    /// </remarks>
    public static object[] PollResponsePayloadAndStackedBillPairs =>
    [
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00010001, 0b00010000, 0b00001000, 0x00, 0x01, 0x02, 0x03, 0x20 },
            (byte)1
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00010001, 0b00010000, 0b00010000, 0x00, 0x01, 0x02, 0x03, 0x38 },
            (byte)2
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00010001, 0b00010000, 0b00011000, 0x00, 0x01, 0x02, 0x03, 0x30 },
            (byte)3
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00010001, 0b00010000, 0b00100000, 0x00, 0x01, 0x02, 0x03, 0x08 },
            (byte)4
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00010001, 0b00010000, 0b00101000, 0x00, 0x01, 0x02, 0x03, 0x00 },
            (byte)5
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00010001, 0b00010000, 0b00110000, 0x00, 0x01, 0x02, 0x03, 0x18 },
            (byte)6
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00010001, 0b00010000, 0b00111000, 0x00, 0x01, 0x02, 0x03, 0x10 },
            (byte)7
        }
    ];

    /// <remarks>
    /// These payloads contain the <see cref="Rs232State.Escrowed"/> enumerator
    /// and no <see cref="Rs232Event"/> enumerators.
    /// </remarks>
    public static object[] PollResponsePayloadAndEscrowedBillPairs =>
    [
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000100, 0b00010000, 0b00001000, 0x00, 0x01, 0x02, 0x03, 0x35 },
            (byte)1
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000100, 0b00010000, 0b00010000, 0x00, 0x01, 0x02, 0x03, 0x2D },
            (byte)2
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000100, 0b00010000, 0b00011000, 0x00, 0x01, 0x02, 0x03, 0x25 },
            (byte)3
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000100, 0b00010000, 0b00100000, 0x00, 0x01, 0x02, 0x03, 0x1D },
            (byte)4
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000100, 0b00010000, 0b00101000, 0x00, 0x01, 0x02, 0x03, 0x15 },
            (byte)5
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000100, 0b00010000, 0b00110000, 0x00, 0x01, 0x02, 0x03, 0x0D },
            (byte)6
        },
        new object[]
        {
            new byte[] { 0x02, 0x0B, 0x21, 0b00000100, 0b00010000, 0b00111000, 0x00, 0x01, 0x02, 0x03, 0x05 },
            (byte)7
        }
    ];
    
    public static object[] BarcodeDetectedResponsePayloadAndBarcodePair =>
    [
        new byte[]
        {
            0x02, 0x28, 0x71, 0x01, 0x01, 0x10, 0x00, 0x00, 0x01, 0x02, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
            0x51, 0x52, 0x03, 0x58
        },
        "0123456789ABCDEFGHIJKLMNOPQR"
    ];
}