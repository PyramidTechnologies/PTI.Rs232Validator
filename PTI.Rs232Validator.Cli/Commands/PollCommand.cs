using PTI.Rs232Validator.Cli.Utility;
using PTI.Rs232Validator.Utility;
using Spectre.Console.Cli;
using System;
using System.Threading;

namespace PTI.Rs232Validator.Cli.Commands;

/// <summary>
/// A command to poll a bill acceptor.
/// </summary>
public class PollCommand : Command<PollCommand.Settings>
{
    /// <summary>
    /// The settings for <see cref="PollCommand"/>.
    /// </summary>
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<port_name>")]
        public string PortName { get; init; } = string.Empty;

        [CommandArgument(1, "<bill_type_to_return>")]
        public byte BillTypeToReturn { get; init; }
        
        [CommandOption("--detect-barcodes")]
        public bool ShouldDetectBarcodes { get; init; }
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        var billValidator = Factory.CreateBillValidator(settings.PortName);
        var commandLogger = Factory.CreateMultiLogger<PollCommand>();

        billValidator.OnStateChanged += (_, state) =>
        {
            commandLogger.LogInfo($"The state has changed from {state.OldState} to {state.NewState}");
        };

        billValidator.OnEventReported += (_, evt) =>
        {
            commandLogger.LogInfo($"Received the following event(s): {evt}");
        };

        billValidator.OnCashboxRemoved += (_, _) => { commandLogger.LogInfo("The cashbox was removed."); };

        billValidator.OnCashboxAttached += (_, _) => { commandLogger.LogInfo("The cashbox was attached."); };

        billValidator.OnBillStacked += (_, billType) =>
        {
            commandLogger.LogInfo($"A bill of type {billType} was stacked.");
        };

        billValidator.OnBillEscrowed += (_, billType) =>
        {
            if (billType == settings.BillTypeToReturn)
            {
                commandLogger.LogInfo($"Sent a request to return of a bill of type {billType}.");
                billValidator.ReturnBill();
                return;
            }

            commandLogger.LogInfo($"Sent a request to stack a bill of type {billType}.");
            billValidator.StackBill();
        };
        
        billValidator.OnBarcodeDetected += (_, barcode) =>
        {
            commandLogger.LogInfo($"Detected a barcode: {barcode.ConvertToHexString(true)}");
        };

        billValidator.OnConnectionLost += (_, _) => { commandLogger.LogError("Lost connection to the acceptor."); };

        billValidator.Configuration.ShouldEscrow = true;
        billValidator.Configuration.ShouldDetectBarcodes = settings.ShouldDetectBarcodes;
        
        if (!billValidator.StartMessageLoop())
        {
            commandLogger.LogError("Failed to start the message loop.");
            return 1;
        }

        var cancelRequested = false;
        Console.CancelKeyPress += (_, _) => { cancelRequested = true; };

        commandLogger.LogInfo("Now polling the acceptor. Press CTRL+C to exit.");
        while (true)
        {
            if (billValidator.IsConnectionPresent && !cancelRequested)
            {
                Thread.Sleep(billValidator.Configuration.PollingPeriod);
                continue;
            }

            commandLogger.LogError("The acceptor is no longer connected. Exiting now.");
            billValidator.StopMessageLoop();
            break;
        }

        return 0;
    }
}