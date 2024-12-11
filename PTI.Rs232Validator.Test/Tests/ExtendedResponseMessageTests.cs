using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Messages.Responses.Extended;
using PTI.Rs232Validator.Test.Utility;

namespace PTI.Rs232Validator.Test.Tests;

public class ExtendedResponseMessageTests
{
    [Test]
    public void ExtendedResponseMessage_DeserializesCommandAndStatusAndData()
    {
        var responsePayload = new byte[]
        {
            0x02, 0x28, 0x71, 0x01, 0x01, 0x10, 0x00, 0x00, 0x01, 0x02, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
            0x51, 0x52, 0x03, 0x58
        };

        const ExtendedCommand expectedCommand = ExtendedCommand.BarcodeDetected;
        var expectedStatus = new byte[] { 0x01, 0x10, 0x00, 0x00, 0x01, 0x02 };
        var expectedData = new byte[]
        {
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
            0x51, 0x52
        };

        var extendedResponseMessage = new ExtendedResponseMessage(responsePayload);

        Assert.That(extendedResponseMessage.IsValid, Is.True);
        Assert.That(extendedResponseMessage.Command, Is.EqualTo(expectedCommand));
        Assert.That(extendedResponseMessage.Status, Is.EqualTo(expectedStatus));
        Assert.That(extendedResponseMessage.Data, Is.EqualTo(expectedData));
    }

    [Test]
    public void BarcodeDetectedResponseMessage_DeserializesBarcode()
    {
        var responsePayload = (byte[])Rs232Payloads.BarcodeDetectedResponsePayloadAndBarcodePair[0];
        var barcode = (string)Rs232Payloads.BarcodeDetectedResponsePayloadAndBarcodePair[1];

        var barcodeDetectedResponseMessage = new BarcodeDetectedResponseMessage(responsePayload);

        Assert.That(barcodeDetectedResponseMessage.IsValid, Is.True);
        Assert.That(barcodeDetectedResponseMessage.Barcode, Is.EqualTo(barcode));
    }
}