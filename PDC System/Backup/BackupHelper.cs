using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace PDC_System.Backup
{
    // Minimal copy of the backup logic so it can run from a scheduler (no UI dependencies)
    public static class BackupHelper
    {
        private static readonly string EncryptionKey = "PDC_BACKUP_2025_SECURE_KEY";

        private static readonly string DefaultBackupFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PDC_Backups");

        private static readonly string SaversFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savers");
        private static readonly string PaysheetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PaysheetFile");

        public static async Task RunBackupAsync(string? customBackupFolder)
        {
            await Task.Run(() =>
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string zipFileName = $"PDC_Backup_{timestamp}.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), zipFileName);

                // Create ZIP
                CreateBackupZip(tempZipPath);

                // Encrypt
                byte[] zipBytes = File.ReadAllBytes(tempZipPath);
                byte[] encryptedBytes = EncryptData(zipBytes);
                string encryptedFileName = $"PDC_Backup_{timestamp}.pdcbak";

                // Save to default
                if (!Directory.Exists(DefaultBackupFolder))
                    Directory.CreateDirectory(DefaultBackupFolder);

                string defaultPath = Path.Combine(DefaultBackupFolder, encryptedFileName);
                File.WriteAllBytes(defaultPath, encryptedBytes);

                // Save to custom if provided and exists
                if (!string.IsNullOrEmpty(customBackupFolder) && Directory.Exists(customBackupFolder))
                {
                    string customPath = Path.Combine(customBackupFolder, encryptedFileName);
                    File.WriteAllBytes(customPath, encryptedBytes);
                }

                // Cleanup temp zip
                try { if (File.Exists(tempZipPath)) File.Delete(tempZipPath); } catch { }
            });
        }

        private static void CreateBackupZip(string zipPath)
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);

            var tempDbCopies = new System.Collections.Generic.List<string>();

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                if (Directory.Exists(SaversFolder))
                    AddFolderToZip(zip, SaversFolder, tempDbCopies);

                if (Directory.Exists(PaysheetFolder))
                    AddFolderToZip(zip, PaysheetFolder, tempDbCopies);

                // SETTINGS EXPORT
                string settingsFile = Path.Combine(Path.GetTempPath(), "settings_backup.txt");
                SettingsExport.Export(settingsFile);

                zip.CreateEntryFromFile(settingsFile, "Settings/settings_backup.txt");
            }



            foreach (var tmp in tempDbCopies)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        private static void AddFolderToZip(ZipArchive zip, string folderPath, System.Collections.Generic.List<string> tempDbCopies)
        {
            var baseDir = Path.GetDirectoryName(folderPath) ?? folderPath;
            foreach (var filePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(baseDir, filePath);
                string fileToAdd = filePath;

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

        private static void SafeCopySqliteDatabase(string sourceDbPath, string destDbPath)
        {
            if (File.Exists(destDbPath)) File.Delete(destDbPath);

            string sourceConnStr = $"Data Source={sourceDbPath};Mode=ReadOnly";
            using var sourceConn = new SqliteConnection(sourceConnStr);
            sourceConn.Open();

            using var cmd = sourceConn.CreateCommand();
            cmd.CommandText = $"VACUUM INTO @dest";
            cmd.Parameters.AddWithValue("@dest", destDbPath);
            cmd.ExecuteNonQuery();
        }

        private static byte[] EncryptData(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = GetEncryptionKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }

            return ms.ToArray();
        }

        private static byte[] GetEncryptionKey()
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey));
        }
    }
}