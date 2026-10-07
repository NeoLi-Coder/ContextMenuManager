using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ContextMenuManager.Methods
{
    internal static partial class MainWindowPlacement
    {
        private const uint MonitorDefaultToNearest = 2;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        /// <summary>按保存位置所在显示器恢复窗口，首次启动才在鼠标所在屏幕居中。</summary>
        public static void Restore(Window window)
        {
            var savedPosition = AppConfig.MainWindowPosition;
            NativePoint target;
            if (savedPosition.HasValue)
                target = new NativePoint { X = savedPosition.Value.X, Y = savedPosition.Value.Y };
            else if (!GetCursorPos(out target))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var monitor = MonitorFromPoint(target, MonitorDefaultToNearest);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var handle = new WindowInteropHelper(window).Handle;
            var requestedWidth = window.Width;
            var requestedHeight = window.Height;
            var work = info.Work;
            var workWidth = work.Right - work.Left;
            var workHeight = work.Bottom - work.Top;

            // 加载后移入目标屏，让 DPI 切换生效，避免用主屏缩放计算副屏位置。
            PlaceWindow(handle, work.Left, work.Top, workWidth, workHeight);
            var scale = GetDpiForWindow(handle) / 96.0;
            var availableWidth = workWidth / scale;
            var availableHeight = workHeight / scale;

            // 小屏或高缩放下，最小尺寸也不能超过工作区；恢复尺寸仍使用 WPF 逻辑单位。
            window.MinWidth = Math.Min(window.MinWidth, availableWidth);
            window.MinHeight = Math.Min(window.MinHeight, availableHeight);
            window.Width = Math.Clamp(requestedWidth, window.MinWidth, availableWidth);
            window.Height = Math.Clamp(requestedHeight, window.MinHeight, availableHeight);

            var width = Math.Min(workWidth, (int)Math.Round(window.Width * scale));
            var height = Math.Min(workHeight, (int)Math.Round(window.Height * scale));
            var left = savedPosition?.X ?? work.Left + (workWidth - width) / 2;
            var top = savedPosition?.Y ?? work.Top + (workHeight - height) / 2;
            // 显示器移除或工作区缩小时，仅调整越界坐标，不覆盖正常保存的位置。
            PlaceWindow(handle, Math.Clamp(left, work.Left, work.Right - width),
                Math.Clamp(top, work.Top, work.Bottom - height), width, height);
        }

        public static void SavePosition(Window window)
        {
            // 最大化、最小化的坐标不是用户调整的普通窗口位置。
            if (window.WindowState != WindowState.Normal) return;

            var handle = new WindowInteropHelper(window).Handle;
            if (!GetWindowRect(handle, out var rect))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            AppConfig.MainWindowPosition = new System.Drawing.Point(rect.Left, rect.Top);
        }

        private static void PlaceWindow(IntPtr handle, int left, int top, int width, int height)
        {
            if (!SetWindowPos(handle, IntPtr.Zero, left, top, width, height, SwpNoZOrder | SwpNoActivate))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetCursorPos(out NativePoint point);

        [LibraryImport("user32.dll")]
        private static partial IntPtr MonitorFromPoint(NativePoint point, uint flags);

        [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [LibraryImport("user32.dll")]
        private static partial uint GetDpiForWindow(IntPtr handle);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr handle, out NativeRect rect);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int left, int top,
            int width, int height, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public uint Flags;
        }
    }
}
