using Microsoft.Win32;
using Org.BouncyCastle.Asn1.Cms;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;
using System.Diagnostics;
using XamlAnimatedGif;

using System.Windows.Media.Imaging;

namespace PDC_System
{
    public partial class EmployeeAddData : Window
    {
        public Employee Employee { get; private set; }
        public ImageInfo imageInfo { get; private set; }

        private Employee originalEmployee;


        private string customFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ImageImports");

        public EmployeeAddData()   // ADD NEW EMPLOYEE
        {
            InitializeComponent();
            LoadCities();
            LoadTimeDropdowns();
            StartGifAnimation();
              Employee = new Employee();   // empty employee



        
        }



        private async void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
                string gifPath = System.IO.Path.Combine(baseFolder, "Assets", "Fingerprint_biometric_scan.gif");
                var gifUri = new Uri(gifPath, UriKind.Absolute);

                Dispatcher.Invoke(() =>
                {
                    AnimationBehavior.SetSourceUri(MyGifImage, gifUri);
                    // Don't start the animation immediately - stop it initially
                    AnimationBehavior.SetRepeatBehavior(MyGifImage, System.Windows.Media.Animation.RepeatBehavior.Forever);
                    StopGifAnimation();
                });
            });
        }


        private void StartGifAnimation()
        {
            // AnimationBehavior does not have SetIsPaused, so use AutoStart property
            AnimationBehavior.SetAutoStart(MyGifImage, true);
        }

        private void StopGifAnimation()
        {
            // AnimationBehavior does not have SetIsPaused, so use AutoStart property
            AnimationBehavior.SetAutoStart(MyGifImage, false);
        }


        private void Finger1_Click(object sender, RoutedEventArgs e)
        {
            // Start the GIF animation when the button is clicked
            StartGifAnimation();
        }   
        private void Finger2_Click(object sender, RoutedEventArgs e)
        {
            // Start the GIF animation when the button is clicked
            StartGifAnimation();
        }   




        public EmployeeAddData(Employee existingEmployee)
        {
            InitializeComponent();
            LoadCities();
            LoadTimeDropdowns();

            // 👉 Use the existing employee data
            Employee = existingEmployee;

            // Load basic text fields
            EmployeeNameTextBox.Text = Employee.Name;
            EmployeeIDTextBox.Text = Employee.EmployeeId;
            EmployeeEmailTextBox.Text = Employee.EmployeeEmail;
            jrole.Text = Employee.jobrole;
            Address1.Text = Employee.Address1;
            Address2.Text = Employee.Address2;
            city.Text = Employee.City;
            province.Text = Employee.Province;
            contact1.Text = Employee.Contactn1;
            contact2.Text = Employee.Contactn2;
            NatId.Text = Employee.NID;
            birthday.SelectedDate = Employee.Birthday;
            Department.Text = Employee.Department;


            CountOffDays.IsChecked = Employee.CountOffDays;

            Mon.IsChecked = Employee.Monday;
            Tue.IsChecked = Employee.Tuesday;
            Wen.IsChecked = Employee.Wednesday;
            The.IsChecked = Employee.Thursday;
            Fri.IsChecked = Employee.Friday;
            Sat.IsChecked = Employee.Saturday;
            Sun.IsChecked = Employee.Sunday;

            EmployeeSalaryTextBox.Text = Employee.Salary.ToString("F2");
            EmployeeBasicSalaryTextBox.Text = Employee.BSalary.ToString("F2");
            EmployeeOTTextBox.Text = Employee.OvertimeAmount.ToString("F2");
            EmployeeDOTTextBox.Text = Employee.DoubleOvertimeAmount.ToString("F2");
            EmployeeAbsentTextBox.Text = Employee.AbesentAmount.ToString("F2");
            NoPayTextBox.Text = Employee.Nopay.ToString("F2");

            // Times
            CbCheckIn_Hour.SelectedItem = Employee.CheckIn.Hours.ToString("00");
            CbCheckIn_Minute.SelectedItem = Employee.CheckIn.Minutes.ToString("00");

            CbCheckOut_Hour.SelectedItem = Employee.CheckOut.Hours.ToString("00");
            CbCheckOut_Minute.SelectedItem = Employee.CheckOut.Minutes.ToString("00");

            CbSatCheckIn_Hour.SelectedItem = Employee.SaturdayCheckIn.Hours.ToString("00");
            CbSatCheckIn_Minute.SelectedItem = Employee.SaturdayCheckIn.Minutes.ToString("00");

            CbSatCheckOut_Hour.SelectedItem = Employee.SaturdayCheckOut.Hours.ToString("00");
            CbSatCheckOut_Minute.SelectedItem = Employee.SaturdayCheckOut.Minutes.ToString("00");



            // store original copy (deep copy)
            originalEmployee = new Employee
            {
                EmployeeId = existingEmployee.EmployeeId,
                Name = existingEmployee.Name,
                EmployeeEmail = existingEmployee.EmployeeEmail,
                jobrole = existingEmployee.jobrole,
                Address1 = existingEmployee.Address1,
                Address2 = existingEmployee.Address2,
                City = existingEmployee.City,
                Province = existingEmployee.Province,
                Contactn1 = existingEmployee.Contactn1,
                Contactn2 = existingEmployee.Contactn2,
                NID = existingEmployee.NID,
                Birthday = existingEmployee.Birthday,
                Department = existingEmployee.Department,

                Salary = existingEmployee.Salary,
                BSalary = existingEmployee.BSalary,
                OvertimeAmount = existingEmployee.OvertimeAmount,
                DoubleOvertimeAmount = existingEmployee.DoubleOvertimeAmount,
                AbesentAmount = existingEmployee.AbesentAmount,
                Nopay = existingEmployee.Nopay,
                CheckIn = existingEmployee.CheckIn,
                CheckOut = existingEmployee.CheckOut,
                SaturdayCheckIn = existingEmployee.SaturdayCheckIn,
                SaturdayCheckOut = existingEmployee.SaturdayCheckOut,


                CountOffDays = existingEmployee.CountOffDays,
                Monday = existingEmployee.Monday,
                Tuesday = existingEmployee.Tuesday,
                Wednesday = existingEmployee.Wednesday,
                Thursday = existingEmployee.Thursday,
                Friday = existingEmployee.Friday,
                Saturday = existingEmployee.Saturday,
                Sunday = existingEmployee.Sunday,
                OriginalPath = existingEmployee.OriginalPath,
                SavedLocation = existingEmployee.SavedLocation
            };





           
        }






        private void adress_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // You might want to add event logic here or remove this method if not needed.
        }

    



        public class ImageInfo
        {
            public string OriginalPath { get; set; }
            public string SavedLocation { get; set; }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate Name
            if (string.IsNullOrWhiteSpace(EmployeeNameTextBox.Text))
            {

                CustomMessageBox.Show("Please enter a valid Employee Name.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate ID
            if (string.IsNullOrWhiteSpace(EmployeeIDTextBox.Text))
            {
                CustomMessageBox.Show("Please enter a valid Employee ID.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate Salary
            if (!decimal.TryParse(EmployeeSalaryTextBox.Text.Trim(), out decimal employeeS) || employeeS < 0)
            {
                CustomMessageBox.Show("Please enter a valid positive number for Salary.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate Salary
            if (!decimal.TryParse(EmployeeBasicSalaryTextBox.Text.Trim(), out decimal employeeBS) || employeeBS < 0)
            {
                CustomMessageBox.Show("Please enter a valid positive number for Basic Salary.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            // Validate OT
            if (!decimal.TryParse(EmployeeOTTextBox.Text, out decimal employeeOT) || employeeOT < 0)
            {
                CustomMessageBox.Show("Please enter a valid positive number for Overtime Hours.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate DOT
            if (!decimal.TryParse(EmployeeDOTTextBox.Text, out decimal employeeDOT) || employeeDOT < 0)
            {
                CustomMessageBox.Show("Please enter a valid positive number for Double Overtime Hours.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate Absent Days
            if (!decimal.TryParse(EmployeeAbsentTextBox.Text, out decimal employeeAbsent) || employeeAbsent < 0)
            {
                CustomMessageBox.Show("Please enter a valid positive number for Absent.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Validate Absent Days
            if (!decimal.TryParse(NoPayTextBox.Text, out decimal NoPay) || NoPay < 0)
            {
                CustomMessageBox.Show("Please enter a valid positive number for NoPay.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Week Days Check-in
            if (CbCheckIn_Hour.SelectedItem == null || CbCheckIn_Minute.SelectedItem == null)
            {
                CustomMessageBox.Show("Please select Weekday check-in time.",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TimeSpan WeekDaysCheckin = new TimeSpan(
                int.Parse(CbCheckIn_Hour.SelectedItem.ToString()),
                int.Parse(CbCheckIn_Minute.SelectedItem.ToString()),
                0);


            // Week Days Check-out
            if (CbCheckOut_Hour.SelectedItem == null || CbCheckOut_Minute.SelectedItem == null)
            {
                CustomMessageBox.Show("Please select Weekday check-out time.",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TimeSpan WeekDaysCheckout = new TimeSpan(
                int.Parse(CbCheckOut_Hour.SelectedItem.ToString()),
                int.Parse(CbCheckOut_Minute.SelectedItem.ToString()),
                0);


            // Saturday Check-in
            if (CbSatCheckIn_Hour.SelectedItem == null || CbSatCheckIn_Minute.SelectedItem == null)
            {
                CustomMessageBox.Show("Please select Saturday check-in time.",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TimeSpan SatCheckin = new TimeSpan(
                int.Parse(CbSatCheckIn_Hour.SelectedItem.ToString()),
                int.Parse(CbSatCheckIn_Minute.SelectedItem.ToString()),
                0);


            // Saturday Check-out
            if (CbSatCheckOut_Hour.SelectedItem == null || CbSatCheckOut_Minute.SelectedItem == null)
            {
                CustomMessageBox.Show("Please select Saturday check-out time.",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TimeSpan SatCheckOut = new TimeSpan(
                int.Parse(CbSatCheckOut_Hour.SelectedItem.ToString()),
                int.Parse(CbSatCheckOut_Minute.SelectedItem.ToString()),
                0);


            // ✅ Validate Email (allow empty, but validate format if provided)
            string email = EmployeeEmailTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                CustomMessageBox.Show("Please enter a valid email address.", "Invalid Email", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmployeeEmailTextBox.Focus();
                return; // Stop saving
            }



            // Create Employee Object
            Employee = new Employee
            {
               
                EmployeeId = EmployeeIDTextBox.Text.Trim(),
                Name = EmployeeNameTextBox.Text.Trim(),
                EmployeeEmail = EmployeeEmailTextBox.Text.Trim(),
                jobrole = jrole.Text.Trim(),
                Address1 = Address1.Text.Trim(),
                Address2 = Address2.Text.Trim(),
                City = city.Text.Trim(),
                Province = province.Text.Trim(),
                Contactn1 = contact1.Text.Trim(),
                Contactn2 = contact2.Text.Trim(),
                NID = NatId.Text.Trim(),
                Birthday = birthday.SelectedDate,
                Department = Department.Text.Trim(),

                CountOffDays = (CountOffDays.IsChecked == true),

                Monday = (Mon.IsChecked == true),
                Tuesday = (Tue.IsChecked == true),
                Wednesday = (Wen.IsChecked == true),
                Thursday = (The.IsChecked == true),
                Friday = (Fri.IsChecked == true),
                Saturday = (Sat.IsChecked == true),
                Sunday = (Sun.IsChecked == true),

                Nopay = NoPay,
                CheckIn = WeekDaysCheckin,
                CheckOut = WeekDaysCheckout,
                SaturdayCheckIn = SatCheckin,
                SaturdayCheckOut = SatCheckOut,

                BSalary = employeeBS,
                Salary = employeeS,
                OvertimeAmount = employeeOT,
                DoubleOvertimeAmount = employeeDOT,
                AbesentAmount = employeeAbsent,
            };

            // Check if imageInfo is null before using it
            if (imageInfo != null)
            {
                Employee.OriginalPath = imageInfo.OriginalPath;
                Employee.SavedLocation = imageInfo.SavedLocation;
            }
            else
            {
                // If no image is provided, leave these properties blank or set them to default values
                Employee.OriginalPath = string.Empty; // Or you can set it to null
                Employee.SavedLocation = string.Empty; // Or you can set it to null
            }

            // Update UI
            EmployeeSalaryTextBox.Text = employeeS.ToString("F2");

            DialogResult = true;


            // ----------------------------
            // SHOW CHANGED FIELDS MESSAGE
            // ----------------------------
            if (originalEmployee != null) // only for edit mode
            {
                string changes = "Updated Successfully!\n\nChanged Fields:\n";

                void CheckChange(string fieldName, object oldValue, object newValue)
                {
                    if (!Equals(oldValue, newValue))
                    {
                        changes += $"{fieldName}: {oldValue} → {newValue}\n";
                    }
                }

                CheckChange("Employee ID", originalEmployee.EmployeeId, Employee.EmployeeId);
                CheckChange("Name", originalEmployee.Name, Employee.Name);
                CheckChange("Email", originalEmployee.EmployeeEmail, Employee.EmployeeEmail);
                CheckChange("Job Role", originalEmployee.jobrole, Employee.jobrole);
                CheckChange("Address 1", originalEmployee.Address1, Employee.Address1);
                CheckChange("Address 2", originalEmployee.Address2, Employee.Address2);
                CheckChange("City", originalEmployee.City, Employee.City);
                CheckChange("Province", originalEmployee.Province, Employee.Province);
                CheckChange("Contact 1", originalEmployee.Contactn1, Employee.Contactn1);
                CheckChange("Contact 2", originalEmployee.Contactn2, Employee.Contactn2);
                CheckChange("NIC", originalEmployee.NID, Employee.NID);
                CheckChange("Birthday", originalEmployee.Birthday?.Date, Employee.Birthday?.Date);

                CheckChange("Department", originalEmployee.Department, Employee.Department);




                CheckChange("Salary", originalEmployee.Salary, Employee.Salary);
                CheckChange("Basic Salary", originalEmployee.BSalary, Employee.BSalary);
                CheckChange("OT Amount", originalEmployee.OvertimeAmount, Employee.OvertimeAmount);
                CheckChange("Double OT", originalEmployee.DoubleOvertimeAmount, Employee.DoubleOvertimeAmount);
                CheckChange("Absent Amount", originalEmployee.AbesentAmount, Employee.AbesentAmount);
                CheckChange("Nopay", originalEmployee.Nopay, Employee.Nopay);

                CheckChange("Check-in", originalEmployee.CheckIn, Employee.CheckIn);
                CheckChange("Check-out", originalEmployee.CheckOut, Employee.CheckOut);
                CheckChange("Saturday Check-in", originalEmployee.SaturdayCheckIn, Employee.SaturdayCheckIn);
                CheckChange("Saturday Check-out", originalEmployee.SaturdayCheckOut, Employee.SaturdayCheckOut);

                // Working days
                CheckChange("Works Monday", originalEmployee.Monday, Employee.Monday);
                CheckChange("Works Tuesday", originalEmployee.Tuesday, Employee.Tuesday);
                CheckChange("Works Wednesday", originalEmployee.Wednesday, Employee.Wednesday);
                CheckChange("Works Thursday", originalEmployee.Thursday, Employee.Thursday);
                CheckChange("Works Friday", originalEmployee.Friday, Employee.Friday);
                CheckChange("Works Saturday", originalEmployee.Saturday, Employee.Saturday);
                CheckChange("Works Sunday", originalEmployee.Sunday, Employee.Sunday);

                // Detect image changes
                CheckChange("Profile Image", originalEmployee.SavedLocation, Employee.SavedLocation);

                CustomMessageBox.Show(changes, "Employee Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                NotificationHelper.ShowNotification("PDC System!", changes);
            }

        }


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



        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Check if the input is a number using a regular expression
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numeric input (0-9) and limit to 10 characters
            if (!char.IsDigit(e.Text, 0) || ((sender as TextBox).Text.Length >= 10))
            {
                e.Handled = true;  // Prevent the input
            }
        }

        private bool vEntered = false;
        private void NatId_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow "V" only once
            if (e.Text == "V" && !vEntered)
            {
                vEntered = true;
            }
            // Block non-numeric characters and ensure the length doesn't exceed 10 characters
            else if ((!char.IsDigit(e.Text, 0) && e.Text != "V") || (NatId.Text.Length >= 12))
            {
                e.Handled = true;
            }
        }

        private void PreviewfloteTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string currentText = textBox?.Text ?? string.Empty;

            // Allow only numeric input (0-9) or a single decimal point
            if (!char.IsDigit(e.Text, 0) && e.Text != ".")
            {
                e.Handled = true;  // Prevent the input
            }
            else
            {
                // Allow only one decimal point
                if (e.Text == "." && currentText.Contains("."))
                {
                    e.Handled = true;  // Prevent input if there's already a decimal point
                }

                // Limit the length to 10 characters including the decimal point
                if (currentText.Length >= 10)
                {
                    e.Handled = true;  // Prevent input if length exceeds 10
                }
            }
        }



        private void TimeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numbers (0-9)
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }


        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                string text = textBox.Text;

                // If the text is not empty and is a valid number
                if (!string.IsNullOrWhiteSpace(text) && decimal.TryParse(text, out decimal result))
                {
                    // Format the number to 2 decimal places (e.g., 500 to 500.00)
                    textBox.Text = result.ToString("F2");
                }
            }
        }

        private void LoadCities()
        {
            string[] cities = new string[]
            {
        "Agalawatta", "Agrapathana", "Akuressa", "Akurana", "Akkaraipattu", "Alawatuwala",
        "Alawwa", "Aluthgama", "Ambalangoda", "Ambalantota", "Ampara", "Ampara City",
        "Anamaduwa", "Angoda", "Anuradhapura", "Anuradhapura City", "Aranayaka",
        "Attanagalla", "Athurugiriya", "Avissawella", "Baddegama", "Badulla", "Badulla City",
        "Balangoda", "Bandaragama", "Bandarawela", "Battaramulla", "Batticaloa", "Batticaloa City",
        "Beliatta", "Belihuloya", "Bentota", "Beruwala", "Bibile", "Bingiriya", "Biyagama",
        "Bokkawala", "Borella", "Boralesgamuwa", "Bulathkohupitiya", "Buttala", "Chavakacheri",
        "Chilaw", "Colombo", "Colombo 1", "Colombo 2", "Colombo 3", "Colombo 4", "Colombo 5",
        "Colombo 6", "Colombo 7", "Colombo 8", "Colombo 9", "Colombo 10", "Colombo 11",
        "Colombo 12", "Colombo 13", "Colombo 14", "Colombo 15", "Damana", "Dambulla",
        "Dehiattakandiya", "Dehiwala", "Delgoda", "Deniyaya", "Deraniyagala", "Dikoya", "Dikwella",
        "Dimbulagala", "Divulapitiya", "Dodanduwa", "Dompe", "Eheliyagoda", "Ella", "Embilipitiya",
        "Eravur", "Eratne", "Galagedara", "Galaha", "Galenbindunuwewa", "Galle", "Galle City",
        "Galnewa", "Gampaha", "Gampola", "Gampolawela", "Ganemulla", "Gelioya", "Giribawa",
        "Giritale", "Godakawela", "Gonapola Junction", "Habaraduwa", "Habarana", "Haggala",
        "Haldummulla", "Hali Ela", "Haliela", "Hambantota", "Hambantota City", "Hanwella",
        "Haputale", "Harispattuwa", "Hatton", "Hettipola", "Hikkaduwa", "Hingurakgoda", "Hiriyala",
        "Horana", "Horowpathana", "Horton Plains", "Hulftsdorp", "Ibbagamuwa", "Imbulgoda",
        "Ingiriya", "Ja-Ela", "Jaffna", "Jaffna City", "Kadawatha", "Kaduruwela", "Kaduwela",
        "Kahatagasdigiliya", "Kahawatta", "Kalawana", "Kalmunai", "Kalutara", "Kalutara City",
        "Kamburupitiya", "Kandana", "Kandavalai", "Kandy", "Kandy City", "Kantalai",
        "Karagoda Uyangoda", "Karapitiya", "Kataragama", "Katugastota", "Katunayake",
        "Kattankudy", "Kayts", "Kegalle", "Kegalle City", "Kekirawa", "Kelaniya", "Kesbewa",
        "Kilinochchi", "Kilinochchi City", "Kinniya", "Kiribathkumbura", "Kirindiwela",
        "Kirulapana", "Kochchikade", "Kolonnawa", "Kosgama", "Koslanda", "Kotadeniyawa",
        "Kotagala", "Kotmale", "Kottawa", "Kuchchaveli", "Kuliyapitiya", "Kundasale", "Kuragala",
        "Kurunegala", "Kurunegala City", "Laggala-Pallegama", "Lahugala", "Laksapana",
        "Lellopitiya", "Lindula", "Liyanagemulla", "Lunugala", "Lunugamvehera", "Madakalapuwa",
        "Madampe", "Madhu", "Madurankuliya", "Mahagalgamuwa", "Maharagama", "Mahawa",
        "Mahiyanganaya", "Mailapitiya", "Maho", "Makandura", "Makumbura", "Malabe", "Maligawila",
        "Mallakam", "Manampitiya", "Mandaitivu", "Mannar", "Manthai East", "Maradana", "Marawila",
        "Maskeliya", "Matale", "Matara", "Matara City", "Mathugama", "Mawanella", "Mawathagama",
        "Medagama", "Medawachchiya", "Medirigiriya", "Meegoda", "Meegollewa", "Meepilimana",
        "Melsiripura", "Mihintale", "Minneriya", "Mirigama", "Mirissa", "Mitiswala",
        "Miyana Eliya", "Monaragala", "Monaragala City", "Moratuwa", "Moronthuduwa",
        "Mount Lavinia", "Mullaitivu", "Mullaitivu City", "Mundel", "Murunkan", "Mutur",
        "Nagalagam Street", "Nallur", "Narahenpita", "Naula", "Navatkuli", "Nawalapitiya", "Nawinna",
        "Negombo", "Negombo City", "Nikaweratiya", "Nilaveli", "Nittambuwa", "Norocholai",
        "Nugegoda", "Oddamavadi", "Ohiya", "Padukka", "Padiyathalawa", "Pahala Madampella",
        "Pallekele", "Panadura", "Pannala", "Pannipitiya", "Parakaduwa", "Passara", "Pattiyapola",
        "Pellessa", "Pelmadulla", "Peradeniya", "Piliyandala", "Pitabeddara", "Pitakotte",
        "Point Pedro", "Polgahawela", "Polonnaruwa", "Polonnaruwa City", "Poonakary",
        "Poruwadanda", "Pothuhera", "Pottuvil", "Puliyankulam", "Punanai", "Puttalam",
        "Puthukkudiyiruppu", "Ragama", "Rajagiriya", "Rajanganaya", "Rambaikulam", "Rambukkana",
        "Ranna", "Ranala", "Ratmalana", "Ratnapura", "Ratnapura City", "Rideegama", "Ruwanwella",
        "Sabaragamuwa", "Sagama", "Saliyapura", "Sammanthurai", "Seeduwa", "Seenigama",
        "Seethawakapura", "Serunuwara", "Seruvila", "Sigiriya", "Siyambalanduwa", "Sooriyawewa",
        "Talawakele", "Tambuttegama", "Tangalle", "Tawalama", "Teldeniya", "Tissamaharama",
        "Trincomalee", "Trincomalee City", "Tulhiriya", "Udadumbara", "Udappuwa", "Ukuwela",
        "Ulpotha", "Unawatuna", "Uragasmanhandiya", "Vaddukoddai", "Vakarai", "Valachchenai",
        "Vandaramulla", "Vavuniya", "Vavuniya City", "Veyangoda", "Wadduwa", "Waga", "Waikkala",
        "Warakapola", "Wariyapola", "Waskaduwa", "Watapuluwa", "Wattegama", "Wattegoda",
        "Wavulagala", "Weligama", "Welimada", "Welimessa", "Wellampitiya", "Wellawatte",
        "Wellawaya", "Wennappuwa", "Weragala", "Wewalwatta", "Wilgamuwa", "Wilpattu", "Wiralur",
        "Wiyaluwa", "Yakkala", "Yala", "Yatiyantota", "Yodakandiya", "Yogarathnapura"
            };

            foreach (string cityName in cities)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = cityName;
                city.Items.Add(item);
            }
        }

        private void LoadTimeDropdowns()
        {
            // Hours 00–23
            for (int i = 0; i < 24; i++)
            {
                string hour = i.ToString("00");

                CbCheckIn_Hour.Items.Add(hour);
                CbCheckOut_Hour.Items.Add(hour);
                CbSatCheckIn_Hour.Items.Add(hour);
                CbSatCheckOut_Hour.Items.Add(hour);
            }

            // Minutes 00–59
            for (int i = 0; i < 60; i++)
            {
                string minute = i.ToString("00");

                CbCheckIn_Minute.Items.Add(minute);
                CbCheckOut_Minute.Items.Add(minute);
                CbSatCheckIn_Minute.Items.Add(minute);
                CbSatCheckOut_Minute.Items.Add(minute);
            }

            // Default values
            CbCheckIn_Hour.SelectedIndex = 0;
            CbCheckIn_Minute.SelectedIndex = 0;
            CbCheckOut_Hour.SelectedIndex = 0;
            CbCheckOut_Minute.SelectedIndex = 0;
            CbSatCheckIn_Hour.SelectedIndex = 0;
            CbSatCheckIn_Minute.SelectedIndex = 0;
            CbSatCheckOut_Hour.SelectedIndex = 0;
            CbSatCheckOut_Minute.SelectedIndex = 0;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }









   


    }


    public class FingerprintSlot
    {
        public int FingerId { get; set; }

        public bool IsRegistered { get; set; }

        public string ButtonText =>
            IsRegistered ? "Replace" : "Capture";

        public string Status =>
            IsRegistered
                ? $"Fingerprint {FingerId} Registered"
                : $"Fingerprint {FingerId} Empty";
    }


}
