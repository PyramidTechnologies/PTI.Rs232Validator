using PTI.Rs232Validator.Cli.Utility;
using PTI.Rs232Validator.Messages.Commands;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Linq;

namespace PTI.Rs232Validator.Cli.Commands;

/// <summary>
/// A command to send a telemetry request to a bill acceptor.
/// </summary>
public class SendTelemetryRequestCommand : Command<SendTelemetryRequestCommand.Settings>
{
    /// <summary>
    /// The settings for <see cref="SendTelemetryRequestCommand"/>.
    /// </summary>
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<port_name>")]
        public string PortName { get; init; } = string.Empty;

        [CommandArgument(1, "<telemetry_command>")]
        [TypeConverter(typeof(ByteConverter))]
        public TelemetryCommand TelemetryCommand { get; init; }

        [CommandArgument(2, "[arguments]")]
        public string[] Arguments { get; init; } = [];
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        var commandLogger = Factory.CreateMultiLogger<SendTelemetryRequestCommand>();
        using var billValidator = Factory.CreateBillValidator(settings.PortName);

        switch (settings.TelemetryCommand)
        {
            case TelemetryCommand.Ping:
            {
                var responseMessage = billValidator.PingAsync().Result;
                if (responseMessage.IsValid)
                {
                    commandLogger.LogInfo("Successfully pinged the acceptor.");
                }
                else
                {
                    commandLogger.LogError("Failed to ping the acceptor.");
                }

                break;
            }

            case TelemetryCommand.GetSerialNumber:
            {
                var responseMessage = billValidator.GetSerialNumberAsync().Result;
                if (responseMessage is { IsValid: true, SerialNumber.Length: > 0 })
                {
                    commandLogger.LogInfo($"The serial number is: {responseMessage.SerialNumber}");
                }
                else if (responseMessage is { IsValid: true, SerialNumber.Length: 0 })
                {
                    commandLogger.LogInfo("The was not assigned a serial number.");
                }
                else
                {
                    commandLogger.LogError("Failed to get the serial number.");
                }

                break;
            }

            case TelemetryCommand.GetCashboxMetrics:
            {
                var responseMessage = billValidator.GetCashboxMetrics().Result;
                if (responseMessage.IsValid)
                {
                    commandLogger.LogInfo($"The cashbox metrics are as follows — {responseMessage}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the cashbox metrics.");
                }

                break;
            }

            case TelemetryCommand.ClearCashboxCount:
            {
                var responseMessage = billValidator.ClearCashboxCount().Result;
                if (responseMessage.IsValid)
                {
                    commandLogger.LogInfo("Successfully cleared the cashbox count.");
                }
                else
                {
                    commandLogger.LogError("Failed to clear the cashbox count.");
                }

                break;
            }

            case TelemetryCommand.GetUnitMetrics:
            {
                var responseMessage = billValidator.GetUnitMetrics().Result;
                if (responseMessage.IsValid)
                {
                    commandLogger.LogInfo($"The unit metrics are as follows — {responseMessage}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the unit metrics.");
                }

                break;
            }

            case TelemetryCommand.GetServiceUsageCounters:
            {
                var responseMessage = billValidator.GetServiceUsageCounters().Result;
                if (responseMessage.IsValid)
                {
                    commandLogger.LogInfo($"The service usage counters are as follows — {responseMessage}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the service usage counters.");
                }

                break;
            }

            case TelemetryCommand.GetServiceFlags:
            {
                var responseMessage = billValidator.GetServiceFlags().Result;
                if (responseMessage.IsValid)
                {
                    commandLogger.LogInfo($"The service flags are as follows — {responseMessage}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the service flags.");
                }

                break;
            }

            case TelemetryCommand.ClearServiceFlags:
            {
                var indexString = settings.Arguments.FirstOrDefault();
                if (indexString is null || !byte.TryParse(indexString, out var index))
                {
                    commandLogger.LogError("The index of the service flag to clear is required.");
                    return 1;
                }

                var correctableComponent = (CorrectableComponent)index;
                var responseMessage = billValidator.ClearServiceFlags(correctableComponent).Result;
                if (responseMessage.IsValid)
                {
                    commandLogger.LogInfo($"Successfully cleared the service flag for index {index}.");
                }
                else
                {
                    commandLogger.LogError($"Failed to clear the service flag for index {index}.");
                }

                break;
            }

            case TelemetryCommand.GetServiceInfo:
            {
                var responseMessage = billValidator.GetServiceInfo().Result;
                if (responseMessage.IsValid)
                {
                    commandLogger.LogInfo($"The service info is as follows — {responseMessage}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the service info.");
                }

                break;
            }

            case TelemetryCommand.GetFirmwareMetrics:
            {
                var responseMessage = billValidator.GetFirmwareMetrics().Result;
                if (responseMessage.IsValid)
                {
                    commandLogger.LogInfo($"The firmware metrics are as follows — {responseMessage}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the firmware metrics.");
                }

                break;
            }

            default:
                commandLogger.LogError("The telemetry command is not supported.");
                return 1;
        }

        return 0;
    }
}