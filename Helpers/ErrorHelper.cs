using CMS.Services; // для DebugLogger
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Helpers;

public static class ErrorHelper
{
    // Путь к файлу логов (можно вынести в настройки)
    private static readonly string LogFilePath = @"r:\temp\cms_crash.log";
    private static IDialogService? _dialogService;


    private static IDialogService DialogService
    {
        get
        {
            if (_dialogService is null)
            {
                _dialogService = Application.Current?.Handler?.MauiContext?.Services
                    .GetRequiredService<IDialogService>();
            }
            return _dialogService ?? throw new InvalidOperationException("DialogService недоступен");
        }
    }

    /// <summary>
    /// Логирует ошибку в файл и в DebugLogger, затем показывает сообщение пользователю.
    /// </summary>
    public static async Task LogAndReportAsync(Exception ex, string title = "Mistake",
        Func<Exception, string>? formatMessage = null, bool displayIt = false)
    {
        if (ex == null) return;

        var message = formatMessage?.Invoke(ex) ?? ex.Message;

        // 1. Запись в файл
        try
        {
            var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{title}] {ex}{Environment.NewLine}";
            await File.AppendAllTextAsync(LogFilePath, entry);
        }
        catch { /* игнорируем ошибки записи в файл */ }

        // 2. Запись в DebugLogger (панель логов)
        DebugLogger.Log(title, message);

        // 3. Вывод в консоль отладки
        Debug.WriteLine($"{title}: {message}{Environment.NewLine}{ex}");

        // 4. Показ Alert пользователю
        if (displayIt)
            await ReportExceptionAsync(ex, title, formatMessage);
    }

    // Исходный ReportExceptionAsync оставляем для обратной совместимости,
    // но теперь он используется только внутри LogAndReportAsync.
    public static async Task ReportExceptionAsync(Exception ex, string title = "Mistake",
        Func<Exception, string>? formatMessage = null)
    {
        var message = formatMessage?.Invoke(ex) ?? ex.Message;

        await DialogService.ShowAlertAsync(title, message);
    }
    public static async Task ReportAsync(string message, string title = "Mistake",
    Func<Exception, string>? formatMessage = null)
    {
        await DialogService.ShowAlertAsync(title, message);
    }

    // TryAsync с автоматическим логированием в случае ошибки
    public static async Task TryAsync(Func<Task> operation, string title = "Mistake")
    {
        try
        {
            await operation();
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await LogAndReportAsync(ex, title);
        }
    }

    public static async Task<T?> TryAsync<T>(Func<Task<T>> operation, string title = "Mistake")
    {
        try
        {
            return await operation();
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await LogAndReportAsync(ex, title);
            return default;
        }
    }
}
