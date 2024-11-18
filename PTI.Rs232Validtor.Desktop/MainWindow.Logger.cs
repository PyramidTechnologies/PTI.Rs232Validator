using PTI.Rs232Validator;
using PTI.Rs232Validator.Loggers;
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace PTI.Rs232Validator.Desktop;

// This portion provides logging.
public partial class MainWindow : ILogger
{
    public ObservableCollection<LogEntry> LogEntries { get; } = [];
        
    public void LogTrace(string format, params object[] args)
    {
        // Do nothing.
    }

    public void LogDebug(string format, params object[] args)
    {
        DoOnUiThread(() =>
        {
            LogEntries.Add(new LogEntry(LogLevel.Debug, DateTimeOffset.Now, string.Format(format, args)));
        });
    }

    public void LogInfo(string format, params object[] args)
    {
        DoOnUiThread(() =>
        {
            LogEntries.Add(new LogEntry(LogLevel.Info, DateTimeOffset.Now, string.Format(format, args)));
        });
    }

    public void LogError(string format, params object[] args)
    {
        DoOnUiThread(() =>
        {
            LogEntries.Add(new LogEntry(LogLevel.Error, DateTimeOffset.Now, string.Format(format, args)));
        });
    }
    
    private void LoggerListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoggerListView.SelectedIndex = LoggerListView.SelectedIndex;
    }
}

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Error
}
    
public record LogEntry(LogLevel Level, DateTimeOffset Timestamp, string Message);