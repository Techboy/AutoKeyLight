namespace AutoKeyLight
{
    internal static class SettingsMigrator
    {
        public static void Migrate()
        {
            if (!Properties.Settings.Default.UpgradeRequired)
                return;

            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.UpgradeRequired = false;
            Properties.Settings.Default.Save();
        }
    }
}
