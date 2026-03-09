using Newtonsoft.Json;
using PDC_System.Customer;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PDC_System
{
    /// <summary>
    /// Interaction logic for Contact.xaml
    /// </summary>
    public partial class Customers : UserControl
    {
        private List<Customerinfo> customers = new List<Customerinfo>();

        private readonly string saversFolder;
        private readonly string jsonFilePath;
        private readonly string jsonFilePath2;

        public Customers()
        {
            InitializeComponent();

            saversFolder = Path.Combine(Directory.GetCurrentDirectory(), "Savers");
            if (!Directory.Exists(saversFolder))
            {
                Directory.CreateDirectory(saversFolder);
            }

            jsonFilePath = Path.Combine(saversFolder, "customers.json");
            jsonFilePath2 = Path.Combine(saversFolder, "Outsource.json");

            LoadData();
        }

        private void LoadData()
        {
            if (File.Exists(jsonFilePath))
            {
                customers = JsonConvert.DeserializeObject<List<Customerinfo>>(File.ReadAllText(jsonFilePath));
                CustomerDataGrid.ItemsSource = customers;

                UpdateCustomerSummary();
            }
        }

        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var addCustomerWindow = new AddCustomerWindow();
            if (addCustomerWindow.ShowDialog() == true)
            {
                customers.Add(addCustomerWindow.Customer);
                CustomerDataGrid.Items.Refresh();
                UpdateCustomerSummary();
                File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(customers, Formatting.Indented));
            }
        }

        private void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            var selectedCustomer = CustomerDataGrid.SelectedItem as Customerinfo;
            if (selectedCustomer == null)
            {
                CustomMessageBox.Show("Please select a customer to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning, Application.Current.MainWindow);
                return;
            }

            var editWindow = new AddCustomerWindow(selectedCustomer);
            if (editWindow.ShowDialog() == true)
            {
                // Update the existing customer's data in-place
                int index = customers.IndexOf(selectedCustomer);
                customers[index] = editWindow.Customer;

                CustomerDataGrid.Items.Refresh();
                UpdateCustomerSummary();
                File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(customers, Formatting.Indented));
            }
        }

        private void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            var selectedCustomer = CustomerDataGrid.SelectedItem as Customerinfo;
            if (selectedCustomer != null)
            {
                var result = CustomMessageBox.Show(
                    "Are you sure you want to delete this customer?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    Application.Current.MainWindow);

                if (result == MessageBoxResult.Yes)
                {
                    customers.Remove(selectedCustomer);
                    CustomerDataGrid.Items.Refresh();
                    UpdateCustomerSummary();
                    File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(customers, Formatting.Indented));
                }
            }
        }

        private void UpdateCustomerSummary()
        {
            if (customers == null) return;

            int totalCustomers = customers.Count;

            int totalCompanies = customers
                .Count(x => x.Type != null &&
                       x.Type.ToLower().Contains("company"));

            TotalCustomersTxt.Text = totalCustomers.ToString();
            TotalCompaniesTxt.Text = totalCompanies.ToString();
        }
    }
}