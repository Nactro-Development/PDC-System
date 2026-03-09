using System;
using System.Threading;
using System.Threading.Tasks;

namespace PDC_System.Backup
{
    public static class AutoBackupScheduler
    {
        private static Timer? _timer;

        // Call this once (e.g., from App.xaml.cs OnStartup or Home UI Loaded)
        public static void Initialize()
        {
            try
            {
                bool enabled = Properties.Settings.Default.AutoBackupEnabled;
                if (!enabled)
                    return;

                bool useInterval = Properties.Settings.Default.AutoBackupUseInterval;
                int intervalMinutes = Properties.Settings.Default.AutoBackupIntervalMinutes;

                if (useInterval && intervalMinutes > 0)
                {
                    // Start immediately, then repeat every interval
                    ScheduleInterval(TimeSpan.FromMinutes(intervalMinutes), startImmediately: true);
                }
            }
            catch
            {
                // best-effort scheduling; swallow exceptions to avoid crashing startup
            }
        }

        private static void ScheduleInterval(TimeSpan interval, bool startImmediately = true)
        {
            _timer?.Dispose();
            TimeSpan dueTime = startImmediately ? TimeSpan.Zero : interval;
            // Start after 'dueTime' and repeat every 'interval'
            _timer = new Timer(async _ => await IntervalTimerCallback(), null, dueTime, interval);
        }

        private static async Task IntervalTimerCallback()
        {
            try
            {
                string? customPath = Properties.Settings.Default.CustomBackupPath;
                await BackupHelper.RunBackupAsync(string.IsNullOrWhiteSpace(customPath) ? null : customPath);
            }
            catch
            {
                // swallow — scheduler should keep running
            }
            // periodic timer handles repetition
        }

        // Optional helper to change interval schedule at runtime (e.g., from Settings UI)
        public static void UpdateInterval(int minutes, bool enableInterval = true, bool startImmediately = true)
        {
            Properties.Settings.Default.AutoBackupUseInterval = enableInterval;
            Properties.Settings.Default.AutoBackupIntervalMinutes = Math.Max(1, minutes);
            Properties.Settings.Default.Save();

            if (enableInterval)
                ScheduleInterval(TimeSpan.FromMinutes(Properties.Settings.Default.AutoBackupIntervalMinutes), startImmediately);
            else
                _timer?.Dispose();
        }
    }
}