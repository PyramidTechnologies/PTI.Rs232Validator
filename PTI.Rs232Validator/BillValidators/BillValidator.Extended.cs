﻿using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses.Extended;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.BillValidators;

public partial class BillValidator
{
    /// <summary>
    /// Gets the last detected barcode after a power cycle.
    /// </summary>
    /// <returns>
    /// A populated string if a barcode was detected since the last power cycle;
    /// an empty string if no barcode was detected;
    /// null if an error occurred.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<string?> GetDetectedBarcode()
    {
        var responseMessage = await SendExtendedMessageAsync(ExtendedCommand.BarcodeDetected, [],
            payload => new BarcodeDetectedResponseMessage(payload));
        return responseMessage?.Barcode;
    }

    private async Task<TResponseMessage?> SendExtendedMessageAsync<TResponseMessage>(ExtendedCommand command,
        IReadOnlyList<byte> requestData, Func<IReadOnlyList<byte>, TResponseMessage> createResponseMessage)
        where TResponseMessage : ExtendedResponseMessage
    {
        return await SendNonPollMessageAsync(
            ack => new ExtendedRequestMessage(ack, command, requestData), createResponseMessage);
    }
}