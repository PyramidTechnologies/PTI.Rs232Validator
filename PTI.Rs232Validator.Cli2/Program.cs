using PTI.Rs232Validator.Cli.Commands;
using PTI.Rs232Validator.Messages.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(configuration =>
{
    configuration.Settings.TrimTrailingPeriod = false;
    configuration.Settings.ValidateExamples = true;

    configuration.AddCommand<PollCommand>("poll")
        .WithDescription("Polls a bill acceptor.")
        .WithExample(["poll", "COM1"]);

    configuration.AddCommand<SendTelemetryRequestCommand>("send-telemetry")
        .WithDescription("Sends a telemetry request to a bill acceptor.")
        .WithExample(["send-telemetry", "COM1", ((byte)TelemetryCommand.Ping).ToString()])
        .WithExample(["send-telemetry", "COM1", ((byte)TelemetryCommand.ClearServiceFlags).ToString(), "1"]);

    configuration.AddCommand<SendExtendedRequestCommand>("send-extended")
        .WithDescription("Sends an extended request to a bill acceptor.")
        .WithExample(["send-extended", "COM1", ((byte)ExtendedCommand.BarcodeDetected).ToString()]);
});

return app.Run(args);