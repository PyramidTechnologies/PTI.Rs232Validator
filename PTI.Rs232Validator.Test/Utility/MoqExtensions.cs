using Moq.Language;
using PTI.Rs232Validator.BillValidators;
using PTI.Rs232Validator.Messages;

namespace PTI.Rs232Validator.Test.Utility;

public static class MoqExtensions
{
    public static readonly byte[] OneAckValidPollResponsePayload =
        [0x02, 11, 0x21, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00];

    public static readonly byte[] ZeroAckValidPollResponsePayload =
        [0x02, 11, 0x20, 0b00000001, 0b00010000, 0b00000000, 0, 1, 2, 0x03, 0x00];

    public static ISetupSequentialResult<byte[]> ReturnsResponse(this ISetupSequentialResult<byte[]> result,
        byte[] responsePayload)
    {
        var payloadCopy = new byte[responsePayload.Length];
        responsePayload.CopyTo(payloadCopy, 0);
        payloadCopy[^1] = Rs232Message.CalculateChecksum(responsePayload);

        return result
            .Returns(payloadCopy[..2])
            .Returns(payloadCopy[2..]);
    }

    public static ISetupSequentialResult<byte[]> ReturnsValidPollResponses(this ISetupSequentialResult<byte[]> result)
    {
        for (var i = 0; i < BillValidator.SuccessfulPollsRequiredToStartPollingLoop; i++)
        {
            var payload = i % 2 == 0 ? OneAckValidPollResponsePayload : ZeroAckValidPollResponsePayload;
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
}