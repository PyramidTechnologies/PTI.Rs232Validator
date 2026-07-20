using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PTI.Rs232Validator.Messages.Requests;

public class ResetRequestMessage : Rs232RequestMessage
{

    public ResetRequestMessage(bool ack) : base(BuildPayload(ack))
    {
        
    }

    public override string ToString()
    {
        return base.ToString() + $" | Reset";
    }

    private static ReadOnlyCollection<byte> BuildPayload(bool ack)
    {
        var payload = new List<byte>
        {
            Stx,
            0x08,
            (byte)(0x60 | (ack ? 0x01 : 0x00)),
            0x7F,
            0x7F,
            0x7F,
            Etx,
            0
        };
        
        return payload.AsReadOnly();
    }
}