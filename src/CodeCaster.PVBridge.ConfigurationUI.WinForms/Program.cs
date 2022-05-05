using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace CodeCaster.PVBridge.ConfigurationUI.WinForms
{
    internal static class Program
    {
        private static DebugForm? _debugForm;
        private static ConfigurationForm? _configurationForm;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 1 && args[0].ToUpperInvariant() == "--STARTUP")
            {
                ShowNotifyIcon();
            }
            else
            {
                Application.Run(new ConfigurationForm());
            }
        }

        private static void ShowNotifyIcon()
        {
            // TODO: InitForm(), or rather, MVVM-style, or rather not?
            if (_debugForm == null)
            {
                _debugForm = new DebugForm();
                _debugForm.FormClosed += (_, _) =>
                {
                    Application.Exit();
                };
            }

            var components = new Container();

            var notifyIcon = new NotifyIcon(components)
            {
                Icon = _debugForm.Icon,// new Icon("appicon.ico");

                Text = "PVBridge Solar Status Syncer",
                Visible = true
            };

            var menuStrip = new ContextMenuStrip();
            menuStrip.Items.Add("Debug", null, (sender, e) =>
            {
                _debugForm.Show();
                _debugForm.Activate();
            });

            menuStrip.Items.Add("Configuration", null, (sender, e) =>
            {
                _debugForm.Hide();

                if (_configurationForm == null)
                {
                    _configurationForm = new ConfigurationForm();
                    _configurationForm.FormClosed += (_, _) =>
                    {
                        Application.Exit();
                    };
                }
                
                _configurationForm.Show();
            });

            menuStrip.Items.Add("-");

            menuStrip.Items.Add("Exit", null, (sender, e) => Application.Exit());

            notifyIcon.ContextMenuStrip = menuStrip;

            notifyIcon.DoubleClick += (_, _) =>
            {
                if (_debugForm.Visible)
                {
                    _debugForm.Hide();
                }
                else
                {
                    _debugForm.Show();
                }
            };


            Application.Run();
        }
    }
}
