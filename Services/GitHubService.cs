using CMS.Helpers;
using CMS.Models;
using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CMS.Services;

public sealed class GitHubDeviceAuthorizationResponse
{
    public string DeviceCode { get; init; } = string.Empty;
    public string UserCode { get; init; } = string.Empty;
    public string VerificationUri { get; init; } = string.Empty;
    public string VerificationUriComplete { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public int Interval { get; init; }
}

public sealed class GitHubDeviceTokenResponse
{
    public string? AccessToken { get; init; }
    public string? TokenType { get; init; }
    public string? Scope { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
    public string? ErrorUri { get; init; }
}

internal interface IGitHubService
{
    Task<GitHubRateLimits?> GetRateLimitsAsync();
    Task<GitHubDeviceAuthorizationResponse?> RequestDeviceCodeAsync(string clientId, string scope);
    Task<GitHubDeviceTokenResponse?> ExchangeDeviceCodeForTokenAsync(string clientId, string deviceCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscussionItem>> GetDiscussionsViaEventsAsync(string owner, string repo);
    Task<IReadOnlyList<DiscussionItem>> GetDiscussionsViaGQLAsync(string owner, string repo);
    Task<DiscussionAtomFeed?> GetDiscussionsAtomAsync(string owner, string repo);

    void SetToken(string? token);
}

internal class GitHubService : IGitHubService
{

    private readonly GitHubClient _client;
    private DateTime _lastChecked = DateTime.UtcNow;

    public GitHubService(string? token = null)
    {
        var projectName = Assembly.GetExecutingAssembly().GetName().Name;

        _client = new GitHubClient(new Octokit.ProductHeaderValue(projectName));

        if (!string.IsNullOrEmpty(token))
        {
            _client.Credentials = new Octokit.Credentials(token);
        }
    }

    public void SetToken(string? token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            _client.Credentials = new Octokit.Credentials(token);
        }
        else
        {
            _client.Credentials = null;
        }
    }

    public async Task<DiscussionAtomFeed?> GetDiscussionsAtomAsync(string owner, string repo)
    {
        await ErrorHelper.LogAndReportAsync(new Exception("start"), "GitHubService.GetDiscussionsAtomAsync");
        try
        {
            var url = $"https://github.com/{owner}/{repo}/discussions.atom";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CMS");
            // Atom-фид не требует авторизации для публичных репозиториев
            string? response = await client.GetStringAsync(url);
            await ErrorHelper.LogAndReportAsync(new Exception($"Response: {response}"), "GitHubService.GetDiscussionsAtomAsync");

            var doc = XDocument.Parse(response);
            XNamespace ns = "http://www.w3.org/2005/Atom";
            var feedElement = doc.Root;
            if (feedElement == null) return null;

            var feed = new DiscussionAtomFeed
            {
                Title = feedElement.Element(ns + "title")?.Value ?? string.Empty,
                Updated = DateTimeOffset.TryParse(feedElement.Element(ns + "updated")?.Value, out var updated) ? updated : DateTimeOffset.MinValue
            };

            foreach (var entry in feedElement.Elements(ns + "entry"))
            {
                var item = new DiscussionAtomEntry
                {
                    Id = entry.Element(ns + "id")?.Value ?? string.Empty,
                    Title = entry.Element(ns + "title")?.Value?.Trim() ?? string.Empty,
                    Link = entry.Element(ns + "link")?.Attribute("href")?.Value ?? string.Empty,
                    Published = DateTimeOffset.TryParse(entry.Element(ns + "published")?.Value, out var pub) ? pub : DateTimeOffset.MinValue,
                    Updated = DateTimeOffset.TryParse(entry.Element(ns + "updated")?.Value, out var upd) ? upd : DateTimeOffset.MinValue,
                    AuthorName = entry.Element(ns + "author")?.Element(ns + "name")?.Value ?? string.Empty,
                    AuthorUri = entry.Element(ns + "author")?.Element(ns + "uri")?.Value ?? string.Empty,
                    ContentHtml = entry.Element(ns + "content")?.Value ?? string.Empty,
                    ThumbnailUrl = entry.Element(ns + "thumbnail")?.Attribute("url")?.Value ?? string.Empty
                };
                feed.Entries.Add(item);
            }

            return feed;
        }
        catch (Exception ex)
        {
            DebugLogger.Log("AtomFeed Error", ex.Message);
            return null;
        }
    }

    public async Task<IReadOnlyList<DiscussionItem>> GetDiscussionsViaEventsAsync(string owner, string repo)
    {
        await ErrorHelper.LogAndReportAsync(new Exception("start"), "GitHubService.GetDiscussionsViaEventsAsync");
        try
        {
            // Получаем последние 50 событий (максимум на страницу)
            IReadOnlyList<Activity> events = await _client.Activity.Events.GetAllForRepository(owner, repo,
                new ApiOptions { PageSize = 50, PageCount = 1 });

            await ErrorHelper.LogAndReportAsync(
                new Exception($"Events: {string.Join(", ", events.Select(e => e.Type).ToList())}"),
                "GitHubService.GetDiscussionsViaEventsAsync");

            List<DiscussionItem> discussionItems = new List<DiscussionItem>();

            foreach (Activity evt in events)
            {
                if (evt.Type != "DiscussionEvent") continue;

                // Octokit не всегда десериализует Payload в DiscussionEventPayload,
                // поэтому читаем через динамический JObject или raw Json.
                // Простой способ: используем Newtonsoft.Json (или System.Text.Json) для извлечения полей.
                JObject payload = JObject.FromObject(evt.Payload);
                if (payload == null) continue;

                // !!! payload не содержит payload["discussion"],
                //     но позволяет увидеть факт появления Дискуссиии и Отправителя и репы
                // TODO: пересмотреть ниже код за ненадобностью
                var discussion = payload["discussion"] as JObject;
                if (discussion == null) continue;

                var title = discussion["title"]?.Value<string>() ?? "Без названия";
                var url = discussion["html_url"]?.Value<string>() ?? "";
                var createdAt = discussion["created_at"]?.Value<DateTime>() ?? DateTime.MinValue;
                var author = discussion["user"]?["login"]?.Value<string>() ?? "Неизвестный";
                var bodyPreview = (discussion["body"]?.Value<string>() ?? "").Truncate(100); // обрежем

                discussionItems.Add(new DiscussionItem(
                    title,
                    author,
                    bodyPreview,
                    createdAt,
                    url,
                    categoryId: "",
                    answered: false));
            }

            return discussionItems.OrderByDescending(d => d.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            DebugLogger.Log("REST Discussions Error", ex.Message);
            return Array.Empty<DiscussionItem>();
        }
    }

    public async Task<GitHubRateLimits?> GetRateLimitsAsync()
    {
        await ErrorHelper.LogAndReportAsync(new Exception("start"), "GitHubService.GetRateLimitsAsync");
        try
        {
            MiscellaneousRateLimit rateLimits = await _client.RateLimit.GetRateLimits();
            GitHubRateLimits result = new GitHubRateLimits
            {
                CoreLimit = rateLimits.Resources.Core.Limit,
                CoreRemaining = rateLimits.Resources.Core.Remaining,
                CoreReset = rateLimits.Resources.Core.Reset,

                SearchLimit = rateLimits.Resources.Search.Limit,
                SearchRemaining = rateLimits.Resources.Search.Remaining,
                SearchReset = rateLimits.Resources.Search.Reset,

                GraphQLLimit = rateLimits.Resources.Graphql.Limit,
                GraphQLRemaining = rateLimits.Resources.Graphql.Remaining,
                GraphQLReset = rateLimits.Resources.Graphql.Reset
            };
            DebugLogger.Log("github.GetRateLimits", result.ToString());
            return result;
        }
        catch (Exception ex)
        {
            await ErrorHelper.LogAndReportAsync(ex, "GetRateLimits error");
            return null;
        }
    }

    public async Task<GitHubDeviceAuthorizationResponse?> RequestDeviceCodeAsync(string clientId, string scope)
    {
        using var client = new HttpClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("scope", scope)
        });

        var response = await client.PostAsync("https://github.com/login/device/code", content);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync();
        var values = ParseUrlEncoded(responseString);
        if (!values.TryGetValue("device_code", out var deviceCode) ||
            !values.TryGetValue("user_code", out var userCode) ||
            !values.TryGetValue("verification_uri", out var verificationUri) ||
            !values.TryGetValue("expires_in", out var expiresInText) ||
            !values.TryGetValue("interval", out var intervalText))
        {
            return null;
        }

        return new GitHubDeviceAuthorizationResponse
        {
            DeviceCode = deviceCode,
            UserCode = userCode,
            VerificationUri = verificationUri,
            VerificationUriComplete = values.GetValueOrDefault("verification_uri_complete", string.Empty),
            ExpiresIn = int.TryParse(expiresInText, out var expiresIn) ? expiresIn : 900,
            Interval = int.TryParse(intervalText, out var interval) ? interval : 5
        };
    }

    public async Task<GitHubDeviceTokenResponse?> ExchangeDeviceCodeForTokenAsync(string clientId, string deviceCode, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("device_code", deviceCode),
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code")
        });

        var response = await client.PostAsync("https://github.com/login/oauth/access_token", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var values = ParseUrlEncoded(responseString);

        return new GitHubDeviceTokenResponse
        {
            AccessToken = values.GetValueOrDefault("access_token"),
            TokenType = values.GetValueOrDefault("token_type"),
            Scope = values.GetValueOrDefault("scope"),
            Error = values.GetValueOrDefault("error"),
            ErrorDescription = values.GetValueOrDefault("error_description"),
            ErrorUri = values.GetValueOrDefault("error_uri")
        };
    }

    private static Dictionary<string, string> ParseUrlEncoded(string content)
    {
        return content.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => WebUtility.UrlDecode(parts[0]), parts => WebUtility.UrlDecode(parts[1]));
    }

//        discussionCategories(
//  after: String,
//  before: String,
//  first: Int,
//  last: Int,
//) : DiscussionCategoryConnection!

    public async Task<IReadOnlyList<DiscussionItem>> GetDiscussionsViaGQLAsync(string owner, string repo)
    {
        await ErrorHelper.LogAndReportAsync(new Exception("start"), "GitHubService.GetDiscussionsViaGQLAsync");
        List<DiscussionItem> discussionItems = new();

        //await ErrorHelper.ReportExceptionAsync(new Exception($"Creds: {_client.Credentials.GetToken()}"));
        if (_client.Credentials == null) return discussionItems;
        
        try
        {
            // GraphQL query to get the last 5 discussions
            var query = @"{
              repository(owner: """ + owner + @""", name: """ + repo + @""") {
                discussions(first: 5, orderBy: {field: CREATED_AT, direction: DESC}) {
                  nodes {
                    title
                    author
                    bodyPreview
                    url
                    createdAt
                    categoryId
                    answered
                  }
                }
              }
            }";
            DebugLogger.Log("GraphQL Request", query);
            // Octokit uses the Connection to send GraphQL
            var response = await _client.Connection.Post<dynamic>(
                new Uri("https://api.github.com/graphql"),
                new { query },
                "application/json",
                "application/json"
            );

            //return response.HttpResponse.Body.ToString();
            string responseString = response.HttpResponse.Body.ToString() + "";
            DebugLogger.Log("GraphQL Response", responseString);

            discussionItems = ParseDiscussions(responseString)
                .Where(d => d.CreatedAt > _lastChecked)
                .OrderByDescending(d => d.CreatedAt)
                .ToList();
            _lastChecked = DateTime.UtcNow;

            return discussionItems;
        }
        catch (Exception ex)
        {
            await ErrorHelper.LogAndReportAsync( ex, "CMS.GetDiscussionsAsync error");
            return discussionItems;
        }
    }

    private static IReadOnlyList<DiscussionItem> ParseDiscussions(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("data", out var dataElement) ||
            !dataElement.TryGetProperty("repository", out var repositoryElement) ||
            !repositoryElement.TryGetProperty("discussions", out var discussionsElement) ||
            !discussionsElement.TryGetProperty("nodes", out var nodesElement))
        {
            return Array.Empty<DiscussionItem>();
        }

        List<DiscussionItem> result = new List<DiscussionItem>();
        foreach (var node in nodesElement.EnumerateArray())
        {
            var title = node.GetProperty("title").GetString() ?? "Без заголовка";
            var url = node.GetProperty("url").GetString() ?? string.Empty;
            var createdAt = node.GetProperty("createdAt").GetDateTime();
            string categoryId = node.GetProperty("categoryId").GetString() ?? string.Empty;
            bool answered = node.GetProperty("answered").GetBoolean();

            result.Add(new DiscussionItem(
                title,
                "Автор неизвестен",
                string.Empty,
                createdAt,
                url,
                categoryId,
                answered));
        }
        return result;
    }
}
