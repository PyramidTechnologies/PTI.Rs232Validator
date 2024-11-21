﻿using PTI.Rs232Validator.Cli.Commands;
using PTI.Rs232Validator.Messages;
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
        .WithExample(["telemetry", "COM1", ((byte)TelemetryCommand.Ping).ToString()])
        .WithExample(["telemetry", "COM1", ((byte)TelemetryCommand.ClearServiceFlags).ToString(), "1"]);
    
    configuration.AddCommand<SendExtendedRequestCommand>("send-extended")
        .WithDescription("Sends an extended request to a bill acceptor.")
        .WithExample(["extended", "COM1", ((byte)ExtendedCommand.BarcodeDetected).ToString()]);
});

return app.Run(args);