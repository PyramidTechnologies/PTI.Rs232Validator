using PTI.Rs232Validator.Messages;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses.Extended;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.Validators;

public partial class BillValidator
{
    /// <summary>
    /// Gets the last barcode detected.
    /// </summary>
    /// <returns>
    /// A populated string if a barcode was detected in the past;
    /// an empty string if no barcode was detected;
    /// null if an error occurred.
    /// </returns>
    public async Task<string?> GetBarcodeDetected()
    {
        // TODO: Better define options.
        byte[] requestData = [0b00001000, 0b00000010];
        requestData[0] |= (byte)(Configuration.ShouldEscrow ? 0b00010000 : 0);
        
        var responseMessage = await SendExtendedMessageAsync(ExtendedCommand.BarcodeDetected, requestData,
            payload => new BarcodeDetectedResponseMessage(payload));
        return responseMessage?.Barcode ?? null;
    }

    private async Task<TResponseMessage?> SendExtendedMessageAsync<TResponseMessage>(ExtendedCommand command,
        IReadOnlyList<byte> requestData, Func<IReadOnlyList<byte>, TResponseMessage> createResponseMessage)
        where TResponseMessage : ExtendedResponseMessage
    {
        return await SendNonPollMessageAsync(
            ack => new ExtendedRequestMessage(ack, command, requestData), createResponseMessage);
    }
}