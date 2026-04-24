using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;

namespace CMS
{
    [QueryProperty(nameof(AuthorizeAfterSave), "authorize")]
    public partial class SettingsPage : ContentPage
    {
        private const string GitHubClientIdKey = "github_client_id";
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
            ClientIdEntry.Text = Preferences.Get(GitHubClientIdKey, string.Empty);
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            Preferences.Set(GitHubClientIdKey, ClientIdEntry.Text?.Trim() ?? string.Empty);

            if (AuthorizeAfterSave && Shell.Current is AppShell appShell)
            {
                AuthorizeAfterSave = false;
                await DisplayAlertAsync("Сохранено", "Client ID сохранён. Запускаем авторизацию...", "Ок");
                await appShell.StartAuthorizationAsync();
                return;
            }

            await DisplayAlertAsync("Сохранено", "Client ID сохранён. Вернитесь в приложение и повторите подключение.", "Ок");
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
                await Clipboard.Default.SetTextAsync(GitHubDevelopersUrl);
                await DisplayAlertAsync("Скопировано", "Ссылка скопирована в буфер обмена.", "Ок");
            }
            catch
            {
                await DisplayAlertAsync("Ошибка", "Не удалось скопировать ссылку.", "Ок");
            }
        }
    }
}
