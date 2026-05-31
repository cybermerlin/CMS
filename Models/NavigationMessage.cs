using System;
using System.Collections.Generic;
using System.Text;

namespace CMS.Models;

internal class NavigationMessage
{
    public string? From { get; init; }
    public string? To { get; init; }
    public bool IsNavigating { get; init; } = false;
}
