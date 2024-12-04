using Moq.Language;
using PTI.Rs232Validator.BillValidators;

namespace PTI.Rs232Validator.Test.Utility;

public static class MoqExtensions
{
    public static ISetupSequentialResult<byte[]> ReturnsResponse(this ISetupSequentialResult<byte[]> result,
        byte[] responsePayload)
    {
        return result
            .Returns(responsePayload[..2])
            .Returns(responsePayload[2..]);
    }

    public static ISetupSequentialResult<byte[]> ReturnsValidPollResponses(this ISetupSequentialResult<byte[]> result)
    {
        for (var i = 0; i < BillValidator.SuccessfulPollsRequiredToStartPollingLoop; i++)
        {
            var payload = i % 2 == 0
                ? Rs232Payloads.OneAckValidPollResponsePayload
                : Rs232Payloads.ZeroAckValidPollResponsePayload;
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