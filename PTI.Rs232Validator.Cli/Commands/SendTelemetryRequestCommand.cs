using PTI.Rs232Validator.Cli.Utility;
using PTI.Rs232Validator.Messages;
using PTI.Rs232Validator.Models;
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
        if (billValidator is null)
        {
            return 1;
        }

        switch (settings.TelemetryCommand)
        {
            case TelemetryCommand.Ping:
                var pingResult = billValidator.PingAsync().Result;
                if (pingResult)
                {
                    commandLogger.LogInfo("Successfully pinged the acceptor.");
                }
                else
                {
                    commandLogger.LogError("Failed to ping the acceptor.");
                }

                break;

            case TelemetryCommand.GetSerialNumber:
                var serialNumber = billValidator.GetSerialNumberAsync().Result;
                if (!string.IsNullOrEmpty(serialNumber))
                {
                    commandLogger.LogInfo($"The serial number is: {serialNumber}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the serial number.");
                }

                break;

            case TelemetryCommand.GetCashboxMetrics:
                var cashboxMetrics = billValidator.GetCashboxMetrics().Result;
                if (cashboxMetrics is not null)
                {
                    commandLogger.LogInfo($"The cashbox metrics are as follows — {cashboxMetrics}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the cashbox metrics.");
                }

                break;

            case TelemetryCommand.ClearCashboxCount:
                var clearCashboxCountResult = billValidator.ClearCashboxCount().Result;
                if (clearCashboxCountResult)
                {
                    commandLogger.LogInfo("Successfully cleared the cashbox count.");
                }
                else
                {
                    commandLogger.LogError("Failed to clear the cashbox count.");
                }

                break;

            case TelemetryCommand.GetUnitMetrics:
                var unitMetrics = billValidator.GetUnitMetrics().Result;
                if (unitMetrics is not null)
                {
                    commandLogger.LogInfo($"The unit metrics are as follows — {unitMetrics}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the unit metrics.");
                }

                break;

            case TelemetryCommand.GetServiceUsageCounters:
                var serviceUsageCounters = billValidator.GetServiceUsageCounters().Result;
                if (serviceUsageCounters is not null)
                {
                    commandLogger.LogInfo($"The service usage counters are as follows — {serviceUsageCounters}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the service usage counters.");
                }

                break;

            case TelemetryCommand.GetServiceFlags:
                var serviceFlags = billValidator.GetServiceFlags().Result;
                if (serviceFlags is not null)
                {
                    commandLogger.LogInfo($"The service flags are as follows — {serviceFlags}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the service flags.");
                }

                break;

            case TelemetryCommand.ClearServiceFlags:
                var indexString = settings.Arguments.FirstOrDefault();
                if (indexString is null || !byte.TryParse(indexString, out var index))
                {
                    commandLogger.LogError("The index of the service flag to clear is required.");
                    return 1;
                }

                var correctableComponent = (CorrectableComponent)index;
                var clearServiceFlagsResult = billValidator.ClearServiceFlags(correctableComponent).Result;
                if (clearServiceFlagsResult)
                {
                    commandLogger.LogInfo($"Successfully cleared the service flag for index {index}.");
                }
                else
                {
                    commandLogger.LogError($"Failed to clear the service flag for index {index}.");
                }

                break;

            case TelemetryCommand.GetServiceInfo:
                var serviceInfo = billValidator.GetServiceInfo().Result;
                if (serviceInfo is not null)
                {
                    commandLogger.LogInfo($"The service info is as follows — {serviceInfo}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the service info.");
                }

                break;

            case TelemetryCommand.GetFirmwareMetrics:
                var firmwareMetrics = billValidator.GetFirmwareMetrics().Result;
                if (firmwareMetrics is not null)
                {
                    commandLogger.LogInfo($"The firmware metrics are as follows — {firmwareMetrics}");
                }
                else
                {
                    commandLogger.LogError("Failed to get the firmware metrics.");
                }

                break;

            default:
                commandLogger.LogError("The telemetry command is not supported.");
                return 1;
        }

        return 0;
    }
}