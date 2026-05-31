using System;
using System.Collections.Generic;
using System.Text;

namespace CMS.Models;

internal class ShowAlertMessage
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Accept { get; init; } = "Ок";

}
