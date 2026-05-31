using CMS.Helpers;
using CMS.Services;
using CMS.ViewModels;
using CMS.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using System;

namespace CMS;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        ErrorHelper.LogAndReportAsync(new Exception("-----------"), "MauiProgram").SafeFireAndForget();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseMauiCommunityToolkit();

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
#endif

        try
        {
            // Pages
            builder.Services.AddTransient<AppShell>();
            //builder.Services.AddTransient<DiscussionsPage>();
            //builder.Services.AddTransient<LogPage>();
            //builder.Services.AddTransient<LogPanel>();

            // ViewModels
            builder.Services.AddTransient<AppShellViewModel>();

            // Services
            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<LoggerService>();
            builder.Services.AddTransient<IAuthorizationService, AuthorizationService>();
            builder.Services.AddTransient<IGitHubService>(sp => new GitHubService());
            
        }
        catch (Exception ex)
        {
            ErrorHelper.LogAndReportAsync(ex, "MauiProgram").SafeFireAndForget();
        }
        ErrorHelper.LogAndReportAsync(new Exception("Started"), "MauiProgram").SafeFireAndForget();


        try
        {
            var app = builder.Build();

            AppDomain.CurrentDomain.UnhandledException += async (sender, args) =>
            {
                Exception? ex = args.ExceptionObject as Exception;
                await ErrorHelper.LogAndReportAsync(ex ?? new Exception("Unknown"), "MauiProgram");
            };

            return app;
        }
        catch (Exception ex)
        {
            ErrorHelper.LogAndReportAsync(ex, "MauiProgram").SafeFireAndForget();
            throw;
        }
    }
}
