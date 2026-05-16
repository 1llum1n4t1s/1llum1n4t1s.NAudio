using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NAudio.Utils
{
    /// <summary>
    /// General purpose native methods for internal NAudio use.
    /// </summary>
    /// <remarks>
    /// XML doc コメント上は「internal use」と書いてあるが、上流 NAudio から `public` で
    /// 露出した歴史的経緯がある (例: AcmDriver 等が呼ぶ)。本フォークでは後方互換のため
    /// public のまま残しつつ、IntelliSense / NuGet 表示から隠して新規利用を抑制する。
    /// 将来メジャーで `internal static class` に降格予定。
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class NativeMethods
    {
        /// <summary>
        /// Loads a DLL
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        /// <summary>
        /// Get procedure address
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);


        /// <summary>
        /// Free a library
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
    }
}
