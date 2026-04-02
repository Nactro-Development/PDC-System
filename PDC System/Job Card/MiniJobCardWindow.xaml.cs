using Newtonsoft.Json;
using PDC_System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PDC_System
{
    public partial class MiniJobCardWindow : Window
    {
        public JobCard JobCard { get; private set; }
        private BitmapSource? capturedImage;
        private string? tempCapturedFilePath; // Temporary save location

        private readonly string saversFolder = Path.Combine(Directory.GetCurrentDirectory(), "Savers");
        private string outsourcingFile => Path.Combine(saversFolder, "Outsource.json");
        private string jsonFile;
        private string currentJobNumber;
        private string? selectedCompany;
        private string? selectedPlateName;
        private string selectedPrintingType;

        public MiniJobCardWindow(List<Customerinfo> customers)
        {
            InitializeComponent();
            typebox.Text = "Person";
            CustomerComboBox.ItemsSource = customers;

            // Handle selection change
            CustomerComboBox.SelectionChanged += CustomerComboBox_SelectionChanged;


            Digital.Visibility = Visibility.Visible;
            Offset.Visibility = Visibility.Collapsed;

            // If Outside Printing checkbox is unchecked, hide the panel
            Oustanding_Printing.Visibility = Visibility.Collapsed;

            // Create Savers folder if not exists
            if (!Directory.Exists(saversFolder))
                Directory.CreateDirectory(saversFolder);

            jsonFile = Path.Combine(saversFolder, "jobData.json");

            GenerateJobNumber();
            Digital_Checked.IsChecked = true; // Default to Digital

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

            Close();
        }

        #endregion



















        private void LoadComboBox()
        {
            if (File.Exists(outsourcingFile))
            {
                string json = File.ReadAllText(outsourcingFile);
                var allCompanies = JsonConvert.DeserializeObject<List<Outsourcinginfo>>(json);

                // Filter only Digital
                var digitalCompanies = allCompanies
                    .Where(c => c.Type1 == "Digital")
                    .ToList();

                // Bind to ComboBox
                Outstanding_PrintingCompanyName.ItemsSource = digitalCompanies;
                Outstanding_PrintingCompanyName.DisplayMemberPath = "DigitalName";
                Outstanding_PrintingCompanyName.SelectedValuePath = "DigitalName";
            }
        }

        private void LoadComboBox2()
        {
            if (File.Exists(outsourcingFile))
            {
                string json = File.ReadAllText(outsourcingFile);
                var allCompanies = JsonConvert.DeserializeObject<List<Outsourcinginfo>>(json);

                // Filter only Digital
                var digitalCompanies = allCompanies
                    .Where(c => c.Type1 == "Plate")
                    .ToList();

                // Bind to ComboBox
                PlateCompanyTextBox.ItemsSource = digitalCompanies;
                PlateCompanyTextBox.DisplayMemberPath = "PlateName";
                PlateCompanyTextBox.SelectedValuePath = "PlateName";
            }
        }



        private void Outstanding_PrintingCompanyName_SelectionChanged2(object sender, SelectionChangedEventArgs e)
        {
            if (PlateCompanyTextBox.SelectedItem is Outsourcinginfo selected)
            {
                // Make sure this matches the property you want
                selectedPlateName = selected.PlateName;
                // Now selectedCompany contains the selected ComboBox value
            }
        }


        private void Outstanding_PrintingCompanyName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Outstanding_PrintingCompanyName.SelectedItem is Outsourcinginfo selected)
            {
                // Make sure this matches the property you want
                selectedCompany = selected.DigitalName;
                // Now selectedCompany contains the selected ComboBox value
            }
        }


        private void GenerateJobNumber()
        {
            List<JobNo> jobs = new List<JobNo>();

            if (File.Exists(jsonFile))
            {
                string json = File.ReadAllText(jsonFile);
                jobs = JsonConvert.DeserializeObject<List<JobNo>>(json) ?? new List<JobNo>();
            }

            if (jobs.Count == 0)
            {
                currentJobNumber = "0001";
            }
            else
            {
                string lastJob = jobs.Last().JobNumber;
                int num;

                // Check if job number contains a dash (e.g., "JOB-0001")
                if (lastJob.Contains("-"))
                {
                    num = int.Parse(lastJob.Split('-')[1]);
                }
                else
                {
                    num = int.Parse(lastJob);
                }

                num++;
                currentJobNumber = num.ToString("D4");

            }

            JobNumberTextBox.Text = currentJobNumber;
        }


        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomerComboBox.SelectedItem is Customerinfo selectedCustomer)
            {
                // Show customer type, or "Person" if Type is null/empty
                typebox.Text = string.IsNullOrEmpty(selectedCustomer.Type) ? "Person" : selectedCustomer.Type;
                companyname.Text = selectedCustomer.companyname;
            }
            else
            {
                // Default to "Person" if nothing is selected
                typebox.Text = "Person";
            }
        }


        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // Minimize this window while capturing instead of hiding
            this.WindowState = WindowState.Minimized;
            var captureWindow = new ScreenCaptureWindow();
            bool? result = captureWindow.ShowDialog();
            this.WindowState = WindowState.Normal;
            this.Activate(); // Bring window to front

            if (result == true && captureWindow.CapturedImage != null)
            {
                capturedImage = captureWindow.CapturedImage;

                // Save to temporary folder
                string tempFolder = Path.GetTempPath();
                tempCapturedFilePath = Path.Combine(tempFolder, $"Temp_JobCard_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(capturedImage));
                using var stream = File.Create(tempCapturedFilePath);
                encoder.Save(stream);

                PreviewImage.Source = capturedImage; // Preview image
            }
        }





        private bool ValidateFields()
        {
            // Common required fields
            string customerName = (CustomerComboBox.SelectedItem as Customerinfo)?.Name ?? CustomerComboBox.Text;
            if (string.IsNullOrWhiteSpace(customerName))
            {
                CustomMessageBox.Show("Please enter a Customer Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                CustomMessageBox.Show("Please enter a Description.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(QuantityTextBox.Text))
            {
                CustomMessageBox.Show("Please enter the Quantity.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Check if a printing type is selected
            if (string.IsNullOrWhiteSpace(selectedPrintingType))
            {
                CustomMessageBox.Show("Please select a Printing Type (Digital or Offset).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Offset-specific validation
            if (selectedPrintingType == "Offset")
            {
                if (PlateCompanyTextBox.SelectedItem == null && string.IsNullOrWhiteSpace(PlateCompanyTextBox.Text))
                {
                    CustomMessageBox.Show("Please select a Plate Company.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(PlateQuantityTextBox.Text))
                {
                    CustomMessageBox.Show("Please enter the Plate Quantity.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // Digital-specific validation
            if (selectedPrintingType == "Digital")
            {
                if (string.IsNullOrWhiteSpace(PaperSizeTextBox.Text))
                {
                    CustomMessageBox.Show("Please select a Paper Size.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GSMTextBox.Text))
                {
                    CustomMessageBox.Show("Please enter the GSM.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(PaperTypeTextBox.Text))
                {
                    CustomMessageBox.Show("Please select a Paper Type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (DsTextBox.SelectedItem == null)
                {
                    CustomMessageBox.Show("Please select D/S (Single Side or Double Side).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (LaminateTextBox.SelectedItem == null)
                {
                    CustomMessageBox.Show("Please select a Laminate option.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // If Outside Printing is checked, company must be selected
                if (OutstandingCheckBox.IsChecked == true && Outstanding_PrintingCompanyName.SelectedItem == null)
                {
                    CustomMessageBox.Show("Please select a Digital Printing Company.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // Special Note is OPTIONAL - no validation needed

            return true;
        }












        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields first (Special Note is optional)
            if (!ValidateFields())
                return;

            List<JobNo> jobs = new List<JobNo>();

            if (File.Exists(jsonFile))
            {
                string json = File.ReadAllText(jsonFile);
                jobs = JsonConvert.DeserializeObject<List<JobNo>>(json) ?? new List<JobNo>();
            }

            // Prevent duplicates silently
            if (jobs.Any(j => j.JobNumber == currentJobNumber))
                return;

            jobs.Add(new JobNo { JobNumber = currentJobNumber });

            // Save file (formatted)
            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(jobs, Formatting.Indented));

            // Generate next job number automatically
            GenerateJobNumber();

            // Set default 0 if empty
            GSMTextBox.Text = string.IsNullOrWhiteSpace(GSMTextBox.Text) ? "0" : GSMTextBox.Text;
            QuantityTextBox.Text = string.IsNullOrWhiteSpace(QuantityTextBox.Text) ? "0" : QuantityTextBox.Text;
            PrintedTextBox.Text = string.IsNullOrWhiteSpace(PrintedTextBox.Text) ? "0" : PrintedTextBox.Text;

            // Parse the values
            int gsm = int.Parse(GSMTextBox.Text);
            int quantity = int.Parse(QuantityTextBox.Text);
            int printed = int.Parse(PrintedTextBox.Text);

            string customerName = (CustomerComboBox.SelectedItem as Customerinfo)?.Name ?? CustomerComboBox.Text;

            string? finalScreenshotPath = null;
            if (!string.IsNullOrEmpty(tempCapturedFilePath) && File.Exists(tempCapturedFilePath))
            {
                // Use your existing folder: Savers\Screenshots
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savers", "Screenshots");

                // Ensure folder exists
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // Save absolute path
                finalScreenshotPath = Path.GetFullPath(Path.Combine(folder, $"JobCard_{DateTime.Now:yyyyMMdd_HHmmss}.png"));

                File.Copy(tempCapturedFilePath, finalScreenshotPath, true); // Copy to permanent location
            }


            // Get the selected date from DatePicker, fallback to today if none selected
            DateTime selectedDateTime = Date.SelectedDate ?? DateTime.Now;


            JobCard = new JobCard
            {
                Customer_Name = customerName,
                JobNo = currentJobNumber,
                DigitalConpanyName = selectedCompany,
                JobCardDate = selectedDateTime,
                selectedPlateName = selectedPlateName,
                Paper_Size = PaperSizeTextBox.Text,
                Description = DescriptionTextBox.Text,
                GSM = gsm,
                Duplex = DsTextBox.Text,
                Laminate = LaminateTextBox.Text,
                Special_Note = SpecialTextBox.Text, // Optional - can be empty
                Paper_Type = PaperTypeTextBox.Text,
                Quantity = quantity,
                Printed = printed,
                PlateQuantitiy = PlateQuantityTextBox.Text,
                ScreenshotPath = finalScreenshotPath,
                Type = selectedPrintingType,
                IsSeen = false,
            };

            this.DialogResult = true;

        }



        private void QuantityTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }


        private void Digital_Clicked(object sender, RoutedEventArgs e)
        {
            if (Digital == null || Offset == null) return;
            Digital.Visibility = Visibility.Visible;
            Offset.Visibility = Visibility.Collapsed;
            selectedPrintingType = "Digital";
            ResetOffsetFields();
            LoadComboBox();

        }

        private void Offset_Clicked(object sender, RoutedEventArgs e)
        {
            Digital.Visibility = Visibility.Collapsed;
            Offset.Visibility = Visibility.Visible;
            ResetDigitalFields();
            selectedPrintingType = "Offset";
            LoadComboBox2();
        }

        private void Outstanding_Checked(object sender, RoutedEventArgs e)
        {
            Oustanding_Printing.Visibility = Visibility.Visible;

        }

        private void Outstanding_Unchecked(object sender, RoutedEventArgs e)
        {
            Oustanding_Printing.Visibility = Visibility.Collapsed;
            selectedCompany = "OUR";
        }


        private void ResetDigitalFields()
        {
            // Uncheck Outside Printing
            OutstandingCheckBox.IsChecked = false;

            // Hide the Outside Printing panel
            Oustanding_Printing.Visibility = Visibility.Collapsed;

            // Clear all TextBoxes
            Outstanding_PrintingCompanyName.Text = "";
            PrintedTextBox.Text = "";
           

            // Clear all ComboBoxes (both selected item and editable text)
            PaperSizeTextBox.SelectedIndex = -1;
            PaperSizeTextBox.Text = "";

            GSMTextBox.SelectedIndex = -1;
            GSMTextBox.Text = "";

            PaperTypeTextBox.SelectedIndex = -1;
            PaperTypeTextBox.Text = "";

            DsTextBox.SelectedIndex = -1;
            DsTextBox.Text = "";

            LaminateTextBox.SelectedIndex = -1;
            LaminateTextBox.Text = "";

           
        }


        private void ResetOffsetFields()
        {
            PlateCompanyTextBox.Text = "";
            PlateQuantityTextBox.Text = "";
        }





    }

}
