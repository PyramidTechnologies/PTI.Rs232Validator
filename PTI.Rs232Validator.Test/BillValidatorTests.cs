using Moq;
using Moq.Language;
using PTI.Rs232Validator.BillValidators;
using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.SerialProviders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.Test;

public class BillValidatorTests
{
    [Test, TestCaseSource(nameof(ResponsePayloadAndStatePairs))]
    public async Task ReportsNewStatesFromPollResponseMessages(byte[] responsePayload, Rs232State expectedState)
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
    public async Task ReportsSingleEventsFromPollResponseMessages(byte[] responsePayload, Rs232Event expectedEvent)
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
    public async Task ReportsMultipleEventsFromPollResponseMessages()
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
    public async Task ReportsCashboxAttachmentFromPollResponseMessages()
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
    public async Task ReportsCashboxRemovalFromPollResponseMessages()
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
    public async Task ReportsStackedBillsFromPollResponseMessages(byte[] responsePayload, byte expectedBillType)
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
    public async Task ReportsEscrowedBillsFromPollResponseMessages(byte[] responsePayload, byte expectedBillType)
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
    public async Task ReportsDetectedBarcodesFromExtendedResponseMessages()
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
    public async Task ReportsLostConnectionFromExtendedResponseMessages()
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

internal static class BillValidatorTestsExtensions
{
    private static readonly byte[] ValidPollResponsePayload1 =
        [0x02, 11, 0x21, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00];

    private static readonly byte[] ValidPollResponsePayload2 =
        [0x02, 11, 0x20, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00];

    public static ISetupSequentialResult<byte[]> ReturnsResponse(this ISetupSequentialResult<byte[]> result,
        byte[] responsePayload)
    {
        var payloadCopy = new byte[responsePayload.Length];
        responsePayload.CopyTo(payloadCopy, 0);
        payloadCopy[^1] = responsePayload.CalculateChecksum();

        return result
            .Returns(payloadCopy[..2])
            .Returns(payloadCopy[2..]);
    }

    public static ISetupSequentialResult<byte[]> ReturnsValidPollResponses(this ISetupSequentialResult<byte[]> result)
    {
        for (var i = 0; i < BillValidator.SuccessfulPollsRequiredToStartPollingLoop; i++)
        {
            var payload = i % 2 == 0 ? ValidPollResponsePayload1 : ValidPollResponsePayload2;
            result = result.ReturnsResponse(payload);
        }

        return result;
    }

    public static ISetupSequentialResult<byte[]> ReturnsEmptyResponses(this ISetupSequentialResult<byte[]> result)
    {
        for (var i = 0; i < BillValidator.MaxReadAttempts; i++)
        {
            result = result.Returns([]);
        }

        return result;
    }

    private static byte CalculateChecksum(this byte[] payload)
    {
        byte checksum = 0;
        for (var i = 1; i < payload.Length - 2; ++i)
        {
            checksum ^= payload[i];
        }

        return checksum;
    }
}