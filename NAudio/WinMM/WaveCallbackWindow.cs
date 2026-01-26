using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave;

/// <summary>
/// Win32 メッセージ専用ウィンドウ。Wave In/Out のウィンドウコールバック用。
/// Win32 API のみ使用し、WinForms/WPF に依存しない。
/// </summary>
internal sealed class WaveCallbackWindow : IDisposable
{
    private const int HWND_MESSAGE = -3;
    private const int GWLP_USERDATA = -21;
    private const uint WM_DESTROY = 0x0002;

    private static readonly string ClassName = "NAudioWaveCallbackWindow";
    private static bool _classRegistered;
    private static readonly object ClassLock = new();

    private readonly WaveInterop.WaveCallback _callback;
    private IntPtr _handle;
    private GCHandle _selfHandle;
    private bool _disposed;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WndClassEx wndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle,
        [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
        [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
        uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClassEx
    {
        public int cbSize;
        public int style;
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public IntPtr lpszMenuName;
        public IntPtr lpszClassName;
        public IntPtr hIconSm;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private static readonly WndProc WndProcDelegate = WndProcImpl;

    public WaveCallbackWindow(WaveInterop.WaveCallback callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        EnsureClassRegistered();
        _handle = CreateWindowExW(
            0,
            ClassName,
            "NAudioWave",
            0,
            0, 0, 0, 0,
            new IntPtr(HWND_MESSAGE),
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("CreateWindowEx failed.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
        _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        SetWindowLongPtr(_handle, GWLP_USERDATA, GCHandle.ToIntPtr(_selfHandle));
    }

    /// <summary>
    /// ウィンドウハンドル。
    /// </summary>
    public IntPtr Handle => _handle;

    private static void EnsureClassRegistered()
    {
        lock (ClassLock)
        {
            if (_classRegistered)
                return;
            var hInstance = Marshal.GetHINSTANCE(typeof(WaveCallbackWindow).Module);
            var wc = new WndClassEx
            {
                cbSize = Marshal.SizeOf<WndClassEx>(),
                style = 0,
                lpfnWndProc = WndProcDelegate,
                hInstance = hInstance,
                lpszClassName = Marshal.StringToHGlobalUni(ClassName)
            };
            if (RegisterClassExW(ref wc) == 0)
                throw new InvalidOperationException("RegisterClassEx failed.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
            Marshal.FreeHGlobal(wc.lpszClassName);
            _classRegistered = true;
        }
    }

    private static IntPtr WndProcImpl(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        if (uMsg == WM_DESTROY)
            return IntPtr.Zero;
        var gcHandle = GetWindowLongPtr(hWnd, GWLP_USERDATA);
        if (gcHandle != IntPtr.Zero && GCHandle.FromIntPtr(gcHandle).Target is WaveCallbackWindow self)
        {
            var message = (WaveInterop.WaveMessage)uMsg;
            switch (message)
            {
                case WaveInterop.WaveMessage.WaveOutDone:
                case WaveInterop.WaveMessage.WaveInData:
                    var waveHeader = new WaveHeader();
                    Marshal.PtrToStructure(lParam, waveHeader);
                    self._callback(wParam, message, IntPtr.Zero, waveHeader, IntPtr.Zero);
                    break;
                case WaveInterop.WaveMessage.WaveOutOpen:
                case WaveInterop.WaveMessage.WaveOutClose:
                case WaveInterop.WaveMessage.WaveInClose:
                case WaveInterop.WaveMessage.WaveInOpen:
                    self._callback(wParam, message, IntPtr.Zero, null!, IntPtr.Zero);
                    break;
            }
        }
        return DefWindowProcW(hWnd, uMsg, wParam, lParam);
    }

    /// <summary>
    /// ウィンドウを破棄し、リソースを解放する。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        if (_handle != IntPtr.Zero)
        {
            _selfHandle.Free();
            DestroyWindow(_handle);
            _handle = IntPtr.Zero;
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 既存ウィンドウをサブクラス化して Wave メッセージを受け取る。Win32 のみ使用し、WinForms/WPF に依存しない。
/// </summary>
internal sealed class WaveCallbackWindowSubclass : IDisposable
{
    private const int GWLP_WNDPROC = -4;

    private static readonly System.Collections.Generic.Dictionary<IntPtr, (WaveCallbackWindowSubclass Instance, IntPtr OldWndProc)> Subclasses = new();
    private static readonly object SubclassLock = new();

    private readonly WaveInterop.WaveCallback _callback;
    private readonly IntPtr _hwnd;
    private IntPtr _oldWndProc;
    private bool _disposed;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CallWindowProcW(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private static readonly WndProc SubclassWndProcDelegate = SubclassWndProcImpl;

    public WaveCallbackWindowSubclass(IntPtr hwnd, WaveInterop.WaveCallback callback)
    {
        _hwnd = hwnd;
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _oldWndProc = GetWindowLongPtr(_hwnd, GWLP_WNDPROC);
        var newProc = Marshal.GetFunctionPointerForDelegate(SubclassWndProcDelegate);
        SetWindowLongPtr(_hwnd, GWLP_WNDPROC, newProc);
        lock (SubclassLock)
        {
            Subclasses[_hwnd] = (this, _oldWndProc);
        }
    }

    private static IntPtr SubclassWndProcImpl(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        WaveCallbackWindowSubclass self = null!;
        IntPtr oldProc = IntPtr.Zero;
        lock (SubclassLock)
        {
            if (Subclasses.TryGetValue(hWnd, out var pair))
            {
                self = pair.Instance;
                oldProc = pair.OldWndProc;
            }
        }
        if (self != null)
        {
            var message = (WaveInterop.WaveMessage)uMsg;
            switch (message)
            {
                case WaveInterop.WaveMessage.WaveOutDone:
                case WaveInterop.WaveMessage.WaveInData:
                    var waveHeader = new WaveHeader();
                    Marshal.PtrToStructure(lParam, waveHeader);
                    self._callback(wParam, message, IntPtr.Zero, waveHeader, IntPtr.Zero);
                    return IntPtr.Zero;
                case WaveInterop.WaveMessage.WaveOutOpen:
                case WaveInterop.WaveMessage.WaveOutClose:
                case WaveInterop.WaveMessage.WaveInClose:
                case WaveInterop.WaveMessage.WaveInOpen:
                    self._callback(wParam, message, IntPtr.Zero, null!, IntPtr.Zero);
                    return IntPtr.Zero;
            }
            return CallWindowProcW(oldProc, hWnd, uMsg, wParam, lParam);
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        if (_hwnd != IntPtr.Zero && _oldWndProc != IntPtr.Zero)
        {
            lock (SubclassLock)
            {
                Subclasses.Remove(_hwnd);
            }
            SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _oldWndProc);
            _oldWndProc = IntPtr.Zero;
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
