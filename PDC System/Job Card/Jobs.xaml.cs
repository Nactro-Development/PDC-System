using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;

using PDC_System.Job_Card;
using static PDC_System.QuotationWindow;

namespace PDC_System
{
    public partial class Jobs : UserControl
    {
        private List<JobCard> jobCards = new List<JobCard>();
        private List<Customerinfo> customers = new List<Customerinfo>();

        private readonly string saversFolder = Path.Combine(Directory.GetCurrentDirectory(), "Savers");
        private readonly string jobCardsFile;
        private readonly string customersFile;

        public Jobs()
        {
            InitializeComponent();

            if (!Directory.Exists(saversFolder))
                Directory.CreateDirectory(saversFolder);

            jobCardsFile = Path.Combine(saversFolder, "jobcards.json");
            customersFile = Path.Combine(saversFolder, "customers.json");

            LoadData();
            JobCardDataGrid.Items.Refresh();

            LoadMiniWidgetCheckBoxState();
        }

        private void LoadData()
        {
            if (File.Exists(jobCardsFile))
            {
                jobCards = JsonConvert.DeserializeObject<List<JobCard>>(File.ReadAllText(jobCardsFile));
                jobCards = jobCards.OrderByDescending(j => j.JobCardDate).ToList();
                JobCardDataGrid.ItemsSource = jobCards;
            }

            if (File.Exists(customersFile))
            {
                customers = JsonConvert.DeserializeObject<List<Customerinfo>>(File.ReadAllText(customersFile));
            }
        }

        private void LoadMiniWidgetCheckBoxState()
        {
            bool isChecked = Properties.Settings.Default.MiniWidgetCheckBoxState;

            if (MiniWidgetCheckBox != null)
            {
                MiniWidgetCheckBox.IsChecked = isChecked;
                if (isChecked)
                    ShowMiniWidgetWindow();
            }
        }

        private void MiniWidgetCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (MiniWidgetCheckBox != null)
            {
                bool isChecked = MiniWidgetCheckBox.IsChecked == true;
                Properties.Settings.Default.MiniWidgetCheckBoxState = isChecked;
                Properties.Settings.Default.Save();

                if (isChecked)
                    ShowMiniWidgetWindow();
                else
                    HideMiniWidgetWindow();
            }
        }

        private void ShowMiniWidgetWindow()
        {
            var existingWindow = Application.Current.Windows.OfType<MiniWidgetWindow>().FirstOrDefault();
            if (existingWindow == null)
            {
                var miniWidgetWindow = new MiniWidgetWindow();
                miniWidgetWindow.Show();
            }
        }

        private void HideMiniWidgetWindow()
        {
            var existingWindow = Application.Current.Windows.OfType<MiniWidgetWindow>().FirstOrDefault();
            existingWindow?.Close();
        }

        private void AddJobCard_Click(object sender, RoutedEventArgs e)
        {
            var addJobCardWindow = new AddJobCardWindow(customers);
            if (addJobCardWindow.ShowDialog() == true)
            {
                jobCards.Insert(0, addJobCardWindow.JobCard);
                JobCardDataGrid.Items.Refresh();
                File.WriteAllText(jobCardsFile, JsonConvert.SerializeObject(jobCards));
            }
        }

        private void EditJob_Click(object sender, RoutedEventArgs e)
        {
            // Get the JobCard from the clicked row's DataContext
            var button = sender as Button;
            var selectedJob = button?.DataContext as JobCard;

            if (selectedJob == null)
                selectedJob = JobCardDataGrid.SelectedItem as JobCard;

            if (selectedJob == null)
            {
                NotificationHelper.ShowNotification("Error", "Please select a job card to edit.");
                return;
            }

            var editWindow = new AddJobCardWindow(customers, selectedJob);
            if (editWindow.ShowDialog() == true)
            {
                // The existing JobCard object was updated in place — just save & refresh
                File.WriteAllText(jobCardsFile, JsonConvert.SerializeObject(jobCards));
                JobCardDataGrid.Items.Refresh();
                NotificationHelper.ShowNotification("Success", "Job card updated successfully.");
            }
        }

        private void DeleteJob_Click(object sender, RoutedEventArgs e)
        {
            var selectedJob = JobCardDataGrid.SelectedItem as JobCard;
            if (selectedJob != null)
            {
                var result = CustomMessageBox.Show(
                    "Are you sure you want to delete this job card?",
                    "Delete Job Card",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    Application.Current.MainWindow);

                if (result == MessageBoxResult.Yes)
                {
                    jobCards.Remove(selectedJob);
                    JobCardDataGrid.Items.Refresh();
                    File.WriteAllText(jobCardsFile, JsonConvert.SerializeObject(jobCards));
                    NotificationHelper.ShowNotification("Success", "Job card deleted successfully.");
                }
            }
            else
            {
                NotificationHelper.ShowNotification("Error", "Please select a job card to delete.");
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                CustomMessageBox.Show("Please select a date range.");
                return;
            }

            ApplyFilter();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            NameAutoCompleteBox1.Text = "";
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            JobCardDataGrid.ItemsSource = null;
            JobCardDataGrid.ItemsSource = jobCards;
        }

        private void OpenJobCardButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRow = JobCardDataGrid.SelectedItem as JobCard;
            if (selectedRow != null)
            {
                JobCardView detailsWindow = new JobCardView(selectedRow);
                detailsWindow.Show();
            }
            else
            {
                CustomMessageBox.Show("Please select a job card.");
            }
        }

        private void CheckBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveJobCardsToJson();
        }

        private void SaveJobCardsToJson()
        {
            try
            {
                foreach (var job in jobCards)
                {
                    Console.WriteLine($"Job: {job.Customer_Name}, Seen: {job.IsSeen}");
                }

                File.WriteAllText(jobCardsFile, JsonConvert.SerializeObject(jobCards));

                JobCardDataGrid.ItemsSource = null;
                JobCardDataGrid.ItemsSource = jobCards;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error saving job cards: {ex.Message}");
            }
        }

        private void SearchBox_TextChanged2(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (NameAutoCompleteBox1.Text == null) return;

            string query = NameAutoCompleteBox1.Text.Trim().ToLower();
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate?.AddDays(1).AddTicks(-1);

            var filteredFiles = jobCards.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                filteredFiles = filteredFiles
                    .Where(jc => !string.IsNullOrEmpty(jc.Customer_Name) &&
                                 jc.Customer_Name.ToLower().Contains(query));
            }

            if (startDate != null && endDate != null)
            {
                filteredFiles = filteredFiles
                    .Where(jc => jc.JobCardDate >= startDate && jc.JobCardDate <= endDate);
            }

            var finalList = filteredFiles.OrderByDescending(jc => jc.JobCardDate).ToList();

            JobCardDataGrid.ItemsSource = null;
            JobCardDataGrid.ItemsSource = finalList;
        }

        private void JobCardDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            while (originalSource != null && !(originalSource is CheckBox))
            {
                originalSource = System.Windows.Media.VisualTreeHelper.GetParent(originalSource);
            }

            if (originalSource is CheckBox checkBox)
            {
                checkBox.IsChecked = !checkBox.IsChecked;

                var bindingExpression = checkBox.GetBindingExpression(CheckBox.IsCheckedProperty);
                bindingExpression?.UpdateSource();

                SaveJobCardsToJson();
                e.Handled = true;
            }
        }
    }
}