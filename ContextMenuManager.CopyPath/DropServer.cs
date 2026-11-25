using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Threading;

namespace ContextMenuManager.CopyPath
{
    [ComVisible(true), Guid("00000001-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IClassFactory
    {
        [PreserveSig] int CreateInstance(IntPtr outer, ref Guid iid, out IntPtr instance);
        void LockServer([MarshalAs(UnmanagedType.Bool)] bool locked);
    }

    [ComVisible(true), Guid("00000122-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDropTarget
    {
        void DragEnter(IDataObject data, uint keys, long point, ref uint effect);
        void DragOver(uint keys, long point, ref uint effect);
        void DragLeave();
        void Drop(IDataObject data, uint keys, long point, ref uint effect);
    }

    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    internal sealed class DropServer(string mode) : IClassFactory, IDropTarget
    {
        private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        internal static int Run(Guid clsid, string mode)
        {
            OleInitialize(IntPtr.Zero);
            try
            {
                // 每次菜单调用由一个独立进程接收整组选项。
                CoRegisterClassObject(ref clsid, new DropServer(mode), 4 /* LOCAL_SERVER */, 0 /* SINGLEUSE */, out uint cookie);
                try { Dispatcher.Run(); }
                finally { CoRevokeClassObject(cookie); }
                return 0;
            }
            finally { OleUninitialize(); }
        }

        public int CreateInstance(IntPtr outer, ref Guid iid, out IntPtr instance)
        {
            instance = IntPtr.Zero;
            if (outer != IntPtr.Zero) return unchecked((int)0x80040110); // CLASS_E_NOAGGREGATION
            var unknown = Marshal.GetIUnknownForObject(this);
            try { return Marshal.QueryInterface(unknown, in iid, out instance); }
            finally { Marshal.Release(unknown); }
        }

        public void LockServer(bool locked) { }
        public void DragEnter(IDataObject data, uint keys, long point, ref uint effect) => effect &= 1;
        public void DragOver(uint keys, long point, ref uint effect) => effect &= 1;
        public void DragLeave() => dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);

        public void Drop(IDataObject data, uint keys, long point, ref uint effect)
        {
            try
            {
                effect = 0;
                // 跨进程的 COM 回调不一定在 STA 线程，剪贴板必须在 STA 上操作。
                dispatcher.Invoke(() =>
                {
                    if (Program.Copy(mode, ReadPaths(data)) != 0)
                        throw new COMException("未能复制选中项的路径。");
                });
                effect = 1; // DROPEFFECT_COPY
            }
            finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
        }

        private static string[] ReadPaths(IDataObject data)
        {
            var format = new FORMATETC { cfFormat = 15 /* CF_HDROP */, dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1, tymed = TYMED.TYMED_HGLOBAL };
            data.GetData(ref format, out var medium);
            try
            {
                var drop = medium.unionmember;
                var paths = new string[DragQueryFile(drop, uint.MaxValue, null, 0)];
                char[] buffer = [];
                for (uint i = 0; i < paths.Length; i++)
                {
                    int capacity = checked((int)DragQueryFile(drop, i, null, 0) + 1);
                    if (capacity > buffer.Length) buffer = new char[capacity];
                    int length = (int)DragQueryFile(drop, i, buffer, (uint)buffer.Length);
                    paths[i] = new string(buffer, 0, length);
                }
                return paths;
            }
            finally { ReleaseStgMedium(ref medium); }
        }

        [DllImport("ole32", PreserveSig = false)] private static extern void OleInitialize(IntPtr reserved);
        [DllImport("ole32")] private static extern void OleUninitialize();
        [DllImport("ole32", PreserveSig = false)] private static extern void CoRegisterClassObject(ref Guid clsid,
            [MarshalAs(UnmanagedType.IUnknown)] object factory, uint context, uint flags, out uint cookie);
        [DllImport("ole32")] private static extern int CoRevokeClassObject(uint cookie);
        [DllImport("ole32")] private static extern void ReleaseStgMedium(ref STGMEDIUM medium);
        [DllImport("shell32", CharSet = CharSet.Unicode)] private static extern uint DragQueryFile(IntPtr drop,
            uint index, [Out] char[] path, uint capacity);
    }
}
