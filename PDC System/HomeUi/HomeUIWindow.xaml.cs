using iText.Layout.Borders;
using LiveCharts;
using LiveCharts.Maps;
using LiveCharts.Wpf;
using Microsoft.Win32;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json;
using PDC_System.Helpers;
using PDC_System.HomeUi;
using PDC_System.Services;
using PDC_System.Models;
using System;
using System.Globalization;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using System.Collections.Generic;

namespace PDC_System
{
    /// <summary>
    /// Interaction logic for HomeUIWindow.xaml
    /// </summary>
    public partial class HomeUIWindow : System.Windows.Controls.UserControl
    {
        private DispatcherTimer timer;
        public List<BirthdayInfo> UpcomingBirthdays { get; set; } = new List<BirthdayInfo>();
        private List<Employee> employees = new List<Employee>();
        public SeriesCollection SalesValues { get; set; }
        public SeriesCollection AttendanceSeries { get; set; }
        public string[] Labels { get; set; }

        // Set the directory path
        private string saversDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savers");

        public HomeUIWindow()
        {
            InitializeComponent();
            LoadData();
            ThemeManager.ApplyTheme(this); // Apply initial theme
            LoadBarChartData();
            LoadAttendanceOverview();
            LoadTotalJobs(); // Load total jobs count
            LoadPresentEmployeesToday(); // Load present employees for today
            LoadTotalCustomers(); // Load total customers count
        }

        // JsonData class
        public class JobCard
        {
            public DateTime JobCardDate { get; set; }
            public string Customer_Name { get; set; }
            public int Quantity { get; set; }
        }

        // Bar chart data class
        public class BarData
        {
            public string Label { get; set; }
            public int Value { get; set; }
        }

        private void LoadPresentEmployeesToday()
        {
            try
            {
                var attendanceManager = new AttendanceManager();
                DateTime today = DateTime.Today;

                // Load today's attendance records
                var todayRecords = attendanceManager.LoadAttendanceWithDateRange(today, today);

                if (todayRecords == null || !todayRecords.Any())
                {
                    Present_Employee.Text = "0";
                    return;
                }

                // Count present employees (excluding absent and missing fingerprint)
                int presentCount = todayRecords.Count(r =>
                    r.CheckIn != "-");

                Present_Employee.Text = presentCount.ToString();
            }
            catch (Exception ex)
            {
                // Handle error gracefully
                Present_Employee.Text = "Error";
                System.Diagnostics.Debug.WriteLine($"Error loading present employees: {ex.Message}");
            }
        }

        private void LoadTotalJobs()
        {
            try
            {
                string jsonFilePath = Path.Combine(saversDirectory, "jobcards.json");

                if (!File.Exists(jsonFilePath))
                {
                    Total_Jobs.Text = "0";
                    return;
                }

                string json = File.ReadAllText(jsonFilePath);
                var jobCards = JsonConvert.DeserializeObject<List<JobCard>>(json);

                if (jobCards == null || !jobCards.Any())
                {
                    Total_Jobs.Text = "0";
                    return;
                }

                int totalJobs = jobCards.Count;

                // Format the number with appropriate suffix (K for thousands, etc.)
                string formattedCount = FormatJobCount(totalJobs);
                Total_Jobs.Text = formattedCount;
            }
            catch (Exception ex)
            {
                // Handle error gracefully
                Total_Jobs.Text = "Error";
                System.Diagnostics.Debug.WriteLine($"Error loading total jobs: {ex.Message}");
            }
        }

        private string FormatJobCount(int count)
        {
            if (count >= 1000000)
                return (count / 1000000.0).ToString("0.#") + "M";
            else if (count >= 1000)
                return (count / 1000.0).ToString("0.#") + "K";
            else
                return count.ToString();
        }

        private void LoadBarChartData()
        {
            string jsonFilePath = Path.Combine(saversDirectory, "jobcards.json");

            if (!File.Exists(jsonFilePath))
                return;

            string json = File.ReadAllText(jsonFilePath);
            var jsonData = JsonConvert.DeserializeObject<List<JobCard>>(json);

            if (jsonData == null || !jsonData.Any())
                return;

            // Last 5 months based on JSON data
            var monthsFromData = jsonData
                .Select(j => j.JobCardDate)
                .Select(d => d.Month + "/" + d.Year) // Month/Year string
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            // Prepare bar chart data
            var filteredData = monthsFromData
                .Select(m =>
                {
                    var parts = m.Split('/');
                    int month = int.Parse(parts[0]);
                    int year = int.Parse(parts[1]);

                    int count = jsonData.Count(j => j.JobCardDate.Month == month && j.JobCardDate.Year == year);

                    return new BarData
                    {
                        Label = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                        Value = count * 20 // scale for rectangle height
                    };
                })
                .ToList();

            BarChart.ItemsSource = filteredData;

            // Example: show job count for the **most recent month** from JSON
            var latestMonth = monthsFromData.Last();
            var latestParts = latestMonth.Split('/');
            int latestMonthNum = int.Parse(latestParts[0]);
            int latestYear = int.Parse(latestParts[1]);
            int latestCount = jsonData.Count(j => j.JobCardDate.Month == latestMonthNum && j.JobCardDate.Year == latestYear);
            Countofjob.Text = latestCount.ToString();
        }

        private void LoadAttendanceOverview()
        {
            try
            {
                var attendanceManager = new AttendanceManager();
                DateTime endDate = DateTime.Today;
                DateTime startDate = endDate.AddDays(-6); // Last 7 days

                var attendanceRecords = attendanceManager.LoadAttendanceWithDateRange(startDate, endDate);

                if (attendanceRecords == null || !attendanceRecords.Any())
                {
                    // Set empty chart
                    AttendanceSeries = new SeriesCollection();
                    Labels = new string[0];
                    DataContext = this;
                    return;
                }

                // Calculate daily attendance statistics
                var dailyStats = new List<DailyAttendanceStats>();

                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dayRecords = attendanceRecords.Where(r => r.Date.Date == date.Date).ToList();

                    int presentCount = dayRecords.Count(r =>
      r.CheckIn != "-");

                    int absentCount = dayRecords.Count(r =>
                        r.CheckIn == "-");

                    // Calculate attendance percentage
                    int totalEmployees = dayRecords.Count;
                    double attendancePercentage = totalEmployees > 0 ? (double)presentCount / totalEmployees * 100 : 0;

                    dailyStats.Add(new DailyAttendanceStats
                    {
                        Date = date,
                        PresentCount = presentCount,
                        AbsentCount = absentCount,
                        TotalEmployees = totalEmployees,
                        AttendancePercentage = attendancePercentage
                    });
                }

                // Create line chart series
                AttendanceSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Present Employees",
                        Values = new ChartValues<int>(dailyStats.Select(d => d.PresentCount)),
                        Stroke = Brushes.DodgerBlue,
                        Fill = new SolidColorBrush(Color.FromArgb(40, 30, 144, 255)),
                        StrokeThickness = 3,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 8,
                        PointForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                        DataLabels = true,
                        LabelPoint = point => $"{point.Y}"
                    },
                    new LineSeries
                    {
                        Title = "Absent Employees",
                        Values = new ChartValues<int>(dailyStats.Select(d => d.AbsentCount)),
                        Stroke = Brushes.Red,
                        Fill = new SolidColorBrush(Color.FromArgb(40, 255, 0, 0)),

                        StrokeThickness = 3,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 8,
                        PointForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                        DataLabels = true,
                        LabelPoint = point => $"{point.Y}"
                    }
                };

                // Create labels (day names with dates)
                Labels = dailyStats.Select(d => $"{d.Date:ddd}\n{d.Date:dd}").ToArray();

                // Update UI
                DataContext = this;
            }
            catch (Exception ex)
            {
                // Handle error gracefully
                AttendanceSeries = new SeriesCollection();
                Labels = new string[0];
                DataContext = this;

                // Optionally log the error
                System.Diagnostics.Debug.WriteLine($"Error loading attendance overview: {ex.Message}");
            }
        }

        private void LoadData()
        {
            employees = EmployeeStorage.Load(); // ✅ FIXED

            RefreshUpcomingBirthdays();
        }


        private void RefreshUpcomingBirthdays()
        {
            UpcomingBirthdays = GetUpcomingBirthdays(31); // Get birthdays within next 30 days
            DataContext = this; // Update UI
        }

        private List<BirthdayInfo> GetUpcomingBirthdays(int daysAhead)
        {
            DateTime today = DateTime.Today;
            DateTime futureDate = today.AddDays(daysAhead);

            return employees
                .Where(e => e.Birthday.HasValue) // Ensure employee has a birthday
                .Select(e =>
                {
                    DateTime birthdayThisYear = new DateTime(today.Year, e.Birthday.Value.Month, e.Birthday.Value.Day);
                    if (birthdayThisYear < today)
                        birthdayThisYear = birthdayThisYear.AddYears(1);

                    int daysLeft = (birthdayThisYear - today).Days;

                    return new BirthdayInfo
                    {
                        Name = e.Name,
                        BirthDate = e.Birthday,
                        DaysLeft = daysLeft
                    };
                })
                .Where(b => b.DaysLeft <= daysAhead)
                .OrderBy(b => b.DaysLeft)
                .ToList();
        }

        private void LoadTotalCustomers()
        {
            try
            {
                string customersFilePath = Path.Combine(saversDirectory, "customers.json");

                if (!File.Exists(customersFilePath))
                {
                    Total_Customers.Text = "0";
                    return;
                }

                string json = File.ReadAllText(customersFilePath);
                var customers = JsonConvert.DeserializeObject<List<Customerinfo>>(json);

                if (customers == null || !customers.Any())
                {
                    Total_Customers.Text = "0";
                    return;
                }

                int totalCustomers = customers.Count;
                Total_Customers.Text = totalCustomers.ToString();
            }
            catch (Exception ex)
            {
                // Handle error gracefully
                Total_Customers.Text = "Error";
                System.Diagnostics.Debug.WriteLine($"Error loading total customers: {ex.Message}");
            }
        }

        // Helper class for daily attendance statistics
        public class DailyAttendanceStats
        {
            public DateTime Date { get; set; }
            public int PresentCount { get; set; }
            public int AbsentCount { get; set; }
            public int TotalEmployees { get; set; }
            public double AttendancePercentage { get; set; }
        }
    }
}