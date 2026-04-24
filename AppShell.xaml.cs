using CMS.Helpers;
using CMS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CMS;

public partial class AppShell : Shell
{
    private const string GitHubClientIdKey = "github_client_id";
    private const string GitHubDevelopersUrl = "https://github.com/settings/developers";

    public AppShell()
    {
        InitializeComponent();
    }

    private async void OnAuthorizeClicked(object sender, EventArgs e)
    {
        await StartAuthorizationAsync();
    }

    public async Task StartAuthorizationAsync()
    {
        var clientId = Preferences.Get(GitHubClientIdKey, string.Empty).Trim();
        if (string.IsNullOrEmpty(clientId))
        {
            await Shell.Current.GoToAsync($"{nameof(SettingsPage)}?authorize=true");
            return;
        }

        var gitHubService = Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<IGitHubService>()
            ?? throw new InvalidOperationException("IGitHubService is not available");

        try
        {
            await ErrorHelper.TryAsync(async () =>
            {
                const string scope = "repo,read:org,read:user";

                var deviceAuthorization = await gitHubService.RequestDeviceCodeAsync(clientId, scope);
                if (deviceAuthorization == null)
                {
                    await DisplayAlertAsync(
                        "Ошибка",
                        "Не удалось получить код устройства от GitHub. Проверьте Client ID в настройках и убедитесь, что GitHub OAuth App создан.",
                        "Ок");
                    return;
                }

                var verificationUri = string.IsNullOrEmpty(deviceAuthorization.VerificationUriComplete)
                    ? deviceAuthorization.VerificationUri
                    : deviceAuthorization.VerificationUriComplete;

                await Clipboard.SetTextAsync(deviceAuthorization.UserCode);
                await Browser.Default.OpenAsync(new Uri(verificationUri), BrowserLaunchMode.SystemPreferred);

                await DisplayAlertAsync(
                    "GitHub Device Flow",
                    $"Код скопирован в буфер обмена: {deviceAuthorization.UserCode}\n" +
                    $"Откройте браузер и вставьте код на странице GitHub.",
                    "Ок");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(deviceAuthorization.ExpiresIn));
                var token = await PollDeviceTokenAsync(gitHubService, clientId, deviceAuthorization, cts.Token);
                if (!string.IsNullOrEmpty(token))
                {
                    await SecureStorage.SetAsync("github_token", token);
                    gitHubService.SetToken(token);
                    await DisplayAlertAsync("Успех", "Авторизация прошла успешно!", "Ок");
                }
            }, "Ошибка авторизации");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Ошибка", "Время действия кода истекло. Попробуйте снова.", "Ок");
        }
    }

    private async Task<string?> PollDeviceTokenAsync(IGitHubService gitHubService, string clientId, GitHubDeviceAuthorizationResponse authorization, CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(authorization.Interval, 5));
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);

            var tokenResponse = await gitHubService.ExchangeDeviceCodeForTokenAsync(clientId, authorization.DeviceCode, cancellationToken);
            if (tokenResponse == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return tokenResponse.AccessToken;
            }

            switch (tokenResponse.Error)
            {
                case "authorization_pending":
                    continue;
                case "slow_down":
                    interval += TimeSpan.FromSeconds(5);
                    continue;
                case "access_denied":
                    await DisplayAlertAsync("Отмена", "Пользователь отменил авторизацию.", "Ок");
                    return null;
                case "expired_token":
                    await DisplayAlertAsync("Ошибка", "Срок действия кода истек.", "Ок");
                    return null;
                default:
                    var message = tokenResponse.ErrorDescription ?? "Не удалось получить токен доступа.";
                    await DisplayAlertAsync("Ошибка", message, "Ок");
                    return null;
            }
        }

        return null;
    }
}
