using Moq;
using PTI.Rs232Validator.BillValidators;
using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Messages;
using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses.Extended;
using PTI.Rs232Validator.SerialProviders;
using PTI.Rs232Validator.Test.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.Test.Tests;

public class BillValidatorTests
{
    [Test]
    public async Task AllCommunicationAttemptsAreReported()
    {
        var validResponsePayload = new byte[]
            { 0x02, 11, 0x21, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 };
        validResponsePayload[^1] = Rs232Message.CalculateChecksum(validResponsePayload);

        var invalidResponsePayload = new byte[]
            { 0x03, 11, 0x20, 0b00000000, 0b00000000, 0b00000000, 0, 1, 2, 0x03, 0x00 };
        invalidResponsePayload[^1] = Rs232Message.CalculateChecksum(invalidResponsePayload);

        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(validResponsePayload)
            .ReturnsResponse(invalidResponsePayload)
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var communicationAttempts = 0;
        var wasValidResponseReported = false;
        var wasInvalidResponseReported = false;
        billValidator.OnCommunicationAttempted += (_, args) =>
        {
            communicationAttempts++;
            if (args.ResponseMessage.Payload.SequenceEqual(validResponsePayload))
            {
                wasValidResponseReported = true;
            }
            else if (args.ResponseMessage.Payload.SequenceEqual(invalidResponsePayload))
            {
                wasInvalidResponseReported = true;
            }
        };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(communicationAttempts,
            Is.EqualTo(BillValidator.SuccessfulPollsRequiredToStartPollingLoop + 2 + 1));
        Assert.That(wasValidResponseReported, Is.True);
        Assert.That(wasInvalidResponseReported, Is.True);
    }

    [Test, TestCaseSource(nameof(ResponsePayloadAndStatePairs))]
    public async Task ANewStateEncodedInAPollResponseIsReported(byte[] responsePayload, Rs232State expectedState)
    {
        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(responsePayload)
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var actualState = Rs232State.None;
        billValidator.OnStateChanged += (_, args) => { actualState = args.NewState; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(actualState, Is.EqualTo(expectedState));
    }

    [Test, TestCaseSource(nameof(ResponsePayloadAndEventPairs))]
    public async Task ASingleEventEncodedInAPollResponseMessageIsReported(byte[] responsePayload,
        Rs232Event expectedEvent)
    {
        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(responsePayload)
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var actualEvent = Rs232Event.None;
        billValidator.OnEventReported += (_, e) => { actualEvent = e; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(actualEvent, Is.EqualTo(expectedEvent));
    }

    [Test]
    public async Task MultipleEventsEncodedInAPollResponseMessageAreReported()
    {
        var responsePayload = new byte[] { 0x02, 11, 0x21, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 };
        var expectedEvent = Rs232Event.None;
        foreach (var obj in ResponsePayloadAndEventPairs)
        {
            var payload = (byte[])((object[])obj)[0];
            for (var i = 0; i < payload.Length; i++)
            {
                responsePayload[i] |= payload[i];
            }

            expectedEvent |= (Rs232Event)((object[])obj)[1];
        }

        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(responsePayload)
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var actualEvent = Rs232Event.None;
        billValidator.OnEventReported += (_, e) => { actualEvent |= e; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(actualEvent, Is.EqualTo(expectedEvent));
    }

    [Test]
    public async Task TheCashboxPresenceEncodedInAPollResponseMessageIsReported()
    {
        var responsePayload = new byte[] { 0x02, 11, 0x21, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 };

        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(responsePayload)
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var wasCashboxAttached = false;
        billValidator.OnCashboxAttached += (_, _) => { wasCashboxAttached = true; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(wasCashboxAttached, Is.True);
    }

    [Test]
    public async Task TheCashboxAbsenceEncodedInAPollResponseMessageIsReported()
    {
        var responsePayload = new byte[] { 0x02, 11, 0x21, 0b00000001, 0b00000000, 0b00000000, 0, 1, 2, 0x03, 0x00 };

        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(responsePayload)
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var wasCashboxRemoved = false;
        billValidator.OnCashboxRemoved += (_, _) => { wasCashboxRemoved = true; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(wasCashboxRemoved, Is.True);
    }

    [Test, TestCaseSource(nameof(ResponsePayloadAndStackedBillPairs))]
    public async Task AStackedBillEncodedInAPollResponseMessageIsReported(byte[] responsePayload, byte expectedBillType)
    {
        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(responsePayload)
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var actualBillType = 0;
        billValidator.OnBillStacked += (_, billType) => { actualBillType = billType; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(actualBillType, Is.EqualTo(expectedBillType));
    }

    [Test, TestCaseSource(nameof(ResponsePayloadAndEscrowedBillPairs))]
    public async Task AnEscrowedBillEncodedInAPollResponseMessageIsReported(byte[] responsePayload,
        byte expectedBillType)
    {
        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(responsePayload)
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var actualBillType = 0;
        billValidator.OnBillEscrowed += (_, billType) => { actualBillType = billType; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(actualBillType, Is.EqualTo(expectedBillType));
    }

    [Test]
    public async Task ADetectedBarcodeEncodedInAnExtendedResponseMessageIsReported()
    {
        const string expectedBarcode = "0123456789ABCDEFGHIJKLMNOPQR";

        var responsePayload = new List<byte> { 0x02, 40, 0x71, 0x01, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2 };
        responsePayload.AddRange(Encoding.ASCII.GetBytes(expectedBarcode));
        responsePayload.AddRange([0x03, 0x00]);

        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(responsePayload.ToArray())
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var actualBarcode = string.Empty;
        billValidator.OnBarcodeDetected += (_, barcode) => { actualBarcode = barcode; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(actualBarcode, Is.EqualTo(expectedBarcode));
    }

    [Test]
    public async Task ALostConnectionIsReported()
    {
        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsEmptyResponses();

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var wasConnectionLost = false;
        billValidator.OnConnectionLost += (_, _) => { wasConnectionLost = true; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(wasConnectionLost, Is.True);
    }

    [Test]
    public async Task ANonPollMessageCanBeSentOutsideAndDuringPolling()
    {
        const string expectedBarcode = "0123456789ABCDEFGHIJKLMNOPQR";

        var barcodeResponsePayload = new List<byte>
            { 0x02, 40, 0x71, 0x01, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2 };
        barcodeResponsePayload.AddRange(Encoding.ASCII.GetBytes(expectedBarcode));
        barcodeResponsePayload.AddRange([0x03, 0x00]);

        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(barcodeResponsePayload.ToArray());

        var rs232Configuration = new Rs232Configuration
        {
            PollingPeriod = TimeSpan.Zero
        };
        using var billValidator = new BillValidator(new NullLogger(), mockSerialProvider.Object, rs232Configuration);

        var responseMessage1 = await billValidator.SendNonPollMessageAsync(
            ack => new ExtendedRequestMessage(ack, ExtendedCommand.BarcodeDetected, []),
            payload => new BarcodeDetectedResponseMessage(payload));

        var wasBarcodeRequested = false;
        var requestAck = false;
        mockSerialProvider
            .Setup(sp => sp.Write(It.IsAny<byte[]>()))
            .Callback((byte[] payload) =>
            {
                if (payload.Length < 2)
                {
                    return;
                }

                wasBarcodeRequested = (payload[2] & 0b11110000) == 0x70;
                requestAck = (payload[2] & 0x01) == 1;
            });

        var responsePayloadRemainder = Array.Empty<byte>();
        mockSerialProvider
            .Setup(sp => sp.Read(It.Is<uint>(count => count == 2)))
            .Returns(() =>
            {
                byte[] responsePayload;
                if (wasBarcodeRequested)
                {
                    responsePayload = barcodeResponsePayload.ToArray();
                    responsePayload[2] =
                        requestAck ? (byte)(responsePayload[2] | 0x01) : (byte)(responsePayload[2] & ~0x01);
                }
                else
                {
                    responsePayload = requestAck
                        ? MoqExtensions.OneAckValidPollResponsePayload
                        : MoqExtensions.ZeroAckValidPollResponsePayload;
                }

                responsePayload[^1] = Rs232Message.CalculateChecksum(responsePayload);
                responsePayloadRemainder = responsePayload[2..];
                return responsePayload[..2];
            });

        mockSerialProvider
            .Setup(sp => sp.Read(It.Is<uint>(count => count != 2)))
            .Returns(() => responsePayloadRemainder);

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        var responseMessage2 = await billValidator.SendNonPollMessageAsync(
            ack => new ExtendedRequestMessage(ack, ExtendedCommand.BarcodeDetected, []),
            payload => new BarcodeDetectedResponseMessage(payload));

        Assert.That(responseMessage1.IsValid, Is.True);
        Assert.That(responseMessage2.IsValid, Is.True);
    }

    private static readonly object[] ResponsePayloadAndStatePairs =
    [
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Idling
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000010, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Accepting
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000100, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Escrowed
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00001000, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Stacking
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00100000, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Returning
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000000, 0b00010100, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.BillJammed
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000000, 0b00011000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232State.StackerFull
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000000, 0b00010000, 0b00000100, 0, 1, 2, 0x03, 0x00 },
            Rs232State.Failure
        }
    ];

    private static readonly object[] ResponsePayloadAndEventPairs =
    [
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00010001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232Event.Stacked
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b01000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232Event.Returned
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000001, 0b00010001, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232Event.Cheated
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000001, 0b00010010, 0b00000000, 0, 1, 2, 0x03, 0x00 },
            Rs232Event.BillRejected
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000001, 0b00010000, 0b00000010, 0, 1, 2, 0x03, 0x00 },
            Rs232Event.InvalidCommand
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000001, 0b00010000, 0b00000001, 0, 1, 2, 0x03, 0x00 },
            Rs232Event.PowerUp
        }
    ];

    private static readonly object[] ResponsePayloadAndStackedBillPairs =
    [
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00010001, 0b00010000, 0b00001000, 0, 1, 2, 0x03, 0x00 },
            (byte)1
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00010001, 0b00010000, 0b00010000, 0, 1, 2, 0x03, 0x00 },
            (byte)2
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00010001, 0b00010000, 0b00011000, 0, 1, 2, 0x03, 0x00 },
            (byte)3
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00010001, 0b00010000, 0b00100000, 0, 1, 2, 0x03, 0x00 },
            (byte)4
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00010001, 0b00010000, 0b00101000, 0, 1, 2, 0x03, 0x00 },
            (byte)5
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00010001, 0b00010000, 0b00110000, 0, 1, 2, 0x03, 0x00 },
            (byte)6
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00010001, 0b00010000, 0b00111000, 0, 1, 2, 0x03, 0x00 },
            (byte)7
        }
    ];

    private static readonly object[] ResponsePayloadAndEscrowedBillPairs =
    [
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000100, 0b00010000, 0b00001000, 0, 1, 2, 0x03, 0x00 },
            (byte)1
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000100, 0b00010000, 0b00010000, 0, 1, 2, 0x03, 0x00 },
            (byte)2
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000100, 0b00010000, 0b00011000, 0, 1, 2, 0x03, 0x00 },
            (byte)3
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000100, 0b00010000, 0b00100000, 0, 1, 2, 0x03, 0x00 },
            (byte)4
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000100, 0b00010000, 0b00101000, 0, 1, 2, 0x03, 0x00 },
            (byte)5
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000100, 0b00010000, 0b00110000, 0, 1, 2, 0x03, 0x00 },
            (byte)6
        },
        new object[]
        {
            new byte[] { 0x02, 11, 0x21, 0b00000100, 0b00010000, 0b00111000, 0, 1, 2, 0x03, 0x00 },
            (byte)7
        }
    ];
}