using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Utility;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace PTI.Rs232Validator.Desktop.Views;

/// <summary>
/// A log entry.
/// </summary>
public record LogEntry(LogLevel Level, string Timestamp, string Message);

/// <summary>
/// A payload exchange between a host and an acceptor.
/// </summary>
public record PayloadExchange(
    string Timestamp,
    string RequestPayload,
    string RequestDecodedInfo,
    string ResponsePayload,
    string ResponseDecodedInfo);

// This portion provides logging.
public partial class MainWindow : ILogger
{
    private const string TimestampFormat = "MM/dd/yyyy hh:mm:ss tt";

    private ScrollViewer? _logScrollViewer;
    private ScrollViewer? _payloadScrollViewer;
    private bool _isAutoScrollEnabledForLogs = true;
    private bool _isAutoScrollEnabledForPayloads = true;

    /// <summary>
    /// A collection of <see cref="LogEntry"/> instances.
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    /// <summary>
    /// A collection of <see cref="PayloadExchange"/> instances.
    /// </summary>
    public ObservableCollection<PayloadExchange> PayloadExchanges { get; } = [];

    /// <inheritdoc />
    public void LogTrace(string format, params object[] args)
    {
        // Do nothing.
    }

    /// <inheritdoc />
    public void LogDebug(string format, params object[] args)
    {
        // Do nothing.
    }

    /// <inheritdoc />
    public void LogInfo(string format, params object[] args)
    {
        Log(LogLevel.Info, format, args);
    }

    /// <inheritdoc />
    public void LogError(string format, params object[] args)
    {
        Log(LogLevel.Error, format, args);
    }

    private void Log(LogLevel level, string format, params object[] args)
    {
        DoOnUiThread(() =>
        {
            LogEntries.Add(new LogEntry(level, DateTimeOffset.Now.ToString(TimestampFormat),
                string.Format(format, args)));

            foreach (var column in LogGridView.Columns)
            {
                column.Width = column.ActualWidth;
                column.Width = double.NaN;
            }
        });
    }

    private void BillValidator_OnCommunicationAttempted(object? sender, CommunicationAttemptedEventArgs e)
    {
        DoOnUiThread(() =>
        {
            PayloadExchanges.Add(new PayloadExchange(
                DateTimeOffset.Now.ToString(TimestampFormat),
                e.RequestMessage.Payload.ConvertToHexString(false, true),
                e.RequestMessage.ToString(),
                e.ResponseMessage.Payload.ConvertToHexString(false, true),
                e.ResponseMessage.ToString()));

            foreach (var column in PayloadGridView.Columns)
            {
                column.Width = column.ActualWidth;
                column.Width = double.NaN;
            }
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
                if (_isAutoScrollEnabledForLogs)
                {
                    _logScrollViewer?.ScrollToBottom();
                }
            });
        };

        PayloadExchanges.CollectionChanged += (_, e) =>
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
            {
                return;
            }

            DoOnUiThread(() =>
            {
                if (_isAutoScrollEnabledForPayloads)
                {
                    _payloadScrollViewer?.ScrollToBottom();
                }
            });
        };
    }

    private void LogListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _logScrollViewer ??= (ScrollViewer)e.OriginalSource;

        var lastExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
        _isAutoScrollEnabledForLogs = e.VerticalOffset + e.ViewportHeight >= lastExtentHeight;
    }

    private void PayloadListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _payloadScrollViewer ??= (ScrollViewer)e.OriginalSource;

        var lastExtentHeight = e.ExtentHeight - e.ExtentHeightChange;
        _isAutoScrollEnabledForPayloads = e.VerticalOffset + e.ViewportHeight >= lastExtentHeight;
    }
}