using System;
using System.Text;
using NetworkAdapterSelector.Hook.Unmanaged;

namespace NetworkAdapterSelector.Hook
{
    internal static class WindowTitle
    {
        public const string Separator = " <---> ";

        public static string GetWindowTitle(IntPtr windowHandler)
        {
            if (windowHandler != IntPtr.Zero && Window.IsWindow(windowHandler))
            {
                var length = Window.GetWindowTextLength(windowHandler);
                var sb = new StringBuilder(length + 1);
                Window.GetWindowText(windowHandler, sb, sb.Capacity);
                var title = sb.ToString();
                if (title.Contains(Separator))
                {
                    title = title.Substring(0, title.IndexOf(Separator, StringComparison.CurrentCulture));
                }
                return title;
            }
            return null;
        }

        public static bool CleanWindowsTitle(IntPtr windowHandler)
        {
            var title = GetWindowTitle(windowHandler);
            return !string.IsNullOrEmpty(title) && Window.SetWindowText(windowHandler, title);
        }

        public static string AppendWindowTitle(string title, string text)
        {
            if (title.Contains(Separator))
            {
                title = title.Substring(0, title.IndexOf(Separator, StringComparison.CurrentCulture));
            }
            return string.IsNullOrEmpty(text) || string.IsNullOrEmpty(title)
                ? title
                : title + Separator + text;
        }

        public static bool AppendWindowTitle(IntPtr windowHandler, string text)
        {
            var title = GetWindowTitle(windowHandler);
            return !string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(title) &&
                   Window.SetWindowText(windowHandler, AppendWindowTitle(title, text));
        }
    }
}