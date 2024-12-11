using Moq;
using PTI.Rs232Validator.BillValidators;
using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses.Extended;
using PTI.Rs232Validator.SerialProviders;
using PTI.Rs232Validator.Test.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.Test.Tests;

public class BillValidatorTests
{
    [Test]
    public async Task AllCommunicationAttemptsAreReported()
    {
        var validResponsePayload = new byte[]
            { 0x02, 0x0B, 0x21, 0b00000001, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x38 };
        var invalidResponsePayload = new byte[]
            { 0x02, 0x0B, 0x20, 0b00000000, 0b00010000, 0b00000000, 0x00, 0x01, 0x02, 0x03, 0x38 };

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

    [Test, TestCaseSource(typeof(Rs232Payloads), nameof(Rs232Payloads.PollResponsePayloadAndStatePairs))]
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

    [Test, TestCaseSource(typeof(Rs232Payloads), nameof(Rs232Payloads.PollResponsePayloadAndEventPairs))]
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
        var expectedEvent = Rs232Event.None;
        foreach (var value in Enum.GetValues(typeof(Rs232Event)))
        {
            expectedEvent |= (Rs232Event)value;
        }

        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(Rs232Payloads.PollResponsePayloadWithEveryEvent)
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

    [Test, TestCaseSource(typeof(Rs232Payloads), nameof(Rs232Payloads.PollResponsePayloadAndCashboxPresencePairs))]
    public async Task TheCashboxPresenceEncodedInAPollResponseMessageIsReported(byte[] responsePayload,
        bool expectedIsCashboxPresent)
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

        bool? wasCashboxAttached = null;
        billValidator.OnCashboxAttached += (_, _) => { wasCashboxAttached = true; };
        billValidator.OnCashboxRemoved += (_, _) => { wasCashboxAttached = false; };

        var didPollingLoopStart = billValidator.StartPollingLoop();
        Assert.That(didPollingLoopStart, Is.True);

        while (billValidator.IsConnectionPresent)
        {
            await Task.Delay(rs232Configuration.PollingPeriod);
        }

        Assert.That(wasCashboxAttached, Is.EqualTo(expectedIsCashboxPresent));
    }

    [Test, TestCaseSource(typeof(Rs232Payloads), nameof(Rs232Payloads.PollResponsePayloadAndStackedBillPairs))]
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

    [Test, TestCaseSource(typeof(Rs232Payloads), nameof(Rs232Payloads.PollResponsePayloadAndEscrowedBillPairs))]
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
        var responsePayload = (byte[])Rs232Payloads.BarcodeDetectedResponsePayloadAndBarcodePair[0];
        var expectedBarcode = (string)Rs232Payloads.BarcodeDetectedResponsePayloadAndBarcodePair[1];

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
        var barcodeDetectedResponsePayload = (byte[])Rs232Payloads.BarcodeDetectedResponsePayloadAndBarcodePair[0];

        var mockSerialProvider = new Mock<ISerialProvider>();
        mockSerialProvider
            .Setup(sp => sp.TryOpen())
            .Returns(true);
        mockSerialProvider
            .SetupSequence(sp => sp.Read(It.IsAny<uint>()))
            .ReturnsValidPollResponses()
            .ReturnsResponse(barcodeDetectedResponsePayload);

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
                if (wasBarcodeRequested && requestAck)
                {
                    responsePayload = barcodeDetectedResponsePayload;
                }
                else
                {
                    responsePayload = requestAck
                        ? Rs232Payloads.OneAckValidPollResponsePayload
                        : Rs232Payloads.ZeroAckValidPollResponsePayload;
                }

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
}