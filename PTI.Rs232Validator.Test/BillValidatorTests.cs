using Moq;
using PTI.Rs232Validator.BillValidators;
using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.SerialProviders;
using System;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.Test;

public class BillValidatorTests
{
    [Test, TestCaseSource(nameof(StateUniquePollResponsePayloads))]
    public async Task ReportsNewStatesFromPollResponseMessages(byte[] responsePayload, Rs232State expectedState)
    {
        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .Returns(ValidPollResponsePayload1[..2]).Returns(ValidPollResponsePayload1[2..])
            .Returns(ValidPollResponsePayload2[..2]).Returns(ValidPollResponsePayload2[2..])
            .Returns(responsePayload[..2]).Returns(responsePayload[2..])
            .Returns([]);

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var actualState = Rs232State.None;
        billValidator.OnStateChanged += (_, args) =>
        {
            actualState = args.NewState;
        };
        
        var didPollingLoopStart = billValidator.StartPollingLoop();
        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(didPollingLoopStart, Is.True);
        Assert.That(actualState, Is.EqualTo(expectedState));
    }

    private static readonly byte[] ValidPollResponsePayload1 =
        [0x02, 11, 0x10, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00];
    
    private static readonly byte[] ValidPollResponsePayload2 =
        [0x02, 11, 0x11, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00];

    private static readonly object[] StateUniquePollResponsePayloads =
    [
        new object[]
        {
            new byte[] { 0x02, 11, 0x11, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Idling
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x11, 0b00000010, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Accepting
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x11, 0b00000100, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Escrowed
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x11, 0b00001000, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Stacking
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x11, 0b01000000, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Returning
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x11, 0b00000000, 0b00010100, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.BillJammed
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x11, 0b00000000, 0b00011000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.StackerFull
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x11, 0b00000000, 0b00010000, 0b00000100, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Failure
        }
    ];
}