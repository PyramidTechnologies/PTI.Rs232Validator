using PTI.Rs232Validator.Cli.Utility;
using PTI.Rs232Validator.Messages.Commands;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace PTI.Rs232Validator.Cli.Commands;

/// <summary>
/// A command to send an extended request to a bill acceptor.
/// </summary>
public class SendExtendedRequestCommand : Command<SendExtendedRequestCommand.Settings>
{
    /// <summary>
    /// The settings for <see cref="SendExtendedRequestCommand"/>.
    /// </summary>
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<port_name>")]
        public string PortName { get; init; } = string.Empty;

        [CommandArgument(1, "<extended_command>")]
        [TypeConverter(typeof(ByteConverter))]
        public ExtendedCommand ExtendedCommand { get; init; }

        [CommandArgument(2, "[arguments]")]
        public byte[] Arguments { get; init; } = [];
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        var commandLogger = Factory.CreateMultiLogger<SendTelemetryRequestCommand>();
        using var billValidator = Factory.CreateBillValidator(settings.PortName);

        switch (settings.ExtendedCommand)
        {
            case ExtendedCommand.BarcodeDetected:
                var responseMessage = billValidator.GetDetectedBarcode().Result;
                if (responseMessage is { IsValid: true, Barcode.Length: > 0 })
                {
                    commandLogger.LogInfo($"The barcode is: {responseMessage.Barcode}");
                }
                else if (responseMessage is { IsValid: true, Barcode.Length: 0 })
                {
                    commandLogger.LogInfo("No barcode was detected since the last power cycle.");
                }
                else
                {
                    commandLogger.LogError("Failed to get the barcode.");
                }

                break;

            default:
                commandLogger.LogError("The specified command is not supported.");
                return 1;
        }

        return 0;
    }
}