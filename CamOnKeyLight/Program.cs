using CamOnKeyLight.Core;

namespace CamOnKeyLight
{
    internal static class Program
    {
        private static Mutex? instanceMutex;

        [STAThread]
        static void Main(string[] args)
        {
            if (UpdateInstaller.IsApplyRequest(args))
            {
                UpdateInstaller.Run(args);
                return;
            }

            if (LegacyLaunch.TryHandOff(args))
                return;

            bool createdNew = false;
            try
            {
                instanceMutex = new Mutex(true, SettingsLocations.SingleInstanceMutex, out createdNew);
            }
            catch (AbandonedMutexException)
            {
                createdNew = true;
            }

            if (!createdNew)
            {
                instanceMutex?.Dispose();
                return;
            }

            try
            {
                SettingsMigrator.Migrate();
            }
            catch (System.Configuration.ConfigurationErrorsException)
            {
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(args));
        }

        public static void OpenElgatoWebsite()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.elgato.com/")
            {
                UseShellExecute = true
            });
        }
    }
}
