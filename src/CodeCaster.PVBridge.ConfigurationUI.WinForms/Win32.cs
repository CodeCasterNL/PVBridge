using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;

namespace CodeCaster.PVBridge.ConfigurationUI.WinForms
{
    // ReSharper disable InconsistentNaming - keep Win32 names for constants
    public static class Win32
    {
        [DllImport("user32")]
        private static extern uint SendMessage(IntPtr hWnd, uint msg, uint wParam, uint lParam);

        private const int BCM_FIRST = 0x1600; // Normal button
        private const int BCM_SETSHIELD = BCM_FIRST + 0x000C; // Elevated button
        private const int UserCanceledUacRequest = 1223;

        public static void AddShieldToButton(Button button)
        {
            button.FlatStyle = FlatStyle.System;

            _ = SendMessage(button.Handle, BCM_SETSHIELD, 0, 0xFFFFFFFF);
        }

        public static bool IsElevated
        {
            get
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static void RestartApplicationElevated()
        {
            var startInfo = new ProcessStartInfo
            {
                Verb = "runas",
                UseShellExecute = true,
                FileName = Application.ExecutablePath,
                WorkingDirectory = Environment.CurrentDirectory,
            };

            try
            {
                _ = Process.Start(startInfo);

                Application.Exit();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == UserCanceledUacRequest)
            {
                // User canceled, do nothing.
            }
        }
    }
}
