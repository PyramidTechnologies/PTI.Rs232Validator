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
    /// <returns>A populated collection of barcode bytes if successful; otherwise, an empty collection.</returns>
    public async Task<IReadOnlyList<byte>> GetBarcodeDetected()
    {
        var responseMessage = await SendExtendedMessageAsync(ExtendedCommand.BarcodeDetected, [], BarcodeDetectedResponseMessage.PayloadByteSize, payload => new BarcodeDetectedResponseMessage(payload));
        return responseMessage?.Barcode ?? [];
    }
    
    private async Task<TResponseMessage?> SendExtendedMessageAsync<TResponseMessage>(ExtendedCommand command,
        IReadOnlyList<byte> requestData, byte expectedResponseByteSize,
        Func<IReadOnlyList<byte>, TResponseMessage> createResponseMessage)
        where TResponseMessage : ExtendedResponseMessage
    {
        var requestMessage = new ExtendedRequestMessage(!_lastAck, command, requestData);
        return await SendNonPollMessageAsync(requestMessage, expectedResponseByteSize, createResponseMessage);
    }
}