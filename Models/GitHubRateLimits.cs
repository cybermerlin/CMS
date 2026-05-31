using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CMS.Models;

public class GitHubRateLimits
{
    public int CoreLimit { get; set; }
    public int CoreRemaining { get; set; }
    public DateTimeOffset CoreReset { get; set; }

    public int SearchLimit { get; set; }
    public int SearchRemaining { get; set; }
    public DateTimeOffset SearchReset { get; set; }

    public int GraphQLLimit { get; set; }
    public int GraphQLRemaining { get; set; }
    public DateTimeOffset GraphQLReset { get; set; }

    public override string ToString()
    {
        return $"Core: {CoreRemaining}/{CoreLimit} (reset: {CoreReset:dd.MM.yyyy HH:mm}), " +
               $"Search: {SearchRemaining}/{SearchLimit} (reset: {SearchReset:dd.MM.yyyy HH:mm}), " +
               $"GraphQL: {GraphQLRemaining}/{GraphQLLimit} (reset: {GraphQLReset:dd.MM.yyyy HH:mm})";
    }
    public string ToJson() => JsonSerializer.Serialize(this);
}
