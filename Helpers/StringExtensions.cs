using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CMS.Helpers;

public static class StringExtensions
{    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }

    public static string StripHtml(this string html)
    {
        return string.IsNullOrEmpty(html)
            ? string.Empty
            : Regex.Replace(html, "<[^>]*>", string.Empty);
    }
}
