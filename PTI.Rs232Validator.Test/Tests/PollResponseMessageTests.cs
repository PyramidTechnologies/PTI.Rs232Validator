using PTI.Rs232Validator.Messages.Responses;
using PTI.Rs232Validator.Test.Utility;
using System;

namespace PTI.Rs232Validator.Test.Tests;

public class PollResponseMessageTests
{
    [Test, TestCaseSource(typeof(Rs232Payloads), nameof(Rs232Payloads.PollResponsePayloadAndStatePairs))]
    public void PollResponseMessage_DeserializesSingleStates(byte[] responsePayload, Rs232State expectedState)
    {
        var pollResponseMessage = new PollResponseMessage(responsePayload);

        Assert.That(pollResponseMessage.IsValid, Is.True);
        Assert.That(pollResponseMessage.State, Is.EqualTo(expectedState));
    }

    [Test, TestCaseSource(typeof(Rs232Payloads), nameof(Rs232Payloads.PollResponsePayloadAndEventPairs))]
    public void PollResponseMessage_DeserializesSingleEvents(byte[] responsePayload, Rs232Event expectedEvent)
    {
        var pollResponseMessage = new PollResponseMessage(responsePayload);

        Assert.That(pollResponseMessage.IsValid, Is.True);
        Assert.That(pollResponseMessage.Event, Is.EqualTo(expectedEvent));
    }

    [Test]
    public void PollResponseMessage_DeserializesMultipleEvents()
    {
        var pollResponseMessage = new PollResponseMessage(Rs232Payloads.PollResponsePayloadWithEveryEvent);

        var expectedEvent = Rs232Event.None;
        foreach (var value in Enum.GetValues(typeof(Rs232Event)))
        {
            expectedEvent |= (Rs232Event)value;
        }

        Assert.That(pollResponseMessage.IsValid, Is.True);
        Assert.That(pollResponseMessage.Event, Is.EqualTo(expectedEvent));
    }

    [Test, TestCaseSource(typeof(Rs232Payloads), nameof(Rs232Payloads.PollResponsePayloadAndCashboxPresencePairs))]
    public void PollResponseMessage_DeserializesCashboxPresence(byte[] responsePayload, bool expectedIsCashboxPresent)
    {
        var pollResponseMessage = new PollResponseMessage(responsePayload);

        Assert.That(pollResponseMessage.IsValid, Is.True);
        Assert.That(pollResponseMessage.IsCashboxPresent, Is.EqualTo(expectedIsCashboxPresent));
    }

    [Test, TestCaseSource(typeof(Rs232Payloads), nameof(Rs232Payloads.PollResponsePayloadAndStackedBillPairs))]
    public void PollResponseMessage_DeserializesBillType(byte[] responsePayload, byte expectedBillType)
    {
        var pollResponseMessage = new PollResponseMessage(responsePayload);

        Assert.That(pollResponseMessage.IsValid, Is.True);
        Assert.That(pollResponseMessage.BillType, Is.EqualTo(expectedBillType));
    }

    [Test]
    public void PollResponseMessage_DeserializesModelNumberAndFirmwareRevision()
    {
        var responsePayload = new byte[] { 0x02, 0x0B, 0x20, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x39 };
        const byte expectedModelNumber = 1;
        const byte expectedFirmwareRevision = 2;

        var pollResponseMessage = new PollResponseMessage(responsePayload);

        Assert.That(pollResponseMessage.IsValid, Is.True);
        Assert.That(pollResponseMessage.ModelNumber, Is.EqualTo(expectedModelNumber));
        Assert.That(pollResponseMessage.FirmwareRevision, Is.EqualTo(expectedFirmwareRevision));
    }
}