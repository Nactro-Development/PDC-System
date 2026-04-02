using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PDC_System.Job_Card
{
    /// <summary>
    /// Interaction logic for JobCardView.xaml
    /// </summary>
    public partial class JobCardView : Window
    {
        #region Fields

        private JobCard _jobCardData;
        private readonly string saversFolder = Path.Combine(Directory.GetCurrentDirectory(), "Savers");

        #endregion

        #region Constructor

        public JobCardView(JobCard data)
        {
            InitializeComponent();
            _jobCardData = data;
            LoadJobCardData();
        }

        #endregion

        #region Data Loading

        private void LoadJobCardData()
        {
            try
            {
                // Load Screenshot Image
                LoadScreenshotImage();

                // Check if Offset type - show simplified view
                bool isOffset = _jobCardData.Type?.Equals("Offset", StringComparison.OrdinalIgnoreCase) == true;

                if (isOffset)
                {
                    // Show only Name, Description, Quantity for Offset
                    CustomerNameText.Text = _jobCardData.Customer_Name;
                    DateText.Text = _jobCardData.JobCardDate.ToString("yyyy-MM-dd");
                    DescriptionText.Text = _jobCardData.Description;
                    QuantityTextSimple.Text = _jobCardData.Quantity.ToString("N0");
                    JobNoText.Text = $"Job #{_jobCardData.JobNo}";

                    PlateQuantityTextSimple.Text = _jobCardData.PlateQuantitiy ?? "-";

                    // Hide all other sections for Offset
                    JobDetailsSection.Visibility = Visibility.Collapsed;
                    PaperSpecSection.Visibility = Visibility.Collapsed;
                    PrintingDetailsSection.Visibility = Visibility.Collapsed;
                    AdditionalInfoSection.Visibility = Visibility.Collapsed;

                    // Show only customer info section with minimal fields
                    CustomerInfoSection.Visibility = Visibility.Visible;
                }
                else
                {
                    // Show all sections for non-Offset types
                    CustomerInfoSection.Visibility = Visibility.Visible;
                    JobDetailsSection.Visibility = Visibility.Visible;
                    PaperSpecSection.Visibility = Visibility.Visible;
                    PrintingDetailsSection.Visibility = Visibility.Visible;
                    AdditionalInfoSection.Visibility = Visibility.Visible;
                    QuantityRowSimple.Visibility = Visibility.Collapsed;
                    // Bind all data
                    JobNoText.Text = $"Job #{_jobCardData.JobNo}";
                    CustomerNameText.Text = _jobCardData.Customer_Name;
                    DateText.Text = _jobCardData.JobCardDate.ToString("yyyy-MM-dd");
                    DescriptionText.Text = _jobCardData.Description;
                    TypeText.Text = _jobCardData.Type;

                    string company = !string.IsNullOrEmpty(_jobCardData.DigitalConpanyName)
                      ? _jobCardData.DigitalConpanyName
                      : _jobCardData.PlateCompanyName;
                    CompanyText.Text = string.IsNullOrEmpty(company) ? "-" : company;

                    PaperSizeText.Text = _jobCardData.Paper_Size;
                    GSMText.Text = _jobCardData.GSM.ToString();
                    PaperTypeText.Text = _jobCardData.Paper_Type;
                    QuantityText.Text = _jobCardData.Quantity.ToString("N0");
                    PrintedText.Text = _jobCardData.Printed.ToString("N0");
                    DuplexText.Text = _jobCardData.Duplex;
                    LaminateText.Text = _jobCardData.Laminate;
                    SpecialNoteText.Text = string.IsNullOrEmpty(_jobCardData.Special_Note) ? "-" : _jobCardData.Special_Note;
                }

                // Load Type Icon
                LoadTypeImage(_jobCardData.Type);
            }
            catch (Exception ex)
            {

                CustomMessageBox.Show($"Error loading job card data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        #region Image Loading

        private void LoadScreenshotImage()
        {
            if (string.IsNullOrEmpty(_jobCardData.ScreenshotPath))
            {
                ScreenshotBorder.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                // Try relative path first
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
     _jobCardData.ScreenshotPath);

                // If relative path doesn't exist, try absolute path
                if (!File.Exists(fullPath))
                {
                    fullPath = _jobCardData.ScreenshotPath;
                }

                if (File.Exists(fullPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.EndInit();
                    ScreenshotImage.Source = bitmap;
                    ScreenshotBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    ScreenshotBorder.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ScreenshotBorder.Visibility = Visibility.Collapsed;
                Console.WriteLine($"Screenshot load error: {ex.Message}");
            }
        }

        private void LoadTypeImage(string type)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string imageFile = type switch
                {
                    "Company" => "Assets/Company.png",
                    "Person" => "Assets/Person.png",
                    "Digital" => "Assets/Digital.png",
                    "Offset" => "Assets/Offset.png",
                    _ => "Assets/Default.png"
                };

                string imagePath = System.IO.Path.Combine(baseDir, imageFile);

                if (File.Exists(imagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    TypeImage.Source = bitmap;
                }
                else
                {
                    // Hide icon if not found instead of showing error
                    TypeImage.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                TypeImage.Visibility = Visibility.Collapsed;
                Console.WriteLine($"Type image load error: {ex.Message}");
            }
        }

        #endregion

        #region Quotation and Invoice

        private void CreateQuotationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load customer from job card
                Customerinfo customer = GetCustomerFromJobCard();

                // Create quotation window
                AddQuotationWindow window = new AddQuotationWindow();

                // Pre-fill customer and items after window content is loaded
                window.ContentRendered += (s, args) =>
                {
                    // Set customer in ComboBox if found
                    if (customer != null)
                    {
                        foreach (var item in window.CustomerComboBox.Items)
                        {
                            if (item is Customerinfo c && c.Name == customer.Name)
                            {
                                window.CustomerComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    // Add initial item with job description
                    if (!string.IsNullOrEmpty(_jobCardData.Description))
                    {
                        window.Items.Add(new QuotationItem
                        {
                            Description = _jobCardData.Description,
                            Qty = (int)_jobCardData.Quantity,
                            UnitAmount = 0,
                            IsTitle = false
                        });
                    }
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error creating quotation: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load all customers for manual selection
                List<Customerinfo> customers = LoadCustomers();

                if (customers == null || customers.Count == 0)
                {
                    CustomMessageBox.Show("No customers found. Please add customers first.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get customer from job card
                Customerinfo customer = GetCustomerFromJobCard();

                // Create invoice with manual customer selection
                InvoiceWindow invoiceWindow = new InvoiceWindow(customers);

                // Pre-fill customer and description after window content is rendered
                invoiceWindow.ContentRendered += (s, args) =>
                {
                    // Set customer in ComboBox if found
                    if (customer != null)
                    {
                        foreach (var item in invoiceWindow.CustomerComboBox.Items)
                        {
                            if (item is Customerinfo c && c.Name == customer.Name)
                            {
                                invoiceWindow.CustomerComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    // Add initial item with job description
                    if (!string.IsNullOrEmpty(_jobCardData.Description))
                    {
                        invoiceWindow.Items.Add(new QuotationItem
                        {
                            Description = _jobCardData.Description,
                            Qty = (int)_jobCardData.Quantity,
                            UnitAmount = 0,
                            IsTitle = false
                        });
                    }
                };

                invoiceWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error creating invoice: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Customer Data

        private Customerinfo GetCustomerFromJobCard()
        {
            try
            {
                string customerFile = Path.Combine(saversFolder, "customers.json");

                if (!File.Exists(customerFile))
                    return null;

                string json = File.ReadAllText(customerFile);
                List<Customerinfo> customers = JsonConvert.DeserializeObject<List<Customerinfo>>(json);

                // Try to find customer by name
                return customers?.FirstOrDefault(c =>
                    c.Name.Equals(_jobCardData.Customer_Name, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading customer: {ex.Message}");
                return null;
            }
        }

        private List<Customerinfo> LoadCustomers()
        {
            try
            {
                string customerFile = Path.Combine(saversFolder, "customers.json");

                if (!File.Exists(customerFile))
                    return new List<Customerinfo>();

                string json = File.ReadAllText(customerFile);
                return JsonConvert.DeserializeObject<List<Customerinfo>>(json) ?? new List<Customerinfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading customers: {ex.Message}");
                return new List<Customerinfo>();
            }
        }

        #endregion

        #region Button Events

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Print the content
                    printDialog.PrintVisual(MainContent, $"Job Card - {_jobCardData.JobNo}");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Print error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Keyboard Shortcuts

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Close window with Escape key
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
        }

        #endregion
    }
}