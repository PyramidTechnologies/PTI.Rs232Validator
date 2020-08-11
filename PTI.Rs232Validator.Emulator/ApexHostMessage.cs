namespace PTI.Rs232Validator.Emulator
{
    using Messages;

    /// <summary>
    ///     Wraps raw host message data
    /// </summary>
    internal class ApexHostMessage : Rs232BaseMessage
    {
        /// <inheritdoc />
        public ApexHostMessage(byte[] data) : base(data)
        {
        }
    }
}