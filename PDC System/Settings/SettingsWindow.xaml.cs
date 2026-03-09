using Google.Apis.PeopleService.v1;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using PDC_System.Helpers;
using PDC_System.Models;
using PDC_System.Properties;
using PDC_System.Services;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection; // NEW

namespace PDC_System.Settings
{
    /// <summary>
    /// Settings Window handles Google Sign-In, Startup toggle, and Theme selection.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        #region Fields



        private PeopleServiceService peopleService;
        // No JSON used anymore – kept only to avoid errors
        private string jsonFile = "";

        public event Action GoogleAccountChanged;
        private bool isLoaded = false;

        #endregion

        #region Constructor

        public SettingsWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            ThemeManager.ApplyTheme(this);
            // Startup checkbox
            chkStartup.IsChecked = StartupManager.IsInStartup();


            UpdateManager.ProgressChanged += UpdateProgress;
            UpdateManager.StateChanged += UpdateStateChanged;

            UpdateProgressBar.Value = UpdateManager.Progress;

            if (UpdateManager.State == UpdateState.Downloaded)
                InstallButton.IsEnabled = true;

            // set current version text in the status textbox
            try
            {
                var current = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
                UpdateStatusText.Text = $"Current: v{current}";
            }
            catch
            {
                UpdateStatusText.Text = "Current: unknown";
            }

            LoadSettings();
            isEnableStatus();
            LoadETF();
            LoadAttendance();
            LoadSystemSettings();
            isLoaded = true;
            // Apply theme when window loads
            ThemeManager.ApplyTheme(this);

            AutoUpdateCheckBox.IsChecked = Properties.Settings.Default.AutoUpdateEnabled;
        }

        #endregion


        #region UI Management

        private async void RefreshUI()
        {
            // Small delay to ensure Settings are saved
            await Task.Delay(100);


        }

        #endregion

        #region Startup Checkbox

        private void chkStartup_Checked(object sender, RoutedEventArgs e)
        {
            StartupManager.AddToStartup();
            Properties.Settings.Default.RunAtStartup = true;
            Properties.Settings.Default.Save();
        }

        private void chkStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupManager.RemoveFromStartup();
            Properties.Settings.Default.RunAtStartup = false;
            Properties.Settings.Default.Save();
        }

        #endregion

        #region Theme Management

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            switch (ThemeManager.CurrentTheme)
            {
                case ThemeMode.Light:
                    LightRadio.IsChecked = true;
                    break;
                case ThemeMode.Dark:
                    DarkRadio.IsChecked = true;
                    break;
                case ThemeMode.SystemDefault:
                    SystemRadio.IsChecked = true;
                    break;
            }
        }

        private void LightRadio_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.CurrentTheme = ThemeMode.Light;
            ThemeManager.UpdateAllWindows();
        }

        private void DarkRadio_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.CurrentTheme = ThemeMode.Dark;
            ThemeManager.UpdateAllWindows();
        }

        private void SystemRadio_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.CurrentTheme = ThemeMode.SystemDefault;
            ThemeManager.UpdateAllWindows();
        }

        #endregion

        #region Database Management



        private void LoadSettings()
        {
            TxtIP.Text = Properties.Settings.Default.DB_IP;
            TxtPort.Text = Properties.Settings.Default.DB_Port;
            TxtUser.Text = Properties.Settings.Default.DB_User;
            TxtPass.Password = Properties.Settings.Default.DB_Password;
            TxtDBName.Text = Properties.Settings.Default.DB_Name;
            TxtTable.Text = Properties.Settings.Default.DB_Table;

            // Load cached JSON data
            DataTable cachedData = LoadDataFromJson();
            if (cachedData != null)
            {




            }
        }

        private void SaveDataToJson(DataTable dt)
        {
            // Convert DataTable rows → List<FingerprintData>
            var list = new List<FingerprintData>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new FingerprintData
                {
                    EmployeeID = row["EmployeeID"].ToString(),
                    DateTime = Convert.ToDateTime(row["DateTime"])
                });
            }

            // Save to SQLite
            var db = new AttendanceDatabase(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savers"));
            db.InsertFingerprintData(list);
        }



        private DataTable LoadDataFromJson()
        {
            var db = new AttendanceDatabase(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savers"));
            var fp = db.GetFingerprintData();

            // convert to DataTable (same structure as before)
            DataTable dt = new DataTable();
            dt.Columns.Add("EmployeeID");
            dt.Columns.Add("DateTime", typeof(DateTime));

            foreach (var f in fp)
            {
                dt.Rows.Add(f.EmployeeID, f.DateTime);
            }

            return dt;
        }


        private void isEnableStatus()
        {
            if (Properties.Settings.Default.isEnable)
            {
                // When logged in / enabled
                SettingsDisable.IsEnabled = false;
                LoginBtn.Visibility = Visibility.Hidden;
                LogoutBtn.Visibility = Visibility.Visible;
            }
            else
            {
                // When logged out / disabled
                SettingsDisable.IsEnabled = true;
                LoginBtn.Visibility = Visibility.Visible;
                LogoutBtn.Visibility = Visibility.Hidden;
            }
        }


        private string GetConnectionString()
        {
            return $"Server={Properties.Settings.Default.DB_IP},{Properties.Settings.Default.DB_Port};" +
                   $"Database={Properties.Settings.Default.DB_Name};" +
                   $"User Id={Properties.Settings.Default.DB_User};" +
                   $"Password={Properties.Settings.Default.DB_Password};" +
                   "Encrypt=True;TrustServerCertificate=True;";
        }



        private string GetConnectionStringTemp()
        {
            return $"Server={TxtIP.Text},{TxtPort.Text};" +
                   $"Database={TxtDBName.Text};" +
                   $"User Id={TxtUser.Text};" +
                   $"Password={TxtPass.Password};" +
                   "Encrypt=True;TrustServerCertificate=True;";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1️⃣ Check if all required fields have value
            if (string.IsNullOrWhiteSpace(TxtIP.Text) ||
                string.IsNullOrWhiteSpace(TxtPort.Text) ||
                string.IsNullOrWhiteSpace(TxtUser.Text) ||
                string.IsNullOrWhiteSpace(TxtPass.Password) ||
                string.IsNullOrWhiteSpace(TxtDBName.Text) ||
                string.IsNullOrWhiteSpace(TxtTable.Text))
            {
                CustomMessageBox.Show("⚠ Please fill all fields before saving!");
                return;
            }

            // 2️⃣ Test SQL Server connection using TEMP values (not Settings)
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionStringTemp()))
                {
                    conn.Open();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("❌ Connection failed: " + ex.Message);
                return; // Stop saving if connection fails
            }

            // 3️⃣ Save settings (only if connection successful)
            Properties.Settings.Default.DB_IP = TxtIP.Text;
            Properties.Settings.Default.DB_Port = TxtPort.Text;
            Properties.Settings.Default.DB_User = TxtUser.Text;
            Properties.Settings.Default.DB_Password = TxtPass.Password;
            Properties.Settings.Default.DB_Name = TxtDBName.Text;
            Properties.Settings.Default.DB_Table = TxtTable.Text;
            Properties.Settings.Default.isEnable = true;
            Properties.Settings.Default.Save();
            isEnableStatus();

            CustomMessageBox.Show("✅ Settings saved and connection verified!");


            isEnableStatus();
        }



        public void BtnLoad_Click()
        {
            // ✅ Check if all required settings have value
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.DB_IP) ||
                string.IsNullOrWhiteSpace(Properties.Settings.Default.DB_Port) ||
                string.IsNullOrWhiteSpace(Properties.Settings.Default.DB_User) ||
                string.IsNullOrWhiteSpace(Properties.Settings.Default.DB_Password) ||
                string.IsNullOrWhiteSpace(Properties.Settings.Default.DB_Name) ||
                string.IsNullOrWhiteSpace(Properties.Settings.Default.DB_Table))
            {
                NotificationHelper.ShowNotification("PDC System!", "⚠ Database settings are empty! Please save your settings first.");
                return; // stop loading
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    string query = $"SELECT * FROM {Properties.Settings.Default.DB_Table}";
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Save to JSON for next time
                    SaveDataToJson(dt);

                    DaliyAttendance.CheckTodayAttendance();
                    EmployeeDailyAttendance.CheckTodayAttendanceAsync();
                    NotificationHelper.ShowNotification("PDC System!", "IVMS Calculate Complete!");

                    NotificationHelper.ShowNotification("PDC System!", "✅ Data loaded successfully!");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error loading data: " + ex.Message);
            }
        }


        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();

            TxtIP.Clear();
            TxtPort.Clear();
            TxtUser.Clear();
            TxtPass.Clear();
            TxtDBName.Clear();
            TxtTable.Clear();
            Properties.Settings.Default.isEnable = false;
            Properties.Settings.Default.Save();
            isEnableStatus();


            CustomMessageBox.Show("🔒 Logged out, settings cleared.");
        }

        private void TxtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // allow only 0-9
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void TxtNumber_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // allow Backspace, Delete, Tab, Left, Right
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Tab || e.Key == Key.Left || e.Key == Key.Right)
            {
                e.Handled = false;
            }
        }


        private void TxtIP_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // ✅ Allow digits and dot
            if (!char.IsDigit(e.Text, 0) && e.Text != ".")
            {
                e.Handled = true; // Block
                return;
            }

            string currentText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            // ❌ Prevent multiple consecutive dots
            if (currentText.Contains(".."))
            {
                e.Handled = true;
                return;
            }

            // ❌ Prevent more than 3 digits in each block
            string[] blocks = currentText.Split('.');
            foreach (string block in blocks)
            {
                if (block.Length > 3)
                {
                    e.Handled = true;
                    return;
                }

                // ❌ Prevent values > 255
                if (int.TryParse(block, out int value) && value > 255)
                {
                    e.Handled = true;
                    return;
                }
            }

            // ❌ Prevent more than 4 blocks
            if (blocks.Length > 4)
            {
                e.Handled = true;
            }
        }









        #endregion

        #region Close Button

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        #endregion


        #region Update Checking


        void UpdateProgress(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProgressBar.Value = progress;
                // show percent in status textbox while downloading
                if (UpdateManager.State == UpdateState.Downloading && UpdateManager.LatestVersion != null)
                {
                    UpdateStatusText.Text = $"Downloading v{UpdateManager.LatestVersion}... {Math.Round(progress * 100)}%";
                }
            });
        }

        void UpdateStateChanged(UpdateState state)
        {
            Dispatcher.Invoke(() =>
            {
                switch (state)
                {
                    case UpdateState.Checking:
                        UpdateStatusText.Text = "Checking for updates...";
                        CheckUpdateButton.IsEnabled = false;
                        InstallButton.IsEnabled = false;
                        break;

                    case UpdateState.Available:
                        // Use LatestVersion (set by UpdateManager)
                        if (!string.IsNullOrEmpty(UpdateManager.LatestVersion))
                            UpdateStatusText.Text = $"Found: v{UpdateManager.LatestVersion}";
                        else
                            UpdateStatusText.Text = "Update available";
                        // still disable install until download completes
                        InstallButton.IsEnabled = false;
                        break;

                    case UpdateState.Downloading:
                        if (!string.IsNullOrEmpty(UpdateManager.LatestVersion))
                            UpdateStatusText.Text = $"Downloading v{UpdateManager.LatestVersion}...";

                        else
                            UpdateStatusText.Text = "Downloading...";
                        InstallButton.IsEnabled = false;
                        UpdateProgressBar.Visibility = Visibility.Visible;

                        break;

                    case UpdateState.Downloaded:
                        if (!string.IsNullOrEmpty(UpdateManager.LatestVersion))
                            UpdateStatusText.Text = $"Available: v{UpdateManager.LatestVersion}";
                        else
                            UpdateStatusText.Text = "Update ready";

                        InstallButton.IsEnabled = true;
                        InstallButton.Visibility = Visibility.Visible;

                        CheckUpdateButton.IsEnabled = true;
                        break;


                    case UpdateState.None:
                    default:
                        // no update or error
                        var current = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
                        UpdateStatusText.Text = $"Up to date (v{current})";
                        CheckUpdateButton.IsEnabled = true;
                        InstallButton.IsEnabled = false;
                        break;
                }
            });
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            // disable to prevent double clicks; state handler will re-enable
            CheckUpdateButton.IsEnabled = false;
            try
            {
                await UpdateManager.CheckForUpdates();
            }
            catch (Exception ex)
            {
                // show friendly error and re-enable
                UpdateStatusText.Text = "Update check failed";
                CheckUpdateButton.IsEnabled = true;
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateManager.InstallUpdate();
        }

        private void AutoUpdateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoUpdateEnabled = true;
            Properties.Settings.Default.Save();
       
        }

        private void AutoUpdateCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoUpdateEnabled = false;
            Properties.Settings.Default.Save();
        }




        #endregion


        #region ETF


        private void LoadETF()
        {
            EPFEmployee.Text = Properties.Settings.Default.EPFEmployee.ToString("0.##", CultureInfo.InvariantCulture);
            EPFEmployer.Text = Properties.Settings.Default.EPFEmployer.ToString("0.##", CultureInfo.InvariantCulture);
            btnSave.Visibility = Visibility.Collapsed;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isLoaded) return;

            if (decimal.TryParse(EPFEmployee.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal empVal) &&
                decimal.TryParse(EPFEmployer.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal emrVal))
            {
                // show Save button only if values changed
                if (empVal != Properties.Settings.Default.EPFEmployee ||
                    emrVal != Properties.Settings.Default.EPFEmployer)
                {
                    btnSave.Visibility = Visibility.Visible;
                }
                else
                {
                    btnSave.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // invalid number input
                btnSave.Visibility = Visibility.Collapsed;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(EPFEmployee.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal empVal) &&
                decimal.TryParse(EPFEmployer.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal emrVal))
            {
                Properties.Settings.Default.EPFEmployee = empVal;
                Properties.Settings.Default.EPFEmployer = emrVal;
                Properties.Settings.Default.Save();
                UpdateAllEPFRecords();

                btnSave.Visibility = Visibility.Collapsed;

                CustomMessageBox.Show("Settings saved successfully ✅", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                CustomMessageBox.Show("Please enter valid decimal values ⚠️", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void UpdateAllEPFRecords()
        {
            string file = "Savers/EPF.json";

            if (!File.Exists(file))
                return;

            var list = JsonConvert.DeserializeObject<List<EPF>>(File.ReadAllText(file));
            if (list == null) return;

            decimal empPercent = Properties.Settings.Default.EPFEmployee;
            decimal emrPercent = Properties.Settings.Default.EPFEmployer;

            foreach (var e in list)
            {
                e.EmployeeAmount = (e.BasicSalary * empPercent) / 100;
                e.EmployerAmount = (e.BasicSalary * emrPercent) / 100;
                e.Total = e.EmployeeAmount + e.EmployerAmount;
            }

            File.WriteAllText(file, JsonConvert.SerializeObject(list, Formatting.Indented));
        }


        #endregion

        #region Attendance
        private void LoadAttendance()
        {
            // Load saved decimal values into TextBoxes
            OT_RoundMinutes.Text = Properties.Settings.Default.OT_RoundMinutes.ToString("0.##", CultureInfo.InvariantCulture);
            TimerIntervalMinutes.Text = Properties.Settings.Default.TimerIntervalMinutes.ToString("0.##", CultureInfo.InvariantCulture);
            chkSendDailyReport.IsChecked = Properties.Settings.Default.SendDailyReport;
            SendAttendanceEmails.IsChecked = Properties.Settings.Default.SendAttendanceEmails;

            // Hide save button initially
            AttendancebtnSave.Visibility = Visibility.Collapsed;

            // Attach event handlers to detect text changes
            OT_RoundMinutes.TextChanged += AttendanceSettingChanged;
            TimerIntervalMinutes.TextChanged += AttendanceSettingChanged;
            chkSendDailyReport.Checked += AttendanceCheckBoxChanged;
            chkSendDailyReport.Unchecked += AttendanceCheckBoxChanged;
            SendAttendanceEmails.Checked += AttendanceCheckBoxChanged;
            SendAttendanceEmails.Unchecked += AttendanceCheckBoxChanged;
        }

        // Show Save button only when values actually change
        private void AttendanceSettingChanged(object sender, TextChangedEventArgs e)
        {
            bool otChanged = decimal.TryParse(OT_RoundMinutes.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal otValue) &&
                             otValue != Properties.Settings.Default.OT_RoundMinutes;

            bool timerChanged = decimal.TryParse(TimerIntervalMinutes.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal timerValue) &&
                                timerValue != Properties.Settings.Default.TimerIntervalMinutes;

            AttendancebtnSave.Visibility = (otChanged || timerChanged) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AttendanceCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if ((sender == chkSendDailyReport && chkSendDailyReport.IsChecked != Properties.Settings.Default.SendDailyReport) ||
                (sender == SendAttendanceEmails && SendAttendanceEmails.IsChecked != Properties.Settings.Default.SendAttendanceEmails))
            {
                AttendancebtnSave.Visibility = Visibility.Visible;
            }
            else
            {
                AttendancebtnSave.Visibility = Visibility.Collapsed;
            }
        }


        private void AttendancebtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(OT_RoundMinutes.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal otValue) &&
                decimal.TryParse(TimerIntervalMinutes.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal timerValue))
            {
                // Save the settings
                Properties.Settings.Default.OT_RoundMinutes = (int)Math.Round(otValue);
                Properties.Settings.Default.TimerIntervalMinutes = (int)Math.Round(timerValue);
                Properties.Settings.Default.SendDailyReport = chkSendDailyReport.IsChecked == true;
                Properties.Settings.Default.SendAttendanceEmails = SendAttendanceEmails.IsChecked == true;
                Properties.Settings.Default.Save();

                // Hide the save button
                AttendancebtnSave.Visibility = Visibility.Collapsed;

                CustomMessageBox.Show("Attendance settings saved successfully ✅", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                CustomMessageBox.Show("Please enter valid decimal values ⚠️", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion

        #region System Settings

        private void LoadSystemSettings()
        {
            SendEmployeeEditEmails.IsChecked = Properties.Settings.Default.SendEmployeeEditEmails;
            SendEmployeeAttendanceEditEmails.IsChecked = Properties.Settings.Default.SendEmployeeAttendanceEditEmails;

            // Load System Email and Password from settings
            SystemEmail.Text = Properties.Settings.Default.SystemAppEmail;
            SystemEmailPassword.Password = Properties.Settings.Default.SystemAppPassword;

            // Load PDC Email and Password from settings
            PDCEmail.Text = Properties.Settings.Default.PDCEmail;
            PDCEmailPassword.Password = Properties.Settings.Default.PDCAppPassword;

            SystemSettingsbtnSave.Visibility = Visibility.Collapsed;

            SendEmployeeEditEmails.Checked += SystemSettingsCheckBoxChanged;
            SendEmployeeEditEmails.Unchecked += SystemSettingsCheckBoxChanged;
            SendEmployeeAttendanceEditEmails.Checked += SystemSettingsCheckBoxChanged;
            SendEmployeeAttendanceEditEmails.Unchecked += SystemSettingsCheckBoxChanged;

            // Detect changes in email and password fields
            SystemEmail.TextChanged += SystemSettingsTextChanged;
            SystemEmailPassword.PasswordChanged += SystemSettingsPasswordChanged;

            // Detect changes in PDC email and password fields
            PDCEmail.TextChanged += SystemSettingsTextChanged;
            PDCEmailPassword.PasswordChanged += SystemSettingsPasswordChanged;
        }

        private void SystemSettingsCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            if ((SendEmployeeEditEmails.IsChecked != Properties.Settings.Default.SendEmployeeEditEmails) ||
                (SendEmployeeAttendanceEditEmails.IsChecked != Properties.Settings.Default.SendEmployeeAttendanceEditEmails))
            {
                SystemSettingsbtnSave.Visibility = Visibility.Visible;
            }
            else
            {
                SystemSettingsbtnSave.Visibility = Visibility.Collapsed;
            }
        }

        private void SystemSettingsbtnSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SendEmployeeEditEmails = SendEmployeeEditEmails.IsChecked == true;
            Properties.Settings.Default.SendEmployeeAttendanceEditEmails = SendEmployeeAttendanceEditEmails.IsChecked == true;

            // Save System Email and Password
            Properties.Settings.Default.SystemAppEmail = SystemEmail.Text;
            Properties.Settings.Default.SystemAppPassword = SystemEmailPassword.Password;

            // Save PDC Email and Password
            Properties.Settings.Default.PDCEmail = PDCEmail.Text;
            Properties.Settings.Default.PDCAppPassword = PDCEmailPassword.Password;

            Properties.Settings.Default.Save();

            SystemSettingsbtnSave.Visibility = Visibility.Collapsed;

            CustomMessageBox.Show("System settings saved successfully ✅", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SystemSettingsTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isLoaded) return;
            SystemSettingsbtnSave.Visibility = Visibility.Visible;
        }

        private void SystemSettingsPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            SystemSettingsbtnSave.Visibility = Visibility.Visible;
        }
        #endregion


    }
}