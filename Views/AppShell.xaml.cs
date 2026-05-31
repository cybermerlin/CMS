using CMS;
using CMS.Models;
using CMS.Helpers;
using CMS.Services;
using CMS.ViewModels;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CMS.Views;

internal partial class AppShell : Shell
{
    private readonly IDialogService _dialogService;

    public AppShell(AppShellViewModel viewModel, IDialogService dialogService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _dialogService = dialogService;

        Loaded += (s, e) =>
        {
            _dialogService.SetMainPage(this);
        };

        Navigated += (s, e) =>
        {
            WeakReferenceMessenger.Default.Send(new NavigationMessage
            {
                From = e.Previous?.Location?.OriginalString,
                To = e.Current?.Location?.OriginalString,
                IsNavigating = false
            });
        };
        Navigating += (s, e) =>
        {
            WeakReferenceMessenger.Default.Send(new NavigationMessage
            {
                To = e.Target?.Location?.OriginalString,
                IsNavigating = true
            });
        };

        // sub on msgs from the ViewModel
        //WeakReferenceMessenger.Default.Register<ShowAlertMessage>(this, async (recipient, message) =>
        //{
        //    if ((Shell.Current ?? Application.Current?.Windows?.FirstOrDefault()?.Page)?.Window != null)
        //        await DisplayAlertAsync(message.Title, message.Message, message.Accept);
        //});

        //Routing.RegisterRoute("discussion", typeof(DiscussionDetailPage));
    }

}
