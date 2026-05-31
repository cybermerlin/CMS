using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services;

/// <summary>
/// to show DialogAlert in safemode always
/// </summary>

internal interface IDialogService
{
    void SetMainPage(Page page);
    Task ShowAlertAsync(string title, string message, string accept = "Ок");
}

internal class DialogService : IDialogService
{
    private readonly Queue<AlertRequest> _pendingAlerts = new();
    private Page? _mainPage;
    private bool _isReady;

    public void SetMainPage(Page page)
    {
        _mainPage = page;
        _isReady = true;
        _ = ProcessPendingAsync();
    }

    public async Task ShowAlertAsync(string title, string message, string accept = "Ок")
    {
        if (_isReady && _mainPage?.Window != null)
        {
            await _mainPage.DisplayAlertAsync(title, message, accept);
            return;
        }

        var tcs = new TaskCompletionSource();
        lock (_pendingAlerts)
        {
            _pendingAlerts.Enqueue(new AlertRequest(title, message, accept, tcs));
        }
        await tcs.Task; // ждём, пока диалог будет показан после готовности
    }

    private async Task ProcessPendingAsync()
    {
        while (true)
        {
            AlertRequest? request = null;
            lock (_pendingAlerts)
            {
                if (_pendingAlerts.Count == 0) break;
                request = _pendingAlerts.Dequeue();
            }

            if (request == null) break;

            if (_mainPage?.Window != null)
            {
                await _mainPage.DisplayAlertAsync(request.Title, request.Message, request.Accept);
                request.CompletionSource.TrySetResult();
            }
            else
            {
                request.CompletionSource.TrySetException(new InvalidOperationException("Страница недоступна для диалога"));
            }
        }
    }

    private record AlertRequest(string Title, string Message, string Accept, TaskCompletionSource CompletionSource);
}
