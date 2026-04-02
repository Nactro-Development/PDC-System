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
    public partial class AddJobCardWindow : Window
    {
        public JobCard JobCard { get; private set; }
        private BitmapSource? capturedImage;
        private string? tempCapturedFilePath;

        private readonly string saversFolder = Path.Combine(Directory.GetCurrentDirectory(), "Savers");
        private string outsourcingFile => Path.Combine(saversFolder, "Outsource.json");
        private string jsonFile;
        private string currentJobNumber;
        private string? selectedCompany;
        private string? selectedPlateName;
        private string selectedPrintingType;

        // Edit mode: holds the original job card being edited
        private JobCard? _editingJobCard;
        private bool _isEditMode => _editingJobCard != null;

        // ── Add mode constructor ──────────────────────────────────────────
        public AddJobCardWindow(List<Customerinfo> customers)
        {
            InitializeComponent();
            typebox.Text = "Person";
            CustomerComboBox.ItemsSource = customers;
            CustomerComboBox.SelectionChanged += CustomerComboBox_SelectionChanged;

            Digital.Visibility = Visibility.Visible;
            Offset.Visibility = Visibility.Collapsed;
            Oustanding_Printing.Visibility = Visibility.Collapsed;

            if (!Directory.Exists(saversFolder))
                Directory.CreateDirectory(saversFolder);

            jsonFile = Path.Combine(saversFolder, "jobData.json");

            GenerateJobNumber();
            Digital_Checked.IsChecked = true;
        }



        // ── Edit mode constructor ─────────────────────────────────────────
        public AddJobCardWindow(List<Customerinfo> customers, JobCard existingJobCard)
            : this(customers)
        {
            _editingJobCard = existingJobCard;
            PreFillFields(existingJobCard);
        }

        private void PreFillFields(JobCard jc)
        {
            // Temporarily unsubscribe to prevent SelectionChanged from resetting fields
            CustomerComboBox.SelectionChanged -= CustomerComboBox_SelectionChanged;

            // Job number (read-only in edit mode)
            currentJobNumber = jc.JobNo ?? currentJobNumber;
            JobNumberTextBox.Text = currentJobNumber;
            Date.SelectedDate = jc.JobCardDate;
            // Customer
            var matchedCustomer = (CustomerComboBox.ItemsSource as List<Customerinfo>)
                                  ?.FirstOrDefault(c => c.Name == jc.Customer_Name);
            if (matchedCustomer != null)
                CustomerComboBox.SelectedItem = matchedCustomer;
            else
                CustomerComboBox.Text = jc.Customer_Name;

            // Re-subscribe after setting customer
            CustomerComboBox.SelectionChanged += CustomerComboBox_SelectionChanged;

            // Description
            DescriptionTextBox.Text = jc.Description;

            // Quantity
            QuantityTextBox.Text = jc.Quantity.ToString();

            // Special note
            SpecialTextBox.Text = jc.Special_Note;

            // Printing type
            if (jc.Type == "Offset")
            {
                Offset_Checked.IsChecked = true;
                PlateCompanyTextBox.Text = jc.selectedPlateName;
                PlateQuantityTextBox.Text = jc.PlateQuantitiy;
                selectedPrintingType = "Offset";
                selectedPlateName = jc.selectedPlateName;
            }
            else
            {
                Digital_Checked.IsChecked = true;
                selectedPrintingType = "Digital";

                PaperSizeTextBox.Text = jc.Paper_Size;
                GSMTextBox.Text = jc.GSM.ToString();
                PaperTypeTextBox.Text = jc.Paper_Type;
                DsTextBox.Text = jc.Duplex;
                LaminateTextBox.Text = jc.Laminate;
                PrintedTextBox.Text = jc.Printed.ToString();

                // Outside printing
                if (!string.IsNullOrEmpty(jc.DigitalConpanyName) && jc.DigitalConpanyName != "OUR")
                {
                    OutstandingCheckBox.IsChecked = true;
                    Oustanding_Printing.Visibility = Visibility.Visible;
                    selectedCompany = jc.DigitalConpanyName;
                    Outstanding_PrintingCompanyName.Text = jc.DigitalConpanyName;
                }
                else
                {
                    OutstandingCheckBox.IsChecked = false;
                    Oustanding_Printing.Visibility = Visibility.Collapsed;
                }
            }

            // Screenshot preview - Handle both relative and absolute paths
            if (!string.IsNullOrEmpty(jc.ScreenshotPath))
            {
                string fullPath = jc.ScreenshotPath;

                // If it's a relative path, combine with app base directory
                if (!Path.IsPathRooted(fullPath))
                {
                    fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, jc.ScreenshotPath);
                }

                // Check if file exists at the resolved path
                if (File.Exists(fullPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.EndInit();
                    PreviewImage.Source = bitmap;
                    capturedImage = bitmap;
                    tempCapturedFilePath = fullPath;
                }
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
                this.Left = _previousLeft;
                this.Top = _previousTop;
                this.Width = _previousWidth;
                this.Height = _previousHeight;
                _isMaximized = false;
            }
            else
            {
                _previousLeft = this.Left;
                _previousTop = this.Top;
                _previousWidth = this.Width;
                _previousHeight = this.Height;

                var workingArea = SystemParameters.WorkArea;
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

        private void LoadComboBox()
        {
            if (File.Exists(outsourcingFile))
            {
                string json = File.ReadAllText(outsourcingFile);
                var allCompanies = JsonConvert.DeserializeObject<List<Outsourcinginfo>>(json);
                var digitalCompanies = allCompanies.Where(c => c.Type1 == "Digital").ToList();
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
                var digitalCompanies = allCompanies.Where(c => c.Type1 == "Plate").ToList();
                PlateCompanyTextBox.ItemsSource = digitalCompanies;
                PlateCompanyTextBox.DisplayMemberPath = "PlateName";
                PlateCompanyTextBox.SelectedValuePath = "PlateName";
            }
        }

        private void Outstanding_PrintingCompanyName_SelectionChanged2(object sender, SelectionChangedEventArgs e)
        {
            if (PlateCompanyTextBox.SelectedItem is Outsourcinginfo selected)
                selectedPlateName = selected.PlateName;
        }

        private void Outstanding_PrintingCompanyName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Outstanding_PrintingCompanyName.SelectedItem is Outsourcinginfo selected)
                selectedCompany = selected.DigitalName;
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
                if (lastJob.Contains("-"))
                    num = int.Parse(lastJob.Split('-')[1]);
                else
                    num = int.Parse(lastJob);

                num++;
                currentJobNumber = num.ToString("D4");
            }

            JobNumberTextBox.Text = currentJobNumber;
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomerComboBox.SelectedItem is Customerinfo selectedCustomer)
            {
                typebox.Text = string.IsNullOrEmpty(selectedCustomer.Type) ? "Person" : selectedCustomer.Type;
                companyname.Text = selectedCustomer.companyname;
            }
            else
            {
                typebox.Text = "Person";
            }
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            var captureWindow = new ScreenCaptureWindow();
            bool? result = captureWindow.ShowDialog();
            this.WindowState = WindowState.Normal;
            this.Activate();

            if (result == true && captureWindow.CapturedImage != null)
            {
                capturedImage = captureWindow.CapturedImage;

                string tempFolder = Path.GetTempPath();
                tempCapturedFilePath = Path.Combine(tempFolder, $"Temp_JobCard_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(capturedImage));
                using var stream = File.Create(tempCapturedFilePath);
                encoder.Save(stream);

                PreviewImage.Source = capturedImage;
            }
        }

        private bool ValidateFields()
        {
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

            if (string.IsNullOrWhiteSpace(selectedPrintingType))
            {
                CustomMessageBox.Show("Please select a Printing Type (Digital or Offset).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

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

                if (OutstandingCheckBox.IsChecked == true && Outstanding_PrintingCompanyName.SelectedItem == null)
                {
                    CustomMessageBox.Show("Please select a Digital Printing Company.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            GSMTextBox.Text = string.IsNullOrWhiteSpace(GSMTextBox.Text) ? "0" : GSMTextBox.Text;
            QuantityTextBox.Text = string.IsNullOrWhiteSpace(QuantityTextBox.Text) ? "0" : QuantityTextBox.Text;
            PrintedTextBox.Text = string.IsNullOrWhiteSpace(PrintedTextBox.Text) ? "0" : PrintedTextBox.Text;

            int gsm = int.Parse(GSMTextBox.Text);
            int quantity = int.Parse(QuantityTextBox.Text);
            int printed = int.Parse(PrintedTextBox.Text);

            string customerName = (CustomerComboBox.SelectedItem as Customerinfo)?.Name ?? CustomerComboBox.Text;

            string? finalScreenshotPath = null;

            if (!string.IsNullOrEmpty(tempCapturedFilePath) && File.Exists(tempCapturedFilePath))
            {
                // Keep existing path if it's already in the permanent folder
                if (tempCapturedFilePath.Contains("Savers"))
                {
                    finalScreenshotPath = tempCapturedFilePath;
                }
                else
                {
                    string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savers", "Screenshots");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    finalScreenshotPath = Path.GetFullPath(Path.Combine(folder, $"JobCard_{DateTime.Now:yyyyMMdd_HHmmss}.png"));
                    File.Copy(tempCapturedFilePath, finalScreenshotPath, true);
                }
            }
            else if (_isEditMode)
            {
                // Keep existing screenshot if no new one was captured
                finalScreenshotPath = _editingJobCard!.ScreenshotPath;
            }

            if (_isEditMode)
            {
                // Update the existing JobCard object in place
                _editingJobCard!.Customer_Name = customerName;
                _editingJobCard.Description = DescriptionTextBox.Text;
                _editingJobCard.JobCardDate = Date.SelectedDate ?? _editingJobCard.JobCardDate;
                _editingJobCard.Quantity = quantity;
                _editingJobCard.Printed = printed;
                _editingJobCard.GSM = gsm;
                _editingJobCard.Paper_Size = PaperSizeTextBox.Text;
                _editingJobCard.Paper_Type = PaperTypeTextBox.Text;
                _editingJobCard.Duplex = DsTextBox.Text;
                _editingJobCard.Laminate = LaminateTextBox.Text;
                _editingJobCard.Special_Note = SpecialTextBox.Text;
                _editingJobCard.Type = selectedPrintingType;
                _editingJobCard.DigitalConpanyName = selectedCompany;
                _editingJobCard.selectedPlateName = selectedPlateName;
                _editingJobCard.PlateQuantitiy = PlateQuantityTextBox.Text;
                _editingJobCard.ScreenshotPath = finalScreenshotPath;
                JobCard = _editingJobCard;
            }
            else
            {
                // Add mode — register job number
                List<JobNo> jobs = new List<JobNo>();
                if (File.Exists(jsonFile))
                {
                    string json = File.ReadAllText(jsonFile);
                    jobs = JsonConvert.DeserializeObject<List<JobNo>>(json) ?? new List<JobNo>();
                }

                if (jobs.Any(j => j.JobNumber == currentJobNumber))
                    return;

                jobs.Add(new JobNo { JobNumber = currentJobNumber });
                File.WriteAllText(jsonFile, JsonConvert.SerializeObject(jobs, Formatting.Indented));
                GenerateJobNumber();
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
                    Special_Note = SpecialTextBox.Text,
                    Paper_Type = PaperTypeTextBox.Text,
                    Quantity = quantity,
                    Printed = printed,
                    PlateQuantitiy = PlateQuantityTextBox.Text,
                    ScreenshotPath = finalScreenshotPath,
                    Type = selectedPrintingType,
                    IsSeen = false,
                };
            }

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
            OutstandingCheckBox.IsChecked = false;
            Oustanding_Printing.Visibility = Visibility.Collapsed;
            Outstanding_PrintingCompanyName.Text = "";
            PrintedTextBox.Text = "";

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

    public class JobNo
    {
        public string JobNumber { get; set; }
    }
}