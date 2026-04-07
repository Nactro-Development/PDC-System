using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;

public enum UpdateState
{
    None,
    Checking,
    Available,
    Downloading,
    Downloaded
}

public class VersionInfo
{
    public string version { get; set; }
    public string url { get; set; }
}

public static class UpdateManager
{
    public static UpdateState State { get; private set; } = UpdateState.None;

    public static double Progress { get; private set; }

    public static bool AutoInstall { get; set; } = false;

    public static string InstallerPath =
        Path.Combine(Path.GetTempPath(), "PDCUpdate.exe");

    // NEW: expose latest discovered version and URL for UI
    public static string? LatestVersion { get; private set; }
    public static string? LatestUrl { get; private set; }

    public static event Action<double>? ProgressChanged;
    public static event Action<UpdateState>? StateChanged;

    static void SetState(UpdateState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }

    public static async Task CheckForUpdates()
    {
        if (State == UpdateState.Downloading)
            return;

        try
        {
            SetState(UpdateState.Checking);

            using HttpClient client = new HttpClient();

            string json = await client.GetStringAsync(
            "https://raw.githubusercontent.com/Nactro-Development/PDC-System-Updater/refs/heads/main/version.json");

            VersionInfo data =
                JsonConvert.DeserializeObject<VersionInfo>(json);

            string currentVersion =
                Assembly.GetExecutingAssembly()
                .GetName()
                .Version
                .ToString();

            Version current = new Version(currentVersion);
            Version latest = new Version(data.version);

            // store latest info for UI consumption
            LatestVersion = data.version;
            LatestUrl = data.url;

            if (latest > current)
            {
                SetState(UpdateState.Available);

                // Note: existing behavior proceeds to download immediately.
                // Keep existing flow — UI will be notified of Available -> Downloading -> Downloaded.
                await DownloadUpdate(data.url);
            }
            else
            {
                SetState(UpdateState.None);
            }
        }
        catch
        {
            SetState(UpdateState.None);
        }
    }

    public static async Task DownloadUpdate(string url)
    {
        try
        {
            SetState(UpdateState.Downloading);

            using HttpClient client = new HttpClient();

            var response = await client.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead);

            var total = response.Content.Headers.ContentLength ?? 1;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var file = File.Create(InstallerPath);

            byte[] buffer = new byte[8192];
            long read = 0;

            while (true)
            {
                int bytes = await stream.ReadAsync(buffer);

                if (bytes == 0)
                    break;

                await file.WriteAsync(buffer, 0, bytes);

                read += bytes;

                Progress = (double)read / total;

                ProgressChanged?.Invoke(Progress);
            }

            SetState(UpdateState.Downloaded);

            if (AutoInstall)
                InstallUpdate();
        }
        catch
        {
            SetState(UpdateState.None);
        }
    }

    public static void InstallUpdate()
    {
        if (!File.Exists(InstallerPath))
            return;

        var result = System.Windows.MessageBox.Show(
            "Before installing the update please CLOSE the application on ALL CLIENT PCs.\n\n" +
            "If other PCs are still running the software the update may fail.\n\n" +
            "Do you want to continue?",
            "Update Warning",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = InstallerPath,
            Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
            UseShellExecute = true
        });

        System.Windows.Application.Current.Shutdown();
    }


}