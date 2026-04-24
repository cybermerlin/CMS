using CMS.Helpers;
using CMS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CMS
{
    internal class DiscussionsPage : ContentPage
    {
        private IGitHubService? _gitHubService;
        private string? _owner;
        private string? _repo;
        private readonly ObservableCollection<DiscussionItem> _discussions = new();
        /// <summary>
        /// Таймер для периодической проверки новых обсуждений
        /// </summary>
        private IDispatcherTimer? _timer;
        private DateTime _lastChecked = DateTime.UtcNow;
        private const string GitHubClientIdKey = "github_client_id";

        public string? Owner
        {
            get => _owner;
            set
            {
                if (_owner != value)
                {
                    _owner = value;
                    _lastChecked = DateTime.UtcNow; // Сбросить время последней проверки
                    _discussions.Clear(); // Очистить список обсуждений
                    // Перезапустить проверку, если нужно
                    _ = CheckForUpdates();
                }
            }
        }

        public string? Repo
        {
            get => _repo;
            set
            {
                if (_repo != value)
                {
                    _repo = value;
                    _lastChecked = DateTime.UtcNow; // Сбросить время последней проверки
                    _discussions.Clear(); // Очистить список обсуждений
                    // Перезапустить проверку, если нужно
                    _ = CheckForUpdates();
                }
            }
        }

        public DiscussionsPage()
        {
            _InitializePage();
        }

        public DiscussionsPage(IGitHubService gitHubService)
        {
            _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
            _InitializePage();
        }

        private void _InitializePage()
        {

            _owner = "cybermerlin"; // Default owner
            _repo = "CMS"; // Default repo

            Title = "Discussions";

            var titleHeader = new Label
            {
                Text = "Последние обсуждения",
                FontAttributes = FontAttributes.Bold,
                FontSize = 24
            };

            var discussionsList = new CollectionView
            {
                ItemsSource = _discussions,
                ItemTemplate = new DataTemplate(() =>
                {
                    var titleLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 18 };
                    titleLabel.SetBinding(Label.TextProperty, nameof(DiscussionItem.Title));

                    var authorLabel = new Label { TextColor = Colors.Gray, FontSize = 14 };
                    authorLabel.SetBinding(Label.TextProperty, nameof(DiscussionItem.Author));

                    var bodyLabel = new Label { MaxLines = 2, FontSize = 14 };
                    bodyLabel.SetBinding(Label.TextProperty, nameof(DiscussionItem.BodyPreview));

                    var createdAtLabel = new Label { TextColor = Colors.Gray, FontSize = 12 };
                    createdAtLabel.SetBinding(Label.TextProperty, nameof(DiscussionItem.CreatedAtText));

                    return new VerticalStackLayout
                    {
                        Padding = 10,
                        Spacing = 4,
                        Children = { titleLabel, authorLabel, bodyLabel, createdAtLabel }
                    };
                })
            };

            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(20, 0),
                    Spacing = 20,
                    Children = { titleHeader, discussionsList }
                }
            };

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(60);
            _timer.Tick += async (s, e) => await CheckForUpdates();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_gitHubService == null)
            {
                _gitHubService = Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<IGitHubService>()
                    ?? throw new InvalidOperationException("IGitHubService is not available");
            }

            try
            {
                // Load token from storage
                var token = await SecureStorage.GetAsync("github_token");
                if (!string.IsNullOrEmpty(token))
                {
                    _gitHubService.SetToken(token);
                }
            }
            catch
            {
                // Token loading failed, continue without authentication
            }

            if (_timer is { IsRunning: false })
            {
                _timer.Start();
            }

            await CheckForUpdates();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_timer?.IsRunning == true)
            {
                _timer.Stop();
            }
        }

        private async Task CheckForUpdates()
        {
            if (_gitHubService == null) return;

            try
            {
                if (string.IsNullOrWhiteSpace(_owner) || string.IsNullOrWhiteSpace(_repo))
                {
                    return;
                }

                var json = await _gitHubService.GetDiscussionsAsync(_owner, _repo);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var latestDiscussions = ParseDiscussions(json)
                    .Where(d => d.CreatedAt > _lastChecked)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToList();

                if (!latestDiscussions.Any())
                {
                    return;
                }

                foreach (var item in latestDiscussions)
                {
                    _discussions.Insert(0, item);
                }

                _lastChecked = DateTime.UtcNow;
                await DisplayAlertAsync("Новое в Discussions", "Появились свежие темы!", "Ок");
            }
            catch
            {
                // Ошибки синхронизации игнорируем, чтобы не ломать навигацию
            }
        }

        private static IEnumerable<DiscussionItem> ParseDiscussions(string json)
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("data", out var dataElement) ||
                !dataElement.TryGetProperty("repository", out var repositoryElement) ||
                !repositoryElement.TryGetProperty("discussions", out var discussionsElement) ||
                !discussionsElement.TryGetProperty("nodes", out var nodesElement))
            {
                return Array.Empty<DiscussionItem>();
            }

            var result = new List<DiscussionItem>();
            foreach (var node in nodesElement.EnumerateArray())
            {
                var title = node.GetProperty("title").GetString() ?? "Без заголовка";
                var url = node.GetProperty("url").GetString() ?? string.Empty;
                var createdAt = node.GetProperty("createdAt").GetDateTime();

                result.Add(new DiscussionItem(
                    title,
                    "Автор неизвестен",
                    string.Empty,
                    createdAt,
                    url));
            }

            return result;
        }

        /// <summary>
        /// Authenticates the user with GitHub via Device Flow.
        /// </summary>
        private async Task AuthenticateAsync()
        {
            if (_gitHubService == null) return;

            try
            {
                await ErrorHelper.TryAsync(async () =>
                {
                    var clientId = Preferences.Get(GitHubClientIdKey, string.Empty).Trim();
                    const string scope = "repo,read:org,read:user";

                    if (string.IsNullOrEmpty(clientId))
                    {
                        await DisplayAlertAsync(
                            "Требуется Client ID",
                            "Укажите Client ID GitHub OAuth App в настройках приложения.",
                            "Ок");
                        await Shell.Current.GoToAsync(nameof(SettingsPage));
                        return;
                    }

                    var deviceAuthorization = await _gitHubService.RequestDeviceCodeAsync(clientId, scope);
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
                    await Browser.OpenAsync(new Uri(verificationUri), BrowserLaunchMode.SystemPreferred);

                    await DisplayAlertAsync(
                        "GitHub Device Flow",
                        $"Код скопирован в буфер обмена: {deviceAuthorization.UserCode}\n" +
                        $"Откройте браузер и вставьте код на странице GitHub.",
                        "Ок");

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(deviceAuthorization.ExpiresIn));
                    var token = await PollDeviceTokenAsync(clientId, deviceAuthorization, cts.Token);
                    if (!string.IsNullOrEmpty(token))
                    {
                        await SecureStorage.SetAsync("github_token", token);
                        _gitHubService.SetToken(token);
                        await DisplayAlertAsync("Успех", "Авторизация прошла успешно!", "Ок");
                        await CheckForUpdates();
                    }
                }, "Ошибка авторизации");
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync("Ошибка", "Время действия кода истекло. Попробуйте снова.", "Ок");
            }
        }

        private async Task<string?> PollDeviceTokenAsync(string clientId, GitHubDeviceAuthorizationResponse authorization, CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(authorization.Interval, 5));
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(interval, cancellationToken);

                var tokenResponse = await _gitHubService!.ExchangeDeviceCodeForTokenAsync(clientId, authorization.DeviceCode, cancellationToken);
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

        private sealed record DiscussionItem(string Title, string Author, string BodyPreview, DateTime CreatedAt, string Url)
        {
            public string CreatedAtText => $"Создано: {CreatedAt.ToLocalTime():g}";
        }
    }
}
