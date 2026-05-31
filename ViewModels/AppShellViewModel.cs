using CMS.Models;
using CMS.Helpers;
using CMS.Services;
using CMS.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CMS.ViewModels;

internal partial class AppShellViewModel : ObservableObject
{
    private readonly IAuthorizationService _authService;
    private string _previousRoute = "//MainPage";


    public AppShellViewModel(IAuthorizationService authService, IDialogService dialogService)
    {
        _authService = authService;

        ErrorHelper.LogAndReportAsync(new Exception("Initialized"), "AppShellViewModel").SafeFireAndForget();

        // to log navigations
        WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (recipient, msg) =>
        {
            var phase = msg.IsNavigating ? "Navigating" : "Navigated";
            ErrorHelper.LogAndReportAsync(
                new Exception($"From: {msg.From ?? "null"} -> To: {msg.To}. [{_previousRoute}]"), phase).SafeFireAndForget();
            _previousRoute = msg.From ?? "//MainPage";
        });
    }


    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync(_previousRoute);
        //await Shell.Current.GoToAsync(".."); // does not work for FlyoutItems navigations
    }

    [RelayCommand]
    private async Task AuthorizeAsync()
    {
        await StartAuthorizationAsync();
    }

    [RelayCommand]
    private async Task OpenLogsAsync()
    {
        await Shell.Current.GoToAsync($"//{nameof(LogPage)}");
    }

    private async Task StartAuthorizationAsync()
    {
        await _authService.StartAuthorizationAsync();
    }
}
