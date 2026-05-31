using CMS.Helpers;
using CMS.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CMS.Services;

internal interface IAuthorizationService
{
    /// <summary>Запросить код устройства (Device Flow).</summary>
    Task<DeviceCodeResponse?> RequestDeviceCodeAsync(string clientId, string scope);

    /// <summary>Обменять код устройства на токен доступа.</summary>
    Task<TokenResponse?> ExchangeDeviceCodeForTokenAsync(string clientId, string deviceCode,
        CancellationToken cancellationToken);

    /// <summary>Сохранить токен в защищённом хранилище.</summary>
    Task SaveTokenAsync(string token);

    /// <summary>Получить сохранённый токен.</summary>
    Task<string?> GetTokenAsync();

    Task StartAuthorizationAsync();
}

internal class DeviceCodeResponse
{
    public string DeviceCode { get; init; } = string.Empty;
    public string UserCode { get; init; } = string.Empty;
    public string VerificationUri { get; init; } = string.Empty;
    public string? VerificationUriComplete { get; init; }
    public int ExpiresIn { get; init; }
    public int Interval { get; init; }
}

internal class TokenResponse
{
    public string? AccessToken { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// try AutoMapper w  mapper.Map&lt;TokenResponse>(response)
    /// </summary>
    /// <param name="source"></param>
    public static implicit operator TokenResponse?(GitHubDeviceTokenResponse? source)
    {
        if (source == null) return null;
        return new TokenResponse
        {
            AccessToken = source.AccessToken,
            Error = source.Error,
            ErrorDescription = source.ErrorDescription
        };
    }
}

internal class AuthorizationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AccessToken { get; set; }
}

internal class AuthorizationService : IAuthorizationService
{
    private readonly IGitHubService _gitHubService;


    public AuthorizationService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }


    public async Task<DeviceCodeResponse?> RequestDeviceCodeAsync(string clientId, string scope)
    {
        var response = await _gitHubService.RequestDeviceCodeAsync(clientId, scope);
        if (response == null) return null;

        return new DeviceCodeResponse
        {
            DeviceCode = response.DeviceCode,
            UserCode = response.UserCode,
            VerificationUri = response.VerificationUri,
            VerificationUriComplete = response.VerificationUriComplete,
            ExpiresIn = response.ExpiresIn,
            Interval = response.Interval
        };
    }

    public async Task<TokenResponse?> ExchangeDeviceCodeForTokenAsync(
        string clientId, string deviceCode, CancellationToken ct)
    {
        return await _gitHubService.ExchangeDeviceCodeForTokenAsync(clientId, deviceCode, ct);
    }

    public async Task SaveTokenAsync(string token)
    {
        await SecureStorage.SetAsync("github_token", token);
        _gitHubService.SetToken(token);
    }

    public Task<string?> GetTokenAsync() => SecureStorage.GetAsync("github_token");


    private async void SendAlert(string title, string message)
    {
        await ErrorHelper.LogAndReportAsync(new Exception(message), title, displayIt: true);
    }

    public async Task StartAuthorizationAsync()
    {
        var clientId = Preferences.Get(PreferenceKeys.GitHubClientIdKey, string.Empty).Trim();
        if (string.IsNullOrEmpty(clientId))
        {
            SendAlert("Настройка", "Client ID не задан. Перейдите в настройки.");
            await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}?authorize=true");
            return;
        }

        try
        {
            await ErrorHelper.TryAsync(async () =>
            {
                const string scope = "repo,read:org,read:user";

                var deviceAuthorization = await RequestDeviceCodeAsync(clientId, scope);
                if (deviceAuthorization == null)
                {
                    string msg = "Не удалось получить код устройства от GitHub. Проверьте Client ID в настройках и убедитесь, что GitHub OAuth App создан.";
                    SendAlert("Ошибка", msg);

                    await ErrorHelper.LogAndReportAsync(new Exception(msg), "AppShellViewModel");
                    return;
                }

                var verificationUri = string.IsNullOrEmpty(deviceAuthorization.VerificationUriComplete)
                    ? deviceAuthorization.VerificationUri
                    : deviceAuthorization.VerificationUriComplete;

                await Clipboard.SetTextAsync(deviceAuthorization.UserCode);
                await Browser.Default.OpenAsync(new Uri(verificationUri), BrowserLaunchMode.SystemPreferred);

                SendAlert("GitHub Device Flow",
                    $"Код скопирован: {deviceAuthorization.UserCode}\nОткройте {verificationUri.ToString()} и вставьте код.");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(deviceAuthorization.ExpiresIn));
                var tokenResponse = await PollDeviceTokenAsync(clientId, deviceAuthorization.DeviceCode, deviceAuthorization.Interval, cts.Token);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    SendAlert("Ошибка", tokenResponse?.ErrorDescription ?? "Не удалось получить токен доступа.");
                }
                else
                {
                    await SaveTokenAsync(tokenResponse.AccessToken);
                    SendAlert("Успех", "Авторизация прошла успешно!");
                }
            }, "Ошибка авторизации");
        }
        catch (TaskCanceledException)
        {
            await ErrorHelper.LogAndReportAsync(new Exception("Время действия кода истекло. Попробуйте снова."), "AppShell Error");
        }
    }

    private async Task<Services.TokenResponse?> PollDeviceTokenAsync(string clientId, string deviceCode, int intervalSeconds,
        CancellationToken cancellationToken)

    {
        var interval = TimeSpan.FromSeconds(Math.Max(intervalSeconds, 5));
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);

            Services.TokenResponse? tokenResponse = await ExchangeDeviceCodeForTokenAsync(clientId, deviceCode, cancellationToken);
            if (tokenResponse == null)
            {
                await ErrorHelper.LogAndReportAsync(new Exception("Token response was null"), "Token Error");
                return null;
            }

            if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return tokenResponse;
            }

            switch (tokenResponse.Error)
            {
                case "authorization_pending":
                    continue;
                case "slow_down":
                    await ErrorHelper.LogAndReportAsync(new Exception("Slow down"), "Token Error");
                    interval += TimeSpan.FromSeconds(5);
                    continue;
                case "access_denied":
                    await ErrorHelper.LogAndReportAsync(new Exception("Пользователь отменил авторизацию."), "Token Error");
                    return null;
                case "expired_token":
                    await ErrorHelper.LogAndReportAsync(new Exception("Срок действия кода истек."), "Token Error");
                    return null;
                default:
                    var message = tokenResponse.ErrorDescription ?? "Не удалось получить токен доступа.";
                    await ErrorHelper.LogAndReportAsync(new Exception(message), "Token Error");
                    return null;
            }
        }

        return null;
    }
}
