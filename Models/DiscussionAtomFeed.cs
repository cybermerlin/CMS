using System;
using System.Collections.Generic;
using System.Text;

namespace CMS.Models;

internal class DiscussionAtomFeed
{
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset Updated { get; set; }
    public List<DiscussionAtomEntry> Entries { get; set; } = new();
}

internal class DiscussionAtomEntry
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTimeOffset Published { get; set; }
    public DateTimeOffset Updated { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorUri { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
}
