using System;
using System.Collections.Generic;
using System.Text;

namespace CMS.Services
{
    internal interface IPageService
    {
        event EventHandler<string> StaticMessageUpdated;
        void UpdateStaticMessage(string message);
        string GetStaticMessage();
    }
}
