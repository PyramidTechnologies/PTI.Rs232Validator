using PTI.Rs232Validator.Loggers;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace PTI.Rs232Validator.Desktop.Views;

public record LogEntry(LogLevel Level, string Timestamp, string Message);

// This portion provides logging.
public partial class MainWindow : ILogger
{
    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    private const string TimestampFormat = "MM/dd/yyyy hh:mm:ss tt";
    
    private ScrollViewer? _scrollViewer;
    private bool _isAutoScrollEnabled = true;
        
    public void LogTrace(string format, params object[] args)
    {
        // Do nothing.
    }

    public void LogDebug(string format, params object[] args)
    {
        // Do nothing.
    }

    public void LogInfo(string format, params object[] args)
    {
        DoOnUiThread(() =>
        {
            LogEntries.Add(new LogEntry(LogLevel.Info, DateTimeOffset.Now.ToString(TimestampFormat), string.Format(format, args)));
        });
    }

    public void LogError(string format, params object[] args)
    {
        DoOnUiThread(() =>
        {
            LogEntries.Add(new LogEntry(LogLevel.Error, DateTimeOffset.Now.ToString(TimestampFormat), string.Format(format, args)));
        });
    }

    private void SetUpLogAutoScroll()
    {
        LogEntries.CollectionChanged += (_, e) =>
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
            {
                return;
            }
            
            DoOnUiThread(() =>
            {
                if (_isAutoScrollEnabled)
                {
                    _scrollViewer?.ScrollToBottom();
                }
            });
        };
    }
    
    private void LogListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _scrollViewer ??= (ScrollViewer)e.OriginalSource;

        var lastExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
        _isAutoScrollEnabled = e.VerticalOffset + e.ViewportHeight >= lastExtentHeight;
    }
}