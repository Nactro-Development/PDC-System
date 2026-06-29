using Microsoft.Win32;
using System;
using PDC_System.Services;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;

using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using XamlAnimatedGif;
using PDC_System.Helpers;

namespace PDC_System
{
    public partial class Loading : Window
    {
        #region Fields

       
        private bool _isLoggingIn = false; // 🔒 rapid login guard

        #endregion

        #region Constructor

        public Loading()
        {
            InitializeComponent();
            SeedAdmin();
           
            ThemeManager.ApplyTheme(this); // Apply initial theme
            LoadSavedCredentials();

        }

        #endregion

        #region Credentials

        private void LoadSavedCredentials()
        {
            if (Properties.Settings.Default.RememberMe)
            {
                // Auto-fill username and password
                UserName.Text = Properties.Settings.Default.SavedUsername;
                Password.Password = Properties.Settings.Default.SavedPassword;
                RememberMeCheckBox.IsChecked = true;
            }
        }

        /// <summary>
        /// Save credentials to Settings if Remember Me is checked
        /// </summary>
        private void SaveCredentials()
        {
            if (RememberMeCheckBox.IsChecked == true)
            {
                Properties.Settings.Default.RememberMe = true;
                Properties.Settings.Default.SavedUsername = UserName.Text;
                Properties.Settings.Default.SavedPassword = Password.Password;
            }
            else
            {
                // Clear saved credentials
                Properties.Settings.Default.RememberMe = false;
                Properties.Settings.Default.SavedUsername = string.Empty;
                Properties.Settings.Default.SavedPassword = string.Empty;
            }
            Properties.Settings.Default.Save();
        }

        #endregion

        #region Window Events

       


        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Startup Checks

        private void UpdateStatus(TextBlock icon, TextBlock text, bool ok)
        {
            if (ok)
            {
                icon.Text = "✅";
                icon.Foreground = Brushes.Gray;
                text.Foreground = Brushes.Gray;
            }
            else
            {
                icon.Text = "❌";
                icon.Foreground = Brushes.Gray;
                text.Foreground = Brushes.Gray;
            }
        }

        private bool HasActiveNetwork()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            return NetworkInterface.GetAllNetworkInterfaces()
                .Any(n =>
                    n.OperationalStatus == OperationalStatus.Up &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                    n.GetIPProperties().UnicastAddresses
                        .Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork));
        }



        private async Task<bool> RunStartupChecksAsync()
        {
            RecheckButton.Visibility = Visibility.Collapsed;
            SkipButton.Visibility = Visibility.Collapsed; // Hide initially

            IvmsIcon.Text = "⏳";
            SqlIcon.Text = "⏳";
            NetIcon.Text = "⏳";

            await Task.Delay(500);
            bool ivmsOk = Process.GetProcessesByName("iVMS-4200.Framework.S").Any();
            UpdateStatus(IvmsIcon, IvmsText, ivmsOk);

            await Task.Delay(500);
            bool sqlOk = Process.GetProcessesByName("sqlservr").Any();
            UpdateStatus(SqlIcon, SqlText, sqlOk);

            await Task.Delay(500);
            bool netOk = HasActiveNetwork();
            UpdateStatus(NetIcon, NetText, netOk);

            bool allOk = ivmsOk && sqlOk && netOk;

            if (!allOk)
            {
                RecheckButton.Visibility = Visibility.Visible;
                SkipButton.Visibility = Visibility.Visible; // Show Skip button
            }

            return allOk;
        }


        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            var users = UserService.Load();
            var user = users.FirstOrDefault(u => u.Username == UserName.Text);

            if (user != null)
            {
                new Home(user).Show();
                Close();
            }
            else
            {
                CustomMessageBox.Show("Invalid user for skip.");
            }
        }




        private async void RecheckButton_Click(object sender, RoutedEventArgs e)
        {
            bool passed = await RunStartupChecksAsync();

            if (passed)
            {
                var users = UserService.Load();
                var user = users.First(u => u.Username == UserName.Text);

                new Home(user).Show();
                Close();
            }
        }

        #endregion

        #region Seed Admin

        void SeedAdmin()
        {
            var users = UserService.Load();
            if (!users.Any())
            {
                users.Add(new Models.User
                {
                    Username = "admin",
                    FName = "PDC",
                    LName = "Administrator",
                    PasswordHash = UserService.Hash("PDC@Admin"),
                    Dashbord = true,
                    OderCheck = true,
                    Jobcard = true,
                    Customer = true,
                    Outsourcing = true,
                    Quotation = true,
                    Employee = true,
                    Attendance = true,
                    Payroll = true,
                    Paysheet = true,
                    UserManager = true,
                    Isadmin = true

                });
                UserService.Save(users);
            }
        }

        #endregion

        #region Login

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            // 🔒 Rapid Enter / double-click block
            if (_isLoggingIn) return;
            _isLoggingIn = true;

            try
            {
                var users = UserService.Load();

                var user = users.FirstOrDefault(u =>
                    u.Username == UserName.Text &&
                    u.PasswordHash == UserService.Hash(Password.Password));

                if (user == null)
                {
                    CustomMessageBox.Show("Invalid login");
                    Logininfo.IsEnabled = true;
                    return;
                }

                SaveCredentials();

                ((Button)sender).IsEnabled = false;
                Logininfo.IsEnabled = false;

                bool needStartupCheck =
                    Properties.Settings.Default.SendDailyReport ||
                    Properties.Settings.Default.SendAttendanceEmails;

                if (needStartupCheck)
                {
                    bool passed = await RunStartupChecksAsync();
                    IvmsVisibility.Visibility = Visibility.Visible;

                    if (!passed)
                    {
                        CustomMessageBox.Show(
                            "System check failed.\nFix the issues and click Recheck.",
                            "Startup Blocked",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        ((Button)sender).IsEnabled = true;
                        Logininfo.IsEnabled = true;
                        return; // ⛔ DO NOT open Home
                    }
                }

                // ✅ ONLY HERE Home opens
                new Home(user).Show();


                string basePath = Path.Combine(
     AppDomain.CurrentDomain.BaseDirectory,
     "Savers");

                AttendanceDatabase db =
                    new AttendanceDatabase(basePath);



                var hik = new HikvisionService(
                    "192.168.1.15",
                    "admin",
                    "priyanthaD@8");

                AttendanceSyncService sync =
                    new AttendanceSyncService(hik, db);

                sync.Start();



                // ✅ Open MiniWidgetWindow only if setting is enabled
                if (Properties.Settings.Default.MiniWidgetCheckBoxState)
                {
                    MiniWidgetWindow widget = new MiniWidgetWindow();
                    widget.Show();
                }

                Close();
            }
            finally
            {
                _isLoggingIn = false; // 🔓 Reset guard
            }
        }

        #endregion
    }
}