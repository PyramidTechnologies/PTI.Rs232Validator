using PTI.Rs232Validator.Messages;
using Spectre.Console.Cli;

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
        [CommandArgument(0, "<PortName>")]
        public string PortName { get; init; } = string.Empty;
        
        [CommandArgument(1, "<TelemetryCommand>")]
        public TelemetryCommand TelemetryCommand { get; init; }
        
        [CommandArgument(2, "<Arguments>")]
        public string[] Arguments { get; init; } = [];
    }

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings)
    {
        // TODO: Continue.
        throw new System.NotImplementedException();
    }
}