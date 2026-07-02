
using Google.Apis.PeopleService.v1;
using Hardcodet.Wpf.TaskbarNotification;
using PDC_System.Helpers;
using PDC_System.Models;
using PDC_System.Outsourcing;
using PDC_System.Payroll_Details;
using PDC_System.Paysheets;
using PDC_System.Settings;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PDC_System.Email;
using PDC_System.Services;

namespace PDC_System
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// Main window with navigation, Google account info, tray icon, and theme handling.
    /// </summary>
    /// 

    public partial class Home : Window
    {
        #region Fields


       

        private PeopleServiceService peopleService;
        private TaskbarIcon _trayIcon;
        private User _user;



        #endregion

        #region Constructor

        public Home(User user)
        {
            InitializeComponent();
           
            ThemeManager.ApplyTheme(this);
            Loaded += Home_Loaded;

            SetupTrayIcon();

            _user = user;
            UserNameTextBox.Text = string.IsNullOrWhiteSpace(_user.Username)
     ? ""
     : _user.Username.Substring(0, 1).ToUpper();

            PopupUserName.Text = _user.FName + " " + _user.LName;
            Adminitaterusername.Text = _user.Username;
            ApplyAccess();

            //LoadCalculateSettings();

            UpdateManager.StateChanged += UpdateManager_StateChanged;

            Loaded += async (s, e) =>
            {
                try
                {
                    if (Properties.Settings.Default.AutoUpdateEnabled)
                    {
                        await UpdateManager.CheckForUpdates();
                    }
                }
                catch
                {
                }
            };


     


        }



        private void UpdateManager_StateChanged(UpdateState state)
        {
            Dispatcher.Invoke(() =>
            {
                if (state == UpdateState.Downloaded)
                {
                    var result = CustomMessageBox.Show(
                        $"New update {UpdateManager.LatestVersion} is ready.\n\nInstall now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        UpdateManager.InstallUpdate();
                    }
                }
            });
        }




        void ApplyAccess()
        {
            Dashbord.Visibility = _user.Dashbord ? Visibility.Visible : Visibility.Collapsed;
            OderCheck.Visibility = _user.OderCheck ? Visibility.Visible : Visibility.Collapsed;
            Jobcard.Visibility = _user.Jobcard ? Visibility.Visible : Visibility.Collapsed;
            Customer.Visibility = _user.Customer ? Visibility.Visible : Visibility.Collapsed;
            Outsourcing.Visibility = _user.Outsourcing ? Visibility.Visible : Visibility.Collapsed;
            Quotation.Visibility = _user.Quotation ? Visibility.Visible : Visibility.Collapsed;
            Employee.Visibility = _user.Employee ? Visibility.Visible : Visibility.Collapsed;
            Attendance.Visibility = _user.Attendance ? Visibility.Visible : Visibility.Collapsed;
            Payroll.Visibility = _user.Payroll ? Visibility.Visible : Visibility.Collapsed;
            Paysheet.Visibility = _user.Paysheet ? Visibility.Visible : Visibility.Collapsed;
            UserManagerTAB.Visibility = _user.UserManager ? Visibility.Visible : Visibility.Collapsed;

        }


        #endregion



        #region Tray Icon

        private void SetupTrayIcon()
        {
            _trayIcon = new TaskbarIcon();
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Main.ico");
            _trayIcon.Icon = new Icon(iconPath);
            _trayIcon.ToolTipText = "PDC System";

            _trayIcon.TrayLeftMouseUp += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            var contextMenu = new ContextMenu();

            var menuOpen = new MenuItem { Header = "Open" };
            menuOpen.Click += (s, e) => ShowWindow();

            var menuSettings = new MenuItem { Header = "Backups" };
            menuSettings.Click += (s, e) => new BackupWindow().Show();

            var Settings = new MenuItem { Header = "Settings" };
            Settings.Click += (s, e) => new SettingsWindow().Show();

            var menuExit = new MenuItem { Header = "Exit" };
            menuExit.Click += (s, e) =>
            {
                _trayIcon.Dispose();
                Application.Current.Shutdown();
            };

            contextMenu.Items.Add(menuOpen);
            contextMenu.Items.Add(menuSettings);
            contextMenu.Items.Add(Settings);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(menuExit);

            _trayIcon.ContextMenu = contextMenu;
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _trayIcon.Dispose();
            base.OnClosing(e);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
                this.Hide();
        }

        #endregion

        #region Navigation

        private void Home_Loaded(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HomeUIWindow();
            LoadView(new HomeUIWindow());
        }

        private void OpenView1_Click(object sender, RoutedEventArgs e) => LoadView(new Customers());
        private void OpenView2_Click(object sender, RoutedEventArgs e) => LoadView(new Jobs());
        private void OpenView3_Click(object sender, RoutedEventArgs e) => LoadView(new QuotationWindow());
        private void OpenView4_Click(object sender, RoutedEventArgs e) => LoadView(new EmployeeWindow());
        private void OpenView5_Click(object sender, RoutedEventArgs e) => LoadView(new AttendanceWindow());
        //*private void OpenView6_Click(object sender, RoutedEventArgs e) => LoadView(new SalaryDetails());
        private void OpenView12_Click(object sender, RoutedEventArgs e) => LoadView(new PayrollDetailsWindow());
        private void OpenView13_Click(object sender, RoutedEventArgs e) => LoadView(new PaysheetWindow());
        private void OpenView8_Click(object sender, RoutedEventArgs e) => LoadView(new HomeUIWindow());
        private void OpenView9_Click(object sender, RoutedEventArgs e) => LoadView(new Oders());
        private void OpenView11_Click(object sender, RoutedEventArgs e) => LoadView(new OutsourcingWindow());
        private void OpenView14_Click(object sender, RoutedEventArgs e) => LoadView(new UserManagerControl());
        private void OpenView15_Click(object sender, RoutedEventArgs e) => LoadView(new EmailWindow());



        private void LoadView(UserControl view)
        {
            TranslateTransform trans = new TranslateTransform();
            view.RenderTransform = trans;
            view.Opacity = 0;

            DoubleAnimation slideAnim = new DoubleAnimation
            {
                From = 50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation fadeAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            MainContent.Content = view;

            trans.BeginAnimation(TranslateTransform.YProperty, slideAnim);
            view.BeginAnimation(UserControl.OpacityProperty, fadeAnim);
        }

        #endregion

        #region Window Control

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private bool _isMaximized = false;
        private double _previousLeft;
        private double _previousTop;
        private double _previousWidth;
        private double _previousHeight;

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (_isMaximized)
            {
                // Restore to previous size and position
                this.Left = _previousLeft;
                this.Top = _previousTop;
                this.Width = _previousWidth;
                this.Height = _previousHeight;
                _isMaximized = false;
            }
            else
            {
                // get before maximizing
                _previousLeft = this.Left;
                _previousTop = this.Top;
                _previousWidth = this.Width;
                _previousHeight = this.Height;

                // Get the working area (screen minus taskbar)
                var workingArea = SystemParameters.WorkArea;

                // Set window position and size to working area
                this.Left = workingArea.Left;
                this.Top = workingArea.Top;
                this.Width = workingArea.Width;
                this.Height = workingArea.Height;

                _isMaximized = true;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {

            Hide();
        }

        #endregion

        #region Settings Window

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            // Check if SettingsWindow is already open
            var existingWindow = Application.Current.Windows
                                    .OfType<SettingsWindow>()
                                    .FirstOrDefault();

            if (existingWindow != null)
            {
                // If already open, bring it to front
                existingWindow.Activate();
            }
            else
            {
                // If not open, create and show a new one
                var settingsWindow = new SettingsWindow();
                settingsWindow.GoogleAccountChanged += () =>
                {
                    // Refresh Home UI when Google account changes

                };
                settingsWindow.Show();
            }
        }


        #endregion


        #region About Window

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.ShowDialog();
        }

        #endregion


        #region Database and Attendance Calculation


        #endregion


        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            // Check if BackupWindow is already open
            var existingWindow = Application.Current.Windows
                                    .OfType<BackupWindow>()
                                    .FirstOrDefault();
            if (existingWindow != null)
            {
                // If already open, bring it to front
                existingWindow.Activate();
            }
            else
            {
                // If not open, create and show a new one
                var backupWindow = new BackupWindow();
                backupWindow.Show();
            }


        }

        private static void RestartApplication()
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });

            Application.Current.Shutdown();
        }


        private void Log_out (object sender, RoutedEventArgs e)
        {
            var result = CustomMessageBox.Show("Are you sure you want to log out?", "Confirm Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                RestartApplication();
            }
        }

     
        private void UserButton_Click(object sender, RoutedEventArgs e)
        {
            UserPopup.IsOpen = !UserPopup.IsOpen;
        }

        public void updateattendacecheck()
        {
           

            string DATE = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            check_status.Text = ($"Last Check: {DATE}");
        }

        public void updateattendacecheck_status(int local, int device)
        {
            attendacecheck_status.Text = $"Local = {local}  Device = {device}";
        }



        private Storyboard _rotationStoryboard;

        public void Rotation_start()
        {
            if (_rotationStoryboard != null)
                return; // Already running

            _rotationStoryboard = new Storyboard();

            DoubleAnimation rotate = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard.SetTarget(rotate, LoadingImage);
            Storyboard.SetTargetProperty(
                rotate,
                new PropertyPath("RenderTransform.Angle"));

            _rotationStoryboard.Children.Add(rotate);
            _rotationStoryboard.Begin();
        }

        public void Rotation_Stop()
        {
            if (_rotationStoryboard != null)
            {
                _rotationStoryboard.Stop();
                _rotationStoryboard = null;
            }
        }


    }
}
