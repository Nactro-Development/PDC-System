using PDC_System.Helpers;
using PDC_System.Properties; // important
using PDC_System.Settings;
using System;
using System.Collections.Generic;
using System.IO;
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



        private DispatcherTimer appTimer; // Timer variable
        private static readonly string CrashLogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");

        protected override async void OnStartup(StartupEventArgs e)
        {
            SQLitePCL.Batteries.Init();  // REQUIRED
            base.OnStartup(e);

            QuestPDF.Settings.License = LicenseType.Community; // ✅ Add this line

            // 🛡️ Global exception handlers for auto-restart
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // 🔧 FIX: enable legacy encodings (windows-1252, cp1252, etc.)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ThemeManager.UpdateAllWindows();
            CleanupOldHistory();


            // 🕒 Get interval from Settings.settings
            int intervalSeconds = PDC_System.Properties.Settings.Default.TimerIntervalMinutes;

            // ✅ Initialize and start the timer
            appTimer = new DispatcherTimer();
            appTimer.Interval = TimeSpan.FromMinutes(intervalSeconds);
            appTimer.Tick += AppTimer_Tick;
            appTimer.Tick += AppTimer_Tick2;
            appTimer.Start();


            Console.WriteLine($"✅ Timer started with {intervalSeconds} second interval");

            // Start auto-backup scheduler (reads user settings)
            AutoBackupScheduler.Initialize();
        }

        /// <summary>
        /// UI thread exceptions
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("DispatcherUnhandledException", e.Exception);
            e.Handled = true;
            RestartApplication();
        }

        /// <summary>
        /// Non-UI thread exceptions
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash("UnhandledException", ex);
            }
            RestartApplication();
        }

        /// <summary>
        /// Unobserved Task exceptions
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash("UnobservedTaskException", e.Exception);
            e.SetObserved();
            RestartApplication();
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
        /// App එක close කරලා නැවත start කරයි
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
                    NotificationHelper.ShowNotification("PDC System!", "IVMS Calclulate Complte!");

                }
                else
                {
                    var backgroundSettings = new SettingsWindow();
                    backgroundSettings.Visibility = Visibility.Hidden;

                    backgroundSettings.BtnLoad_Click();
                    backgroundSettings.Close();
                    NotificationHelper.ShowNotification("PDC System!", "IVMS Calclulate Complte!");

                }
            }
            catch (Exception ex)
            {
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
                NotificationHelper.ShowNotification("PDC System!", "Attendance Calclulate Complte!");
                // 💬 Optional on-screen message

            }
            catch (Exception ex)
            {
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
    }
}