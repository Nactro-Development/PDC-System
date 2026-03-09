using System;
using System.Net.Http;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows;

public class UpdateManager
{
    class VersionInfo
    {
        public string version { get; set; }
        public string url { get; set; }
    }

    public static async Task CheckForUpdate()
    {
        try
        {
            // current app version
            string currentVersion =
                Assembly.GetExecutingAssembly()
                .GetName()
                .Version
                .ToString();

            HttpClient client = new HttpClient();

            // GitHub version.json RAW URL
            string json = await client.GetStringAsync(
            "https://raw.githubusercontent.com/USERNAME/REPO/main/version.json");

            VersionInfo data =
                JsonConvert.DeserializeObject<VersionInfo>(json);

            Version current = new Version(currentVersion);
            Version latest = new Version(data.version);

            if (latest > current)
            {
                var result = MessageBox.Show(
                    $"New version {latest} available.\nUpdate now?",
                    "Update",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    string tempFile = Path.Combine(
                        Path.GetTempPath(),
                        "update.exe");

                    byte[] file =
                        await client.GetByteArrayAsync(data.url);

                    File.WriteAllBytes(tempFile, file);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tempFile,
                        Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
                        UseShellExecute = true
                    });

                    Application.Current.Shutdown();
                }
            }
        }
        catch
        {
            // ignore update errors
        }
    }
}