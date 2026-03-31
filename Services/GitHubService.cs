using Octokit;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CMS.Services
{
    internal class GitHubService
    {

        private readonly GitHubClient _client;

        public GitHubService(string token)
        {
            var projectName = Assembly.GetExecutingAssembly().GetName().Name;

            _client = new GitHubClient(new Octokit.ProductHeaderValue(projectName))
            {
                Credentials = new Octokit.Credentials(token)
            };
        }

        public async Task<string?> GetDiscussionsAsync(string owner, string repo)
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
    }
}
