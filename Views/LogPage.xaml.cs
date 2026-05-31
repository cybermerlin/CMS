using CMS.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;


namespace CMS.Views;

internal partial class LogPage : ContentPage
{
	public LogPage()
	{
		InitializeComponent();
        LogsCollection.ItemsSource = DebugLogger.Logs;
    }
}
