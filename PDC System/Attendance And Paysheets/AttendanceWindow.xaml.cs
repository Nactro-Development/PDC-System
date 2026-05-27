using Google.Apis.PeopleService.v1.Data;
using ModernCalendarLib;
using Newtonsoft.Json;
using PDC_System.Attendance_And_Paysheets;
using PDC_System.Models;
using PDC_System.Services;
using PDC_System.Settings;
using PdfSharp.Charting;
using PdfSharp.UniversalAccessibility;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using static PDC_System.QuotationWindow;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace PDC_System
{
    public partial class AttendanceWindow : UserControl
    {
        #region Fields

        private List<Employee> employees = new List<Employee>(); // Employee list
        
        private List<Holiday> holidays = new List<Holiday>(); // Attendance list
        private DataTable dataTable; // DataTable for DataGridMain
        private List<AttendanceRecord> _allAttendanceRecords;
        private AttendanceManager _manager;


        private readonly string saversFolder; // Folder to store JSON files

        #endregion

        #region Constructor

        public AttendanceWindow()
        {
            InitializeComponent();

            // Populate Month and Year ComboBoxes
            _manager = new AttendanceManager();
           
            // Set the Savers folder inside the current working directory
            saversFolder = Path.Combine(Directory.GetCurrentDirectory(), "Savers");
            if (!Directory.Exists(saversFolder))
            {
                Directory.CreateDirectory(saversFolder);
            }

            // Load existing data
            LoadData();
        }

        #endregion

        #region Load Data

        private void LoadData()
        {
            // Load holidays (JSON)
            string holidayFile = Path.Combine(saversFolder, "Holiday.json");
            if (File.Exists(holidayFile))
            {
                holidays = JsonConvert.DeserializeObject<List<Holiday>>(File.ReadAllText(holidayFile));
                HolydayDataGrid.ItemsSource = holidays;
            }

            // Load employees (JSON)
            string employeeFile = Path.Combine(saversFolder, "employee.json");
            if (File.Exists(employeeFile))
            {
                employees = JsonConvert.DeserializeObject<List<Employee>>(File.ReadAllText(employeeFile));
            }

            // Load raw punch data from SQLite
            dataTable = _manager.LoadRawPunchTable();

            if (dataTable != null)
            {
                DataGridMain.ItemsSource = dataTable.DefaultView;

                // Default filter = today’s punches
                DateTime today = DateTime.Today;
                DataView view = dataTable.DefaultView;
                view.RowFilter =
                    $"DateTime >= #{today:yyyy-MM-dd}# AND DateTime < #{today.AddDays(1):yyyy-MM-dd}#";
            }
        }





        #endregion



        private void DataGridMain_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Find the parent ScrollViewer (inside the TabControl)
            var parent = FindVisualParent<ScrollViewer>((DependencyObject)sender);

            if (parent != null)
            {
                // Scroll manually
                parent.ScrollToVerticalOffset(parent.VerticalOffset - e.Delta / 3);
                e.Handled = true;
            }
        }

        // Helper function to walk up the visual tree
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }




        private void FilterChanged(object sender, EventArgs e)
        {
            if (dataTable == null) return;

            string filter = "";

            // Search filter
            string searchText = TxtSearch.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrEmpty(searchText))
            {
                filter += $"(Convert(EmployeeID, 'System.String') LIKE '%{searchText}%' " +
                          $"OR EmployeeName LIKE '%{searchText}%')";
            }

            // Date filters
            if (StartDate.SelectedDate.HasValue)
            {
                if (filter != "") filter += " AND ";
                filter += $"DateTime >= #{StartDate.SelectedDate:yyyy-MM-dd}#";
            }

            if (EndDate.SelectedDate.HasValue)
            {
                if (filter != "") filter += " AND ";
                filter += $"DateTime <= #{EndDate.SelectedDate:yyyy-MM-dd}#";
            }

    (DataGridMain.ItemsSource as DataView).RowFilter = filter;
        }



        #region Holidays





        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Show the input panel
            InputStackPanel.Visibility = Visibility.Visible;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = TxtName.Text.Trim();
                DateTime startDate = HolidayStartDatePicker.SelectedDate ?? DateTime.Now;
                DateTime endDate = HolidayEndDatePicker.SelectedDate ?? startDate;

                if (endDate < startDate)
                {
                    CustomMessageBox.Show("End date cannot be earlier than start date!", "Invalid Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int addedCount = 0;

                // Loop through date range
                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    // Skip duplicates
                    if (holidays.Any(h => h.Date.Date == date.Date))
                        continue;

                    // Create holiday
                    var holiday = new Holiday
                    {
                        Name = name,
                        Date = date.Date
                    };

                    holidays.Add(holiday);
                    addedCount++;
                }

                // Save to JSON
                string JsonFile2 = Path.Combine(saversFolder, "Holiday.json");
                File.WriteAllText(JsonFile2, JsonConvert.SerializeObject(holidays, Formatting.Indented));

                // Refresh DataGrid
                HolydayDataGrid.ItemsSource = null;
                HolydayDataGrid.ItemsSource = holidays;

                // Clear input fields
                InputStackPanel.Visibility = Visibility.Collapsed;
                TxtName.Clear();
                HolidayStartDatePicker.SelectedDate = null;
                HolidayEndDatePicker.SelectedDate = null;
                HolidayStartDatePicker.ResetDate();
                HolidayEndDatePicker.ResetDate();

                CustomMessageBox.Show(
                    addedCount > 0
                        ? $"✅ {addedCount} holiday(s) added successfully!"
                        : "⚠️ No new holidays added (duplicates skipped).",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error: " + ex.Message);
            }
        }


     
        // Selected holidays track කරන්න HashSet එකක්
        private HashSet<Holiday> selectedHolidays = new HashSet<Holiday>();

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var holiday = checkbox.Tag as Holiday;
            if (holiday != null)
            {
                selectedHolidays.Add(holiday);
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var holiday = checkbox.Tag as Holiday;
            if (holiday != null)
            {
                selectedHolidays.Remove(holiday);
            }
        }

        private void HolidayDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedHolidays.Count == 0)
                {
                    CustomMessageBox.Show("Please select at least one holiday to delete.",
                                  "No Selection",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                var result = CustomMessageBox.Show(
                    $"Are you sure you want to delete {selectedHolidays.Count} holiday(s)?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Remove selected holidays
                    foreach (var holiday in selectedHolidays.ToList())
                    {
                        holidays.Remove(holiday);
                    }

                    // Clear selection
                    selectedHolidays.Clear();

                    // Save to JSON
                    string jsonFile2 = Path.Combine(saversFolder, "Holiday.json");
                    File.WriteAllText(jsonFile2, JsonConvert.SerializeObject(holidays, Formatting.Indented));

                    // Refresh DataGrid
                    HolydayDataGrid.ItemsSource = null;
                    HolydayDataGrid.ItemsSource = holidays;

                    CustomMessageBox.Show($"✅ Holiday(s) deleted successfully!",
                                  "Success",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error deleting holidays: {ex.Message}",
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }






        #endregion


        #region Attendance Calculate

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Hide the DataGrid while loading
                DataGridMain.Visibility = Visibility.Collapsed;
                AttendanceGrid.Visibility = Visibility.Collapsed;
                TxtLoading.Visibility = Visibility.Visible; // optional: show a "Loading..." TextBlock
                TxtLoading2.Visibility = Visibility.Visible;

                await Task.Run(() =>
                {
                    // Load all attendance records
                    _allAttendanceRecords = _manager.LoadAttendance();

                    // Save all records
                    _manager.SaveAllAttendanceRecords(_allAttendanceRecords);
                });

                // Display only today's records
                DateTime today = DateTime.Today;
                AttendanceGrid.ItemsSource = _allAttendanceRecords
                                             .Where(r => r.Date.Date == today)
                                             .ToList();

                StartDatePicker.SelectedDate = today;
                EndDatePicker.SelectedDate = today;

                // ✅ Add this line - Load missing dashboard AFTER data is loaded!
                LoadMissingDashboard();

                // Show the DataGrid after loading
                DataGridMain.Visibility = Visibility.Visible;
                AttendanceGrid.Visibility = Visibility.Visible;
                TxtLoading2.Visibility = Visibility.Collapsed;
                TxtLoading.Visibility = Visibility.Collapsed; // hide the loading text
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading attendance data: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private void LoadAttendanceData()
        {
            try
            {
                _allAttendanceRecords = _manager.LoadAttendanceWithDateRange(
                    StartDatePicker.SelectedDate ?? DateTime.Today,
                    EndDatePicker.SelectedDate ?? DateTime.Today);

                ApplyFilters();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading attendance data: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allAttendanceRecords == null) return;

            var filteredRecords = _allAttendanceRecords.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                string searchTerm = SearchTextBox.Text.ToLower();
                filteredRecords = filteredRecords.Where(r =>
                    r.EmployeeId.ToLower().Contains(searchTerm) ||
                    r.Name.ToLower().Contains(searchTerm));
            }

            AttendanceGrid.ItemsSource = filteredRecords.ToList();
        }

        private void BtnFilterAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
                {
                    CustomMessageBox.Show("Start date cannot be after end date.", "Invalid Date Range",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LoadAttendanceData();
            }
            else
            {
                CustomMessageBox.Show("Please select both start and end dates.", "Missing Dates",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }







        // Ensure this method exists in your code-behind file with the correct signature:
        private void BtnFilterAttendanceID_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        
       

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
        }

        private void BtnResetDates_Click(object sender, RoutedEventArgs e)
        {
            // Clear selected dates
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;

            StartDatePicker.ResetDate();
            EndDatePicker.ResetDate();

            // Load all attendance records
            _allAttendanceRecords = _manager.LoadAttendance();

            // Get today's date
            DateTime today = DateTime.Today;

            // Filter and display only today's records in the grid
            var todayRecords = _allAttendanceRecords
                .Where(r => r.Date.Date == today)
                .ToList();

            AttendanceGrid.ItemsSource = todayRecords;
        }





        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var record = button.DataContext as AttendanceRecord;

            // Create and show dim window
            var dim = new DimWindow();
            dim.Show();

            // Open Edit window
            var editWindow = new EditAttendanceWindow(record);
            editWindow.Owner = dim;
            editWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            editWindow.ShowDialog();

            // Close dim window
            dim.Close();

            if (editWindow.DialogResult == true)
            {
                LoadAttendanceData();
            }
        }






        private void BtnRefresh(object sender, RoutedEventArgs e)
        {
            LoadAttendanceData();
        }

       

        private void btnResetRow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var record = button.DataContext as AttendanceRecord;

                if (record == null) return;



                var result = CustomMessageBox.Show(
                    $"Are you sure you want to reset manual edits for {record.Name} on {record.Date:yyyy-MM-dd}?",
                    "Reset Manual Edit",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var manager = new AttendanceManager();

                    // Reset only this record
                    manager.ResetManualEditForRecord(record);


                    NotificationHelper.ShowNotification(
                        "PDC System!",
                        $"{record.Name}\n" +
                        $"{record.Date:yyyy-MM-dd}\n" +
                        $"Reset Manual Edit!"

                    );

                  

                    CustomMessageBox.Show("Manual edit reset successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh grid
                    LoadAttendanceData();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error resetting row: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✔ Convert grid rows to a simple list
                var exportList = AttendanceGrid.Items
                    .OfType<AttendanceRecord>()
                    .ToList();

                if (exportList.Count == 0)
                {
                    CustomMessageBox.Show("No data to export.", "Empty", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✔ Ask user for save location
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF File|*.pdf",
                    FileName = $"AttendanceReport_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    AttendancePdfGenerator.CreatePdf(exportList, dialog.FileName);

                    CustomMessageBox.Show("PDF exported successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error exporting PDF: " + ex.Message);
            }
        }



        #endregion

        private void StartDate_Loaded(object sender, RoutedEventArgs e)
        {

        }


        private void EditMissingAttendance_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var record = button.Tag as AttendanceRecord;

            var dim = new DimWindow();
            dim.Show();

            var edit = new EditAttendanceWindow(record);
            edit.Owner = dim;
            edit.ShowDialog();

            dim.Close();

            if (edit.DialogResult == true)
            {
                LoadAttendanceData();
                LoadMissingDashboard();
            }
        }

        private void LoadMissingDashboard()
        {
            // ✅ Always load fresh data for the dashboard - don't rely on _allAttendanceRecords
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Load attendance records for the entire current month
            var monthlyRecords = _manager.LoadAttendanceWithDateRange(startOfMonth, endOfMonth);

            if (monthlyRecords == null) return;

            var missing = monthlyRecords
                .Where(x => x.Status == "Missing Finger Print")
                .OrderByDescending(x => x.Date)
                .ToList();

            MissingListBox.ItemsSource = missing;

            int missingToday = missing.Count(x => x.Date.Date == today);

            MissingTodayTxt.Text = missingToday.ToString();
            TotalMissingTxt.Text = missing.Count.ToString();
        }








        private void BtnRefreshIvms_Click(object sender, RoutedEventArgs e)
        {

            // 🔍 Try to find existing SettingsWindow
            var settingsWindow = Application.Current.Windows
                .OfType<SettingsWindow>()
                .FirstOrDefault();

            if (settingsWindow != null)
            {
                settingsWindow.BtnLoad_Click();


            }
            else
            {
                var backgroundSettings = new SettingsWindow();
                backgroundSettings.Visibility = Visibility.Hidden;

                if (backgroundSettings != null)
                {
                    backgroundSettings.BtnLoad_Click();
                    backgroundSettings.Close();


                }
                else
                {
                    CustomMessageBox.Show("⚠️ Settings window could not be created.",
                                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // 🧾 Attendance Manager section
            var manager = new PDC_System.Services.AttendanceManager();

            if (manager != null)
            {
                var records = manager.LoadAttendance();

                if (records != null && records.Any())
                {
                    manager.SaveAllAttendanceRecords(records);
                    NotificationHelper.ShowNotification("PDC System!", "Attendance Calculate Complete!");
                }
                else
                {
                    CustomMessageBox.Show("⚠️ No attendance records found to process.",
                                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                CustomMessageBox.Show("⚠️ Attendance Manager not initialized.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


            LoadAttendanceData();
            LoadData();

            // With this line:
            (DataGridMain.ItemsSource as ICollectionView)?.Refresh();
        }



    }
}
