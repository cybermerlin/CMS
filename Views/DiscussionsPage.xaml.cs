using CMS.Helpers;
using CMS.Models;
using CMS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;


namespace CMS.Views;

internal partial class DiscussionsPage : ContentPage
{
    private IGitHubService? _gitHubService;
    private string _owner = String.Empty;
    private string _repo = String.Empty;
    private GitHubRateLimits? _limits;
    // Свойства для привязки
    public ObservableCollection<Models.DiscussionItem> Discussions { get; } = new();
    /// <summary>
    /// Таймер для периодической проверки новых обсуждений
    /// </summary>
    private IDispatcherTimer? _timer;
    private DateTime _lastChecked = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public string PageTitle => $"Последние обсуждения {_repo}";

    public string? Owner
    {
        get => _owner;
        set
        {
            if (_owner != value && value != null)
            {
                _owner = value;
                _lastChecked = DateTime.UtcNow;
                Discussions.Clear();
                OnPropertyChanged(nameof(PageTitle));
                _ = CheckForUpdates();
            }
        }
    }

    public string? Repo
    {
        get => _repo;
        set
        {
            if (_repo != value && value != null)
            {
                _repo = value;
                _lastChecked = DateTime.UtcNow;
                Discussions.Clear();
                OnPropertyChanged(nameof(PageTitle));
                _ = CheckForUpdates();
            }
        }
    }

    public DiscussionsPage(IGitHubService gitHubService)
    {
        InitializeComponent();
        _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
        _owner = "cybermerlin";
        _repo = "CMS";
        BindingContext = this;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(600000);
        _timer.Tick += async (s, e) => await CheckForUpdates();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var token = await SecureStorage.GetAsync("github_token");
            if (!string.IsNullOrEmpty(token))
            {
                _gitHubService?.SetToken(token);
            }
        }
        catch(Exception ex)
        {
            await ErrorHelper.LogAndReportAsync(ex, "Token Error");
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

    private async Task GetGithubStatuses()
    {
        if (_gitHubService == null) return;

        _limits = await _gitHubService.GetRateLimitsAsync();
        if (_limits != null)
        {
            LimitsPanel.BindingContext = _limits;
        }
    }

    private async Task<List<DiscussionItem>> GetDiscussionsFromAtom()
    {
        List<DiscussionItem> latestDiscussions = new();

        await ErrorHelper.LogAndReportAsync(new Exception("start"), "DiscussionsPage.GetDiscussionsFromAtom");
        if (_gitHubService == null) return latestDiscussions;

        try
        {
            var feed = await _gitHubService.GetDiscussionsAtomAsync(_owner, _repo);
            if (feed != null)
            {
                List<DiscussionItem> newDiscussions = feed.Entries
                    .Where(e => e.Published > _lastChecked)
                    .Select(e => e.ToDiscussionItem())
                    .ToList();

                // добавить в коллекцию
                if (newDiscussions.Count > 0)
                {
                    latestDiscussions.AddRange(newDiscussions);
                    await ErrorHelper.ReportAsync($"Count: {newDiscussions.Count}", "News in Discussions");
                }
            }
        }
        catch (Exception ex)
        {
            await ErrorHelper.LogAndReportAsync(ex, "Discussions Error");
        }

        return latestDiscussions;
    }

    private async Task CheckForUpdates()
    {
        await ErrorHelper.LogAndReportAsync(new Exception("start"), "DiscussionsPage.CheckForUpdates");
        
        try
        {
            await GetGithubStatuses();

            List<Models.DiscussionItem> latestDiscussions = await GetDiscussions();
            await ErrorHelper.ReportAsync($"Count: {latestDiscussions.Count}", "in latestDiscussions");

            if (latestDiscussions.Count == 0) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var item in latestDiscussions.OrderBy(ld => ld.CreatedAt).ToList())
                    Discussions.Insert(0, item);
                _lastChecked = DateTime.UtcNow;
            });

            await ErrorHelper.LogAndReportAsync(
                new Exception($"dis: {string.Join("\n", Discussions.Select((d, i) => $"{i + 1}. {d.Title} {d.BodyPreview} ({d.CreatedAt:g})"))}"), "in CheckForUpdates");
        }
        catch (Exception ex)
        {
            await ErrorHelper.LogAndReportAsync(ex, "Discussions Error");
        }
    }

    private async Task<List<Models.DiscussionItem>> GetDiscussions()
    {
        if (_gitHubService == null) return new();

        if (_limits == null) return await GetDiscussionsFromAtom();
        if (_limits.GraphQLRemaining > 0)
            return (await _gitHubService.GetDiscussionsViaGQLAsync(_owner, _repo)).ToList();
        if (_limits.CoreRemaining > 50){
            List<Models.DiscussionItem> res = (await _gitHubService.GetDiscussionsViaEventsAsync(_owner, _repo)).ToList();
            if (res.Count == 0)
                return await GetDiscussionsFromAtom();
            else
                return res;
        }

        return await GetDiscussionsFromAtom();
    }
}
