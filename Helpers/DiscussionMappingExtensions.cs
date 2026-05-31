using CMS.Models;

namespace CMS.Helpers;


internal static class DiscussionMappingExtensions
{
    /// <summary>
    /// Преобразует элемент Atom-ленты обсуждений в универсальную модель <see cref="DiscussionItem"/>.
    /// </summary>
    public static DiscussionItem ToDiscussionItem(this DiscussionAtomEntry entry, int bodyPreviewMaxLength = 100)
    {
        string bodyPreview = (entry.ContentHtml ?? string.Empty).StripHtml()
            .Truncate(bodyPreviewMaxLength);

        return new DiscussionItem(
            Title: entry.Title,
            Author: entry.AuthorName,
            BodyPreview: bodyPreview,
            CreatedAt: entry.Published.DateTime,
            Url: entry.Link,
            categoryId: string.Empty,   // Atom-лента не содержит категорию
            answered: false             // атомарная лента не показывает статус отвеченности
        );
    }
}