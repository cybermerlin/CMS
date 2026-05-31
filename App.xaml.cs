#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Graphics;
using Microsoft.UI;
using System.Runtime.InteropServices;   // для DllImport
#endif

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using CMS.Views;
using Microsoft.Maui.Graphics;
using CMS.Helpers;
using Microsoft.Extensions.DependencyInjection;
using CMS.Services;
using CMS.ViewModels;

namespace CMS;

internal partial class App : Application
{
    private readonly AppShell _shell;
    public App(AppShell shell){
        InitializeComponent();
        _shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window? window = null;
        try
        {
            //window = new Window(new AppShell());
            window = new Window(_shell);
            //test 1
            //window = new Window(new ContentPage { Content = new Label { Text = "Hello" } });  // for test
            //test 2
            //var gitHubService = new GitHubService(); // если у него нет зависимостей
            //var authService = new AuthorizationService(gitHubService); // если реализован
            //var dialog = new DialogService();
            //var vm = new AppShellViewModel(authService, dialog);
            //var shell = new AppShell(vm, dialog);
            //window = new Window(shell);
        }
        catch (Exception ex)
        {
            ErrorHelper.LogAndReportAsync(ex, "App").SafeFireAndForget();
            throw;
        }
        ErrorHelper.LogAndReportAsync(new Exception("Started"), "App").SafeFireAndForget();

#if WINDOWS
        void ConfigureAppWindow()
        {
            if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
                return;

            var hwnd = WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow == null) return;

            // --- Определяем монитор, где сейчас мышь ---
            DisplayArea targetDisplay;
            if (GetCursorPos(out var pt))
            {
                // Преобразуем в PointInt32, понятный WinUI
                PointInt32 point = new PointInt32(pt.X, pt.Y);
                targetDisplay = DisplayArea.GetFromPoint(point, DisplayAreaFallback.Nearest);
            }
            else
            {
                // Если не удалось получить позицию, берём основной
                targetDisplay = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            }
            // -------------------------------------------

            RectInt32 workArea = targetDisplay.WorkArea;
            int maxWidth = Math.Min(1100, workArea.Width * 40 / 100);
            int maxHeight = Math.Min(600, workArea.Height * 40 / 100);
            SizeInt32 desiredSize = new SizeInt32 { Width = maxWidth, Height = maxHeight };
            appWindow.Resize(desiredSize);

            // Центрируем относительно рабочей области ВЫБРАННОГО монитора
            int x = workArea.X + (workArea.Width - desiredSize.Width) / 2;
            int y = workArea.Y + (workArea.Height - desiredSize.Height) / 2;
            appWindow.Move(new PointInt32(x, y));
        }

        if (window.Handler != null)
            ConfigureAppWindow();
        else
            window.HandlerChanged += (sender, args) => ConfigureAppWindow();
#endif

        return window;
    }

#if WINDOWS
    // Win32-функция для получения позиции курсора
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    private struct POINT
    {
        public int X;
        public int Y;
    }
#endif
}
