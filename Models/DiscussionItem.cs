using System;
using System.Collections.Generic;
using System.Text;

namespace CMS.Models;

internal record DiscussionItem(string Title, string Author, string BodyPreview,
    DateTime CreatedAt, string Url, string categoryId, bool answered)
{
    public string CreatedAtText => $"Создано: {CreatedAt.ToLocalTime():g}";
    public string answeredAtText => $"Есть ответ: {answered.ToString()}";
}
