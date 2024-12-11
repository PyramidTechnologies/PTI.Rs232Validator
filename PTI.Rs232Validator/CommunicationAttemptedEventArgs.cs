using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses;
using System;

namespace PTI.Rs232Validator;

/// <summary>
/// An implementation of <see cref="EventArgs"/> that contains info about an attempt to communicate with an acceptor.
/// </summary>
public class CommunicationAttemptedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="CommunicationAttemptedEventArgs"/>.
    /// </summary>
    /// <param name="requestMessage"><see cref="RequestMessage"/>.</param>
    /// <param name="responseMessage"><see cref="ResponseMessage"/>.</param>
    public CommunicationAttemptedEventArgs(Rs232RequestMessage requestMessage, Rs232ResponseMessage responseMessage)
    {
        RequestMessage = requestMessage;
        ResponseMessage = responseMessage;
    }
    
    /// <summary>
    /// An instance of <see cref="Rs232RequestMessage"/>, the <see cref="Rs232RequestMessage.Payload"/> of which was
    /// sent to the acceptor.
    /// </summary>
    public Rs232RequestMessage RequestMessage { get; }
    
    /// <summary>
    /// An instance of <see cref="Rs232ResponseMessage"/>, the <see cref="Rs232ResponseMessage.Payload"/> of which was
    /// either received from the acceptor or created as an empty collection due to a timeout.
    /// </summary>
    /// <remarks>Consider checking <see cref="Rs232ResponseMessage.IsValid"/> of the instance.</remarks>
    public Rs232ResponseMessage ResponseMessage { get; }
}