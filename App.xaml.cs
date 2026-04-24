#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Graphics;
using Microsoft.UI;
#endif

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;

namespace CMS;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

#if WINDOWS
		void ConfigureAppWindow()
		{
			if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
			{
				return;
			}

			var hwnd = WindowNative.GetWindowHandle(nativeWindow);
			var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
			var appWindow = AppWindow.GetFromWindowId(windowId);
			if (appWindow == null)
			{
				return;
			}

			var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
			var maxWidth = Math.Min(800, displayArea.WorkArea.Width * 40 / 100);
			var maxHeight = Math.Min(600, displayArea.WorkArea.Height * 40 / 100);
			var desiredSize = new SizeInt32 { Width = maxWidth, Height = maxHeight };
			appWindow.Resize(desiredSize);

			var x = displayArea.WorkArea.X + (displayArea.WorkArea.Width - desiredSize.Width) / 2;
			var y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - desiredSize.Height) / 2;
			appWindow.Move(new PointInt32(x, y));
		}

		if (window.Handler != null)
		{
			ConfigureAppWindow();
		}
		else
		{
			window.HandlerChanged += (sender, args) => ConfigureAppWindow();
		}
#endif

        return window;
    }
}
