using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Configuration;
using PDC_System.Backup;

namespace PDC_System
{
    public partial class BackupWindow : Window
    {
        // Default backup location = Documents\PDC_Backups
        private readonly string defaultBackupFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PDC_Backups");

        // Custom backup location (user can set)
        private string customBackupFolder = string.Empty;

        // Source folders to backup
        private readonly string saversFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savers");
        private readonly string paysheetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PaysheetFile");

        // Encryption key
        private static readonly string EncryptionKey = "PDC_BACKUP_2025_SECURE_KEY";

        // --- Log persistence ---
        // Log stored in Documents\PDC_Backups\BackupWindow.log.txt
        private readonly string logDirectory;
        private readonly string logFilePath;
        private readonly object logFileLock = new();

        public BackupWindow()
        {
            InitializeComponent();

            logDirectory = defaultBackupFolder;
            logFilePath = Path.Combine(logDirectory, "BackupWindow.log.txt");

            LoadCustomBackupPath();
            LoadAutoBackupSettings();

            // Load persisted log and prune entries older than 7 days
            LoadAndPruneLogFile();

            LogMessage("Backup window loaded.");
            LogMessage($"Default backup location: {defaultBackupFolder}");
        }

        // ==================== LOAD SAVED CUSTOM PATH ====================
        private void LoadCustomBackupPath()
        {
            string savedPath = Properties.Settings.Default.CustomBackupPath;
            if (!string.IsNullOrEmpty(savedPath))
            {
                customBackupFolder = savedPath;
                txtCustomBackupPath.Text = savedPath;
                LogMessage($"Loaded saved custom backup location: {savedPath}");
            }
        }

        // ==================== LOAD AUTO BACKUP SETTINGS ====================
        private void LoadAutoBackupSettings()
        {
            try
            {
                chkAutoBackup.IsChecked = Properties.Settings.Default.AutoBackupEnabled;
                txtAutoBackupInterval.Text = Properties.Settings.Default.AutoBackupIntervalMinutes.ToString();
                LogMessage("Auto backup settings loaded.");
            }
            catch
            {
                // best-effort
            }
        }

        // ==================== SAVE AUTO BACKUP SETTINGS ====================
        private void SaveAutoBackupSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool enabled = chkAutoBackup.IsChecked == true;

                if (!int.TryParse(txtAutoBackupInterval.Text.Trim(), out int minutes) || minutes < 1)
                {
                    CustomMessageBox.Show("Please enter a valid number of minutes (>= 1).", "Invalid Interval",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Properties.Settings.Default.AutoBackupEnabled = enabled;
                Properties.Settings.Default.AutoBackupUseInterval = true;
                Properties.Settings.Default.AutoBackupIntervalMinutes = minutes;
                Properties.Settings.Default.Save();

                if (enabled)
                {
                    // Update scheduler with the new interval (do not start immediate backup by default)
                    AutoBackupScheduler.UpdateInterval(minutes, enableInterval: true, startImmediately: false);
                    LogMessage($"Auto backup enabled. Interval set to {minutes} minute(s).");
                }
                else
                {
                    // Disable scheduling
                    AutoBackupScheduler.UpdateInterval(minutes, enableInterval: false, startImmediately: false);
                    LogMessage("Auto backup disabled.");
                }

                CustomMessageBox.Show("Auto backup settings saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to save auto backup settings: {ex.Message}");
                CustomMessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== CLOSE ====================
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ==================== BROWSE CUSTOM BACKUP LOCATION ====================
        private void BrowseBackupLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Custom Backup Location",
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                customBackupFolder = dialog.FileName;
                txtCustomBackupPath.Text = customBackupFolder;

                // Save to Settings
                Properties.Settings.Default.CustomBackupPath = customBackupFolder;
                Properties.Settings.Default.Save();

                LogMessage($"Custom backup location set and saved: {customBackupFolder}");
            }
        }

        // ==================== BACKUP ====================
        private async void SaveAndBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool useCustom = !string.IsNullOrEmpty(customBackupFolder) && Directory.Exists(customBackupFolder);

                if (useCustom)
                {
                    LogMessage($"Backup will be saved to default + custom location: {customBackupFolder}");
                }
                else
                {
                    LogMessage("Backup will be saved to default location only.");
                }

                LogMessage("Starting backup...");

                await Task.Run(() =>
                {
                    // Generate zip file name with date & time
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string zipFileName = $"PDC_Backup_{timestamp}.zip";

                    // Create temp zip first
                    string tempZipPath = Path.Combine(Path.GetTempPath(), zipFileName);

                    // Create the zip from Savers + PaysheetFile folders
                    CreateBackupZip(tempZipPath);

                    // Encrypt the zip
                    byte[] zipBytes = File.ReadAllBytes(tempZipPath);
                    byte[] encryptedBytes = EncryptData(zipBytes);

                    // Save encrypted file with .pdcbak extension
                    string encryptedFileName = $"PDC_Backup_{timestamp}.pdcbak";

                    // Save to DEFAULT location
                    if (!Directory.Exists(defaultBackupFolder))
                        Directory.CreateDirectory(defaultBackupFolder);

                    string defaultPath = Path.Combine(defaultBackupFolder, encryptedFileName);
                    File.WriteAllBytes(defaultPath, encryptedBytes);

                    Dispatcher.Invoke(() => LogMessage($"✅ Backup saved to default: {defaultPath}"));

                    // Save to CUSTOM location (if set via Browse button)
                    if (useCustom)
                    {
                        if (!Directory.Exists(customBackupFolder))
                            Directory.CreateDirectory(customBackupFolder);

                        string customPath = Path.Combine(customBackupFolder, encryptedFileName);
                        File.WriteAllBytes(customPath, encryptedBytes);

                        Dispatcher.Invoke(() => LogMessage($"✅ Backup saved to custom: {customPath}"));
                    }

                    // Cleanup temp file
                    if (File.Exists(tempZipPath))
                        File.Delete(tempZipPath);
                });

                LogMessage("🎉 Backup completed successfully!");
                NotificationHelper.ShowNotification("PDC System!", "Backup completed successfully! ✅");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Backup failed: {ex.Message}");
                CustomMessageBox.Show($"Backup failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== RESTORE (Default - Last Backup) ====================
        // ==================== RESTORE (Opens History Window) ====================
        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new RestoreHistoryWindow { Owner = this };
            historyWindow.ShowDialog();
        }


        // ==================== CUSTOM RESTORE (From Custom Location) ====================
        private async void CustomRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Let user select a .pdcbak file from any location
                var openDialog = new OpenFileDialog
                {
                    Title = "Select Backup File to Restore",
                    Filter = "PDC Backup Files (*.pdcbak)|*.pdcbak",
                    InitialDirectory = !string.IsNullOrEmpty(customBackupFolder) && Directory.Exists(customBackupFolder)
                        ? customBackupFolder
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (openDialog.ShowDialog() != true)
                {
                    LogMessage("Custom restore cancelled by user.");
                    return;
                }

                string backupFilePath = openDialog.FileName;
                LogMessage($"Selected backup file: {backupFilePath}");

                // Confirm restore
                var confirmResult = CustomMessageBox.Show(
                    "⚠️ This will overwrite your current data and restart the application!\n\nAre you sure you want to restore from this file?",
                    "Confirm Custom Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult != MessageBoxResult.Yes)
                {
                    LogMessage("Custom restore cancelled.");
                    return;
                }

                LogMessage("Starting custom restore...");

                await PerformRestore(backupFilePath);

                LogMessage("🎉 Custom restore completed successfully! Restarting...");
                RestartApplication();
            }
            catch (CryptographicException)
            {
                LogMessage("❌ Restore failed: Invalid or corrupted backup file.");
                CustomMessageBox.Show("Invalid or corrupted backup file! Decryption failed.",
                    "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Restore failed: {ex.Message}");
                CustomMessageBox.Show($"Restore failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== AUTO RESTART ====================
        /// <summary>
        /// Restarts the application after a successful restore by launching a new 
        /// instance of the current exe and shutting down the current one.
        /// </summary>
        private void RestartApplication()
        {
            string exePath = Environment.ProcessPath!;

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });

            Application.Current.Shutdown();
        }

        // ==================== SHARED RESTORE LOGIC ====================
        private async Task PerformRestore(string backupFilePath)
        {
            await Task.Run(() =>
            {
                // Read encrypted backup
                byte[] encryptedBytes = File.ReadAllBytes(backupFilePath);

                // Decrypt
                byte[] zipBytes = DecryptData(encryptedBytes);

                // Write temp zip
                string tempZipPath = Path.Combine(Path.GetTempPath(), "PDC_Restore_Temp.zip");
                File.WriteAllBytes(tempZipPath, zipBytes);

                // Extract and restore
                RestoreFromZip(tempZipPath);

                // Cleanup
                if (File.Exists(tempZipPath))
                    File.Delete(tempZipPath);
            });
        }

        // ==================== ZIP CREATION ====================
        private void CreateBackupZip(string zipPath)
        {
            // Delete existing temp zip
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            // Track temp copies of .db files so we can clean them up
            var tempDbCopies = new System.Collections.Generic.List<string>();

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Add Savers folder
                if (Directory.Exists(saversFolder))
                {
                    AddFolderToZip(zip, saversFolder, "Savers", tempDbCopies);
                    Dispatcher.Invoke(() => LogMessage("📁 Savers folder added to backup."));
                }
                else
                {
                    Dispatcher.Invoke(() => LogMessage("⚠️ Savers folder not found, skipping."));
                }

                // Add PaysheetFile folder
                if (Directory.Exists(paysheetFolder))
                {
                    AddFolderToZip(zip, paysheetFolder, "PaysheetFile", tempDbCopies);
                    Dispatcher.Invoke(() => LogMessage("📁 PaysheetFile folder added to backup."));
                }
                else
                {
                    Dispatcher.Invoke(() => LogMessage("⚠️ PaysheetFile folder not found, skipping."));
                }

                // Add application config (app.config / exe.config)
                try
                {
                    string exeConfig = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
                    if (!string.IsNullOrEmpty(exeConfig) && File.Exists(exeConfig))
                    {
                        zip.CreateEntryFromFile(exeConfig, "Settings/app.config", CompressionLevel.Optimal);
                        Dispatcher.Invoke(() => LogMessage($"⚙️ Application config added to backup: {exeConfig}"));
                    }
                }
                catch { /* best-effort */ }

                // Add per-user config (user.config)
                try
                {
                    var userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                    string userConfigPath = userConfig.FilePath;
                    if (!string.IsNullOrEmpty(userConfigPath) && File.Exists(userConfigPath))
                    {
                        zip.CreateEntryFromFile(userConfigPath, "Settings/user.config", CompressionLevel.Optimal);
                        Dispatcher.Invoke(() => LogMessage($"⚙️ User config added to backup: {userConfigPath}"));
                    }
                }
                catch { /* best-effort */ }
            }

            // Cleanup temporary .db copies
            foreach (var tempFile in tempDbCopies)
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }

        private void AddFolderToZip(ZipArchive zip, string folderPath, string entryPrefix,
            System.Collections.Generic.List<string> tempDbCopies)
        {
            foreach (string filePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(Path.GetDirectoryName(folderPath)!, filePath);
                string fileToAdd = filePath;

                // If it's a SQLite .db file, create a safe copy using SQLite backup API
                if (filePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                {
                    string tempCopy = Path.Combine(Path.GetTempPath(), $"backup_{Path.GetFileName(filePath)}");
                    SafeCopySqliteDatabase(filePath, tempCopy);
                    fileToAdd = tempCopy;
                    tempDbCopies.Add(tempCopy);
                }

                zip.CreateEntryFromFile(fileToAdd, relativePath, CompressionLevel.Optimal);
            }
        }

        /// <summary>
        /// Safely copies a SQLite database that may be locked by another connection,
        /// using SQLite's built-in online backup API.
        /// </summary>
        private void SafeCopySqliteDatabase(string sourceDbPath, string destDbPath)
        {
            // Remove destination if it already exists
            if (File.Exists(destDbPath))
                File.Delete(destDbPath);

            // Open source in read-only mode to avoid conflicts
            string sourceConnStr = $"Data Source={sourceDbPath};Mode=ReadOnly";

            using var sourceConn = new SqliteConnection(sourceConnStr);
            sourceConn.Open();

            // VACUUM INTO creates the destination file itself — no need to open a destConn
            using var cmd = sourceConn.CreateCommand();
            cmd.CommandText = $"VACUUM INTO @dest";
            cmd.Parameters.AddWithValue("@dest", destDbPath);
            cmd.ExecuteNonQuery();
        }

        // ==================== RESTORE FROM ZIP ====================
        private void RestoreFromZip(string zipPath)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            using (var zip = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue; // Skip directory entries

                    // Special handling for saved settings
                    if (entry.FullName == "Settings/app.config")
                    {
                        try
                        {
                            string dest = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
                            using var entryStream = entry.Open();
                            using var fs = File.Create(dest);
                            entryStream.CopyTo(fs);
                            Dispatcher.Invoke(() => LogMessage($"⚙️ Restored application config to: {dest}"));
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => LogMessage($"⚠️ Failed to restore app config: {ex.Message}"));
                        }
                        continue;
                    }

                    if (entry.FullName == "Settings/user.config")
                    {
                        try
                        {
                            var userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                            string dest = userConfig.FilePath;
                            // Ensure destination directory exists
                            string? destDir = Path.GetDirectoryName(dest);
                            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                                Directory.CreateDirectory(destDir);

                            using var entryStream = entry.Open();
                            using var fs = File.Create(dest);
                            entryStream.CopyTo(fs);

                            Dispatcher.Invoke(() => LogMessage($"⚙️ Restored user config to: {dest}"));
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => LogMessage($"⚠️ Failed to restore user config: {ex.Message}"));
                        }
                        continue;
                    }

                    // Default behaviour - restore into application folder structure
                    string destinationPath = Path.Combine(baseDir, entry.FullName);
                    string? destinationDir = Path.GetDirectoryName(destinationPath);

                    if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                        Directory.CreateDirectory(destinationDir);

                    // For .db files, release all pooled SQLite connections before overwriting
                    if (destinationPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                    {
                        ReleaseSqliteFile(destinationPath);
                    }

                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            Dispatcher.Invoke(() => LogMessage("📂 All files restored to original locations."));
        }

        /// <summary>
        /// Releases all pooled SQLite connections holding a lock on the given .db file,
        /// so the file can be overwritten during restore.
        /// </summary>
        private void ReleaseSqliteFile(string dbPath)
        {
            try
            {
                // Clear the specific connection pool for this database
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                SqliteConnection.ClearPool(conn);

                // Also clear all pools as a safety net
                SqliteConnection.ClearAllPools();

                // Give the OS a moment to release the file handle
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch
            {
                // Best-effort — if it fails, ExtractToFile will throw a clearer error
            }
        }

        // ==================== ENCRYPTION (AES-256) ====================
        private static byte[] EncryptData(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = GetEncryptionKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();

            // Write IV first (16 bytes) so we can read it during decryption
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }

            return ms.ToArray();
        }

        private static byte[] DecryptData(byte[] encryptedData)
        {
            using var aes = Aes.Create();
            aes.Key = GetEncryptionKey();

            // Read IV from first 16 bytes
            byte[] iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var resultMs = new MemoryStream();

            cs.CopyTo(resultMs);
            return resultMs.ToArray();
        }

        private static byte[] GetEncryptionKey()
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey));
        }

        // ==================== LOG ====================
        /// <summary>
        /// Appends a message to the on-screen history and persists it to a text file.
        /// Log file format: each line = "[yyyy-MM-dd HH:mm:ss] message"
        /// On load the file is pruned to only keep the last 7 days.
        /// </summary>
        private void LogMessage(string message)
        {
            // UI display uses short time as before
            string displayTimestamp = DateTime.Now.ToString("HH:mm:ss");
            txtHistory.AppendText($"[{displayTimestamp}] {message}\n");
            txtHistory.ScrollToEnd();

            // Persist full timestamp + message to file
            try
            {
                AppendToLogFile(message);
            }
            catch
            {
                // best-effort — do not crash UI if logging fails
            }
        }

        /// <summary>
        /// Reads the persisted log file, removes entries older than 7 days, writes the pruned file,
        /// and displays the remaining entries in the txtHistory control.
        /// </summary>
        private void LoadAndPruneLogFile()
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                if (!File.Exists(logFilePath))
                    return;

                string[] allLines;
                lock (logFileLock)
                {
                    allLines = File.ReadAllLines(logFilePath);
                }

                var keepLines = new System.Collections.Generic.List<string>();
                DateTime cutoff = DateTime.Now.AddDays(-7);

                foreach (var line in allLines)
                {
                    // Expect format: "[yyyy-MM-dd HH:mm:ss] message"
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    int closeIdx = line.IndexOf(']');
                    if (line.StartsWith("[") && closeIdx > 1)
                    {
                        string tsPart = line.Substring(1, closeIdx - 1);
                        if (DateTime.TryParseExact(tsPart, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime ts))
                        {
                            if (ts >= cutoff)
                            {
                                keepLines.Add(line);
                            }
                            // else skip (older than 7 days)
                            continue;
                        }
                    }

                    // If we can't parse the timestamp, keep the line (conservative)
                    keepLines.Add(line);
                }

                // Overwrite file with pruned lines
                lock (logFileLock)
                {
                    File.WriteAllLines(logFilePath, keepLines);
                }

                // Display kept lines in UI (clear existing)
                txtHistory.Clear();
                foreach (var line in keepLines)
                {
                    // Parse again to show only HH:mm:ss as UI did before
                    int closeIdx = line.IndexOf(']');
                    string messagePart = line;
                    DateTime? ts = null;
                    if (line.StartsWith("[") && closeIdx > 1)
                    {
                        string tsPart = line.Substring(1, closeIdx - 1);
                        if (DateTime.TryParseExact(tsPart, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime parsed))
                        {
                            ts = parsed;
                            messagePart = line.Substring(closeIdx + 2); // skip "] "
                        }
                    }

                    string displayTime = ts?.ToString("HH:mm:ss") ?? DateTime.Now.ToString("HH:mm:ss");
                    txtHistory.AppendText($"[{displayTime}] {messagePart}\n");
                }

                txtHistory.ScrollToEnd();
            }
            catch
            {
                // best-effort; don't prevent window from loading
            }
        }

        /// <summary>
        /// Appends a single log line to the log file in a thread-safe manner.
        /// Format: "[yyyy-MM-dd HH:mm:ss] message"
        /// </summary>
        private void AppendToLogFile(string message)
        {
            try
            {
                lock (logFileLock)
                {
                    if (!Directory.Exists(logDirectory))
                        Directory.CreateDirectory(logDirectory);

                    string line = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {message}";
                    File.AppendAllLines(logFilePath, new[] { line });
                }
            }
            catch
            {
                // best-effort
            }
        }
    }
}