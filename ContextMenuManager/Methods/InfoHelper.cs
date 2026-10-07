using System;
using System.Reflection;

namespace ContextMenuManager.Methods
{
    internal class InfoHelper
    {
        public static Version ProductVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
    }
}
