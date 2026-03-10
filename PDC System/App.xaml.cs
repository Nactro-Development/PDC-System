using PDC_System.Helpers;
using PDC_System.Properties; // important
using PDC_System.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

using System.Threading;
using QuestPDF.Infrastructure;
using System.Text;
using System.Windows;
using System.Windows.Threading; // add this for DispatcherTimer
using System.Diagnostics;
using PDC_System.Backup; // Add this using directive

namespace PDC_System
{
    public partial class App : Application
    {

        private static Mutex _mutex;
        private static EventWaitHandle _showWindowEvent;
        private DispatcherTimer appTimer; // Timer variable
        private static readonly string CrashLogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");

        protected override async void OnStartup(StartupEventArgs e)
        {
            SQLitePCL.Batteries.Init();  // REQUIRED
            base.OnStartup(e);

            QuestPDF.Settings.License = LicenseType.Community;

            // 🛡️ Global exception handlers — NO auto-restart on minor errors
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // 🔧 FIX: enable legacy encodings (windows-1252, cp1252, etc.)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ThemeManager.UpdateAllWindows();
            CleanupOldHistory();

            if (PDC_System.Properties.Settings.Default.UpgradeRequired)
            {
                PDC_System.Properties.Settings.Default.Upgrade();
                PDC_System.Properties.Settings.Default.UpgradeRequired = false;
                PDC_System.Properties.Settings.Default.Save();
            }

            bool createdNew;

            _mutex = new Mutex(true, "PDC_System_Single_Instance", out createdNew);

            _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "PDC_System_Show_Window");

            if (!createdNew)
            {
                // ✅ Signal existing instance to come to foreground
                try
                {
                    _showWindowEvent?.Set();
                }
                catch { /* ignore */ }

                Shutdown();
                return;
            }

            // ✅ Listen for signal from second instance — bring window to foreground
            ThreadPool.RegisterWaitForSingleObject(
    _showWindowEvent,
    (state, timedOut) =>
    {
        try
        {
            Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // ✅ MainWindow වෙනුවට Home window එක හොයනවා
                    var window = Current.Windows
                        .OfType<Home>() // ← ඔබේ Home window class name එක
                        .FirstOrDefault();

                    if (window != null)
                    {
                        window.Show();

                        if (window.WindowState == WindowState.Minimized)
                            window.WindowState = WindowState.Normal;

                        window.Activate();
                        window.Topmost = true;
                        window.Topmost = false;
                        window.Focus();
                    }
                }
                catch (Exception ex)
                {
                    LogCrash("ShowWindow_Callback_UI", ex);
                }
            });
        }
        catch (Exception ex)
        {
            LogCrash("ShowWindow_Callback", ex);
        }
    },
    null,
    -1,
    false);




            // 🕒 Get interval from Settings.settings
            int intervalSeconds = PDC_System.Properties.Settings.Default.TimerIntervalMinutes;

            // ✅ Initialize and start the timer
            appTimer = new DispatcherTimer();
            appTimer.Interval = TimeSpan.FromMinutes(intervalSeconds);
            appTimer.Tick += AppTimer_Tick;
            appTimer.Tick += AppTimer_Tick2;
            appTimer.Start();

            Console.WriteLine($"✅ Timer started with {intervalSeconds} minute interval");

            // Start auto-backup scheduler (reads user settings)
            AutoBackupScheduler.Initialize();
        }

        /// <summary>
        /// UI thread exceptions — handle gracefully, DO NOT restart for minor errors
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("DispatcherUnhandledException", e.Exception);

            // ✅ Mark as handled so app continues running
            e.Handled = true;

            // ✅ Only restart for truly fatal errors (OutOfMemory, StackOverflow, etc.)
            if (e.Exception is OutOfMemoryException || e.Exception is StackOverflowException)
            {
                RestartApplication();
            }
            // ❌ DO NOT call RestartApplication() for all exceptions — causes restart loop
        }

        /// <summary>
        /// Non-UI thread exceptions — truly unrecoverable, safe to restart
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash("UnhandledException", ex);
            }

            // ✅ Only restart if it is truly terminating
            if (e.IsTerminating)
            {
                RestartApplication();
            }
        }

        /// <summary>
        /// Unobserved Task exceptions — observe and log, DO NOT restart
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash("UnobservedTaskException", e.Exception);

            // ✅ Mark as observed so app doesn't crash — DO NOT restart
            e.SetObserved();
        }

        /// <summary>
        /// Crash log file එකට error details save කරයි
        /// </summary>
        private static void LogCrash(string source, Exception ex)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n" +
                                  $"Message: {ex.Message}\n" +
                                  $"StackTrace: {ex.StackTrace}\n" +
                                  $"{"".PadRight(80, '-')}\n";

                File.AppendAllText(CrashLogPath, logEntry);
            }
            catch
            {
                // Logging itself failed — ignore to prevent infinite loop
            }
        }

        /// <summary>
        /// App එක close කරලා නැවත start කරයි — FATAL errors විතරකට call කරන්න
        /// </summary>
        private static void RestartApplication()
        {
            try
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                // Restart failed — app will just close
            }
            finally
            {
                Environment.Exit(1);
            }
        }

        private void AppTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var settingsWindow = Application.Current.Windows
                    .OfType<SettingsWindow>()
                    .FirstOrDefault();

                if (settingsWindow != null)
                {
                    settingsWindow.BtnLoad_Click();
                    NotificationHelper.ShowNotification("PDC System!", "IVMS Calculate Complete!");
                }
                else
                {
                    var backgroundSettings = new SettingsWindow();
                    backgroundSettings.Visibility = Visibility.Hidden;
                    backgroundSettings.BtnLoad_Click();
                    backgroundSettings.Close();
                    NotificationHelper.ShowNotification("PDC System!", "IVMS Calculate Complete!");
                }
            }
            catch (Exception ex)
            {
                // ✅ Log only — DO NOT crash or restart
                LogCrash("AppTimer_Tick", ex);
                CustomMessageBox.Show("❌ Timer error: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AppTimer_Tick2(object? sender, EventArgs e)
        {
            try
            {
                // 🔄 Create AttendanceManager
                var manager = new PDC_System.Services.AttendanceManager();

                // 🧾 Load all attendance (auto-refresh)
                var records = manager.LoadAttendance();

                // 💾 Save refreshed data
                manager.SaveAllAttendanceRecords(records);
                NotificationHelper.ShowNotification("PDC System!", "Attendance Calculate Complete!");
            }
            catch (Exception ex)
            {
                // ✅ Log only — DO NOT crash or restart
                LogCrash("AppTimer_Tick2", ex);
                CustomMessageBox.Show("❌ Auto-refresh failed:\n" + ex.Message,
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CleanupOldHistory()
        {
            string historyFilePath = Path.Combine(Directory.GetCurrentDirectory(), "backup_history.txt");

            if (!File.Exists(historyFilePath))
                return;

            try
            {
                var lines = File.ReadAllLines(historyFilePath);
                var filteredLines = new List<string>();
                DateTime sevenDaysAgo = DateTime.Now.AddDays(-7);

                foreach (var line in lines)
                {
                    if (line.Length < 21)
                        continue;

                    string datePart = line.Substring(1, 19); // [yyyy-MM-dd HH:mm:ss]
                    if (DateTime.TryParse(datePart, out DateTime logDate))
                    {
                        if (logDate >= sevenDaysAgo)
                            filteredLines.Add(line);
                    }
                }

                File.WriteAllLines(historyFilePath, filteredLines);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error cleaning up history: " + ex.Message);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }
}