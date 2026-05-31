using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CMS.Services;

internal interface ILoggerService
{
    void Log(string title, string content);
    ObservableCollection<HttpLogEntry> Logs { get; }
}
internal class LoggerService
{
}


internal class HttpLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public required string Title { get; set; } // "Request" или "Response"
    public required string Content { get; set; }
}

internal class DebugLogger
{
    public static ObservableCollection<HttpLogEntry> Logs { get; } = new();

    public static void Log(string title, string content)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            Logs.Insert(0, new HttpLogEntry { Title = title, Content = content }));
    }
}
