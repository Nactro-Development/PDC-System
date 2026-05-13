using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace PDC_System.Backup
{
    public partial class RestoreHistoryWindow : Window
    {
        private static readonly string DefaultBackupFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PDC_Backups");

        private static readonly string EncryptionKey = "PDC_BACKUP_2025_SECURE_KEY";

        private BackupFileItem? _selectedItem;

        public RestoreHistoryWindow()
        {
            InitializeComponent();
            LoadBackupFiles();
        }

        // ==================== LOAD BACKUP FILE LIST ====================
        private void LoadBackupFiles()
        {
            if (!Directory.Exists(DefaultBackupFolder))
            {
                txtNoBackups.Visibility = Visibility.Visible;
                lstBackups.Visibility = Visibility.Collapsed;
                return;
            }

            var files = Directory.GetFiles(DefaultBackupFolder, "*.pdcbak")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            if (files.Count == 0)
            {
                txtNoBackups.Visibility = Visibility.Visible;
                lstBackups.Visibility = Visibility.Collapsed;
                return;
            }

            var items = files.Select((f, index) => new BackupFileItem
            {
                FullPath = f,
                FileName = Path.GetFileName(f),
                CreatedDate = File.GetCreationTime(f).ToString("yyyy-MM-dd  HH:mm"),
                FileSize = FormatFileSize(new FileInfo(f).Length),
                IsLatest = index == 0
            }).ToList();

            lstBackups.ItemsSource = items;
        }

        // ==================== SELECTION CHANGED ====================
        private void lstBackups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedItem = lstBackups.SelectedItem as BackupFileItem;

            if (_selectedItem is not null)
            {
                txtSelectedInfo.Text = $"Selected: {_selectedItem.FileName}";
                btnRestore.IsEnabled = true;
            }
            else
            {
                txtSelectedInfo.Text = "No backup selected.";
                btnRestore.IsEnabled = false;
            }
        }

        // ==================== RESTORE CLICK ====================
        private async void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem is null) return;

            var confirm = CustomMessageBox.Show(
                $"⚠️ This will overwrite your current data and restart the application!\n\n" +
                $"Restoring: {_selectedItem.FileName}\n\nAre you sure?",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            btnRestore.IsEnabled = false;
            txtSelectedInfo.Text = "Restoring… please wait.";

            try
            {
                await PerformRestoreAsync(_selectedItem.FullPath);
                RestartApplication();
            }
            catch (CryptographicException)
            {
                CustomMessageBox.Show("Invalid or corrupted backup file! Decryption failed.",
                    "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnRestore.IsEnabled = true;
                txtSelectedInfo.Text = "Restore failed.";
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Restore failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnRestore.IsEnabled = true;
                txtSelectedInfo.Text = "Restore failed.";
            }
        }

        // ==================== CLOSE ====================
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ==================== RESTORE LOGIC ====================
        private Task PerformRestoreAsync(string backupFilePath)
        {
            return Task.Run(() =>
            {
                byte[] encryptedBytes = File.ReadAllBytes(backupFilePath);
                byte[] zipBytes = DecryptData(encryptedBytes);

                string tempZipPath = Path.Combine(Path.GetTempPath(), "PDC_Restore_Temp.zip");
                File.WriteAllBytes(tempZipPath, zipBytes);

                try
                {
                    RestoreFromZip(tempZipPath);
                    
                    // Settings are restored through RestoreFromZip() above
                    // which handles app.config and user.config files
                }
                finally
                {
                    try { if (File.Exists(tempZipPath)) File.Delete(tempZipPath); } catch { }
                }
            });
        }

        private void RestoreFromZip(string zipPath)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

                if (entry.FullName == "Settings/app.config")
                {
                    try
                    {
                        string dest = System.Configuration.ConfigurationManager
                            .OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None).FilePath;
                        using var s = entry.Open();
                        using var fs = File.Create(dest);
                        s.CopyTo(fs);
                    }
                    catch { }
                    continue;
                }

                if (entry.FullName == "Settings/user.config")
                {
                    try
                    {
                        var cfg = System.Configuration.ConfigurationManager
                            .OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal);
                        string dest = cfg.FilePath;
                        string? destDir = Path.GetDirectoryName(dest);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);

                        using var s = entry.Open();
                        using var fs = File.Create(dest);
                        s.CopyTo(fs);
                    }
                    catch { }
                    continue;
                }

                string destPath = Path.Combine(baseDir, entry.FullName);
                string? dir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (destPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                    ReleaseSqliteFile(destPath);

                entry.ExtractToFile(destPath, overwrite: true);
            }
        }

        private static void ReleaseSqliteFile(string dbPath)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                SqliteConnection.ClearPool(conn);
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }

        private static void RestartApplication()
        {
            string exePath = Environment.ProcessPath!;
            Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
            Application.Current.Shutdown();
        }

        // ==================== DECRYPTION ====================
        private static byte[] DecryptData(byte[] encryptedData)
        {
            using var aes = Aes.Create();
            aes.Key = GetEncryptionKey();

            byte[] iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var res = new MemoryStream();
            cs.CopyTo(res);
            return res.ToArray();
        }

        private static byte[] GetEncryptionKey()
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey));
        }

        // ==================== HELPERS ====================
        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1_024) return $"{bytes / 1_024.0:F1} KB";
            return $"{bytes} B";
        }
    }

    // ==================== VIEW MODEL ====================
    public class BackupFileItem
    {
        public string FullPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public bool IsLatest { get; set; }

        public Visibility IsLatestVisibility =>
            IsLatest ? Visibility.Visible : Visibility.Collapsed;
    }
}