using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Helpers
{
    public static class ErrorHelper
    {
        public static async Task ReportExceptionAsync(Exception ex, string title = "Ошибка", Func<Exception, string>? formatMessage = null)
        {
            Debug.WriteLine(ex);
            var message = formatMessage?.Invoke(ex) ?? ex.Message;

            Page? currentPage = Shell.Current ?? Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (currentPage != null)
            {
                try
                {
                    await Clipboard.Default.SetTextAsync(ex.Message);
                }
                catch
                {
                    // ignore clipboard failures
                }

                await currentPage.DisplayAlertAsync(title, message, "Ок");
            }
            else
            {
                Console.Error.WriteLine($"{title}: {message}");
            }
        }

        public static async Task TryAsync(Func<Task> operation, string title = "Ошибка", Func<Exception, string>? formatMessage = null)
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
                await ReportExceptionAsync(ex, title, formatMessage);
            }
        }

        public static async Task<T?> TryAsync<T>(Func<Task<T>> operation, string title = "Ошибка", Func<Exception, string>? formatMessage = null)
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
                await ReportExceptionAsync(ex, title, formatMessage);
                return default;
            }
        }
    }
}
