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

namespace CMS.Services
{
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

    public interface IGitHubService
    {
        Task<GitHubDeviceAuthorizationResponse?> RequestDeviceCodeAsync(string clientId, string scope);
        Task<GitHubDeviceTokenResponse?> ExchangeDeviceCodeForTokenAsync(string clientId, string deviceCode, CancellationToken cancellationToken = default);
        Task<string?> GetDiscussionsAsync(string owner, string repo);
        void SetToken(string? token);
    }

    internal class GitHubService : IGitHubService
    {

        private readonly GitHubClient _client;

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

        public async Task<string?> GetDiscussionsAsync(string owner, string repo)
        {
            try
            {
                // GraphQL query to get the last 5 discussions
                var query = @"{
              repository(owner: """ + owner + @""", name: """ + repo + @""") {
                discussions(first: 5, orderBy: {field: CREATED_AT, direction: DESC}) {
                  nodes {
                    title
                    url
                    createdAt
                  }
                }
              }
            }";

                // Octokit uses the Connection to send GraphQL
                var response = await _client.Connection.Post<dynamic>(
                    new Uri("https://github.com"),
                    new { query = query },
                    "application/json",
                    "application/json"
                );

                return response.HttpResponse.Body.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDiscussionsAsync error: {ex}");
                return null;
            }
        }
    }
}
