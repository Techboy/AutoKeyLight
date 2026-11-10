using System.Runtime.InteropServices;
using CamOnKeyLight.Core;

namespace CamOnKeyLight
{
    internal sealed class Win32HotkeyRegistrar : IHotkeyRegistrar
    {
        private readonly Func<IntPtr> handle;

        public Win32HotkeyRegistrar(Func<IntPtr> handle)
        {
            this.handle = handle;
        }

        public bool TryRegister(int id, HotkeyChord chord)
        {
            IntPtr hwnd = handle();
            if (hwnd == IntPtr.Zero || !HotkeyGrammar.TryVirtualKey(chord, out uint modifiers, out uint virtualKey))
                return false;

            return RegisterHotKey(hwnd, id, modifiers, virtualKey);
        }

        public void Unregister(int id)
        {
            IntPtr hwnd = handle();
            if (hwnd != IntPtr.Zero)
                UnregisterHotKey(hwnd, id);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
