using CMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Helpers;

internal static class TaskExtensions
{
    public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            onException?.Invoke(ex);
            await ErrorHelper.LogAndReportAsync(ex, "SafeFireAndForget exception");
        }
    }
}
