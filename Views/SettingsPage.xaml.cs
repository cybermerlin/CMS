using CMS.Helpers;
using CMS.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;

namespace CMS.Views
{
    [QueryProperty(nameof(AuthorizeAfterSave), "authorize")]
    public partial class SettingsPage : ContentPage
    {
        private const string GitHubDevelopersUrl = "https://github.com/settings/developers";
        private bool _authorizeAfterSave;

        public bool AuthorizeAfterSave
        {
            get => _authorizeAfterSave;
            set => _authorizeAfterSave = value;
        }

        public SettingsPage()
        {
            InitializeComponent();
            ClientIdEntry.Text = Preferences.Get(PreferenceKeys.GitHubClientIdKey, string.Empty);
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            Preferences.Set(PreferenceKeys.GitHubClientIdKey, ClientIdEntry.Text?.Trim() ?? string.Empty);

            if (AuthorizeAfterSave && Shell.Current is AppShell appShell)
            {
                AuthorizeAfterSave = false;
                await ErrorHelper.LogAndReportAsync(new Exception("Client ID сохранён. Запускаем авторизацию..."), "Saved", displayIt: true);
                AppShellViewModel? vm = Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<AppShellViewModel>()
                    ?? throw new InvalidOperationException("AppShellViewModel not registered");
                vm?.AuthorizeCommand.Execute(null);
                return;
            }

            await ErrorHelper.LogAndReportAsync(new Exception("Client ID сохранён. Вернитесь в приложение и повторите подключение."), "Saved", displayIt: true);
        }

        private async void OnGitHubLinkTapped(object? sender, EventArgs e)
        {
            await Browser.Default.OpenAsync(GitHubDevelopersUrl, BrowserLaunchMode.SystemPreferred);
        }

        private void OnInstructionToggleClicked(object? sender, EventArgs e)
        {
            if (InstructionContent == null || InstructionToggleButton == null)
            {
                return;
            }

            InstructionContent.IsVisible = !InstructionContent.IsVisible;
            InstructionToggleButton.Text = InstructionContent.IsVisible
                ? "Инструкция по настройке GitHub ▼"
                : "Инструкция по настройке GitHub ▶";
        }

        private async void OnCopyLinkMenuClicked(object? sender, EventArgs e)
        {
            try
            {
                if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is string text)
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        await Clipboard.Default.SetTextAsync(text);
                        await DisplayAlertAsync("Скопировано", "Ссылка скопирована в буфер обмена.", "Ок");
                    }
                    else
                    {
                        await DisplayAlertAsync("Ошибка", "Не удалось скопировать ссылку.", "Ок");
                    }

                }
            }
            catch
            {
                await DisplayAlertAsync("Ошибка", "Не удалось скопировать ссылку.", "Ок");
            }
        }
    }
}
