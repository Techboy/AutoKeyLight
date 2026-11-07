namespace AutoKeyLight
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (UpdateInstaller.IsApplyRequest(args))
            {
                UpdateInstaller.Run(args);
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
    }
}
