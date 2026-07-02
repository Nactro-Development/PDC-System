using Newtonsoft.Json;
using PDC_System.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PDC_System
{
    public partial class EmployeeWindow : System.Windows.Controls.UserControl
    {
        private List<Employee> employees = new List<Employee>();
        private List<Employee> filteredEmployees = new List<Employee>();

        // Set the path to the Savers folder in the current directory
        private string saversFolder = Path.Combine(Directory.GetCurrentDirectory(), "Savers");
    

        public EmployeeWindow()
        {
            InitializeComponent();
            LoadData();
            ConvertJsonToDat();
        }

        private void LoadData()
        {
            string dataFile = Path.Combine(saversFolder, "employees.dat");

            if (File.Exists(dataFile))
            {
                byte[] encrypted = File.ReadAllBytes(dataFile);

                byte[] decrypted = ProtectedData.Unprotect(
                    encrypted,
                    null,
                    DataProtectionScope.LocalMachine);


                string json = Encoding.UTF8.GetString(decrypted);

                employees = JsonConvert.DeserializeObject<List<Employee>>(json)
                            ?? new List<Employee>();
            }

            filteredEmployees = new List<Employee>(employees);
            EmployeeDataGrid.ItemsSource = filteredEmployees;
        }



        private void SaveData()
        {
            string json = JsonConvert.SerializeObject(
                employees,
                Formatting.Indented);

            byte[] data = Encoding.UTF8.GetBytes(json);

            byte[] encrypted = ProtectedData.Protect(
                data,
                null,
                DataProtectionScope.LocalMachine);

            string dataFile = Path.Combine(saversFolder, "employees.dat");

            File.WriteAllBytes(dataFile, encrypted);
        }



        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            var addEmployeeWindow = new EmployeeAddData();  // NO PARAM

            if (addEmployeeWindow.ShowDialog() == true)
            {
                employees.Add(addEmployeeWindow.Employee);
                ApplyFilter();
                SaveData();
            }
        }


        private async void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = EmployeeDataGrid.SelectedItem as Employee;




            var hikvision = new HikvisionService(
   "192.168.1.15",
            "admin",
            "priyanthaD@8");

            bool success = await hikvision.DeleteUser(
             selectedEmployee.EmployeeId
            );

            if (!success)
            {
                MessageBox.Show("Failed to create user on terminal.");
                return;
            }






            if (selectedEmployee != null)
            {
                MessageBoxResult result = CustomMessageBox.Show(
                    $"Are you sure you want to delete employee '{selectedEmployee.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    employees.Remove(selectedEmployee);
                    ApplyFilter();
                    SaveData();
                }
            }
            else
            {
                CustomMessageBox.Show(
                    "Please select an employee to delete.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }




        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string query = SearchBox.Text.Trim().ToLower();
            filteredEmployees = employees
                .Where(emp => emp.Name.ToLower().Contains(query) || emp.NID.ToLower().Contains(query))
                .ToList();

            EmployeeDataGrid.ItemsSource = null;
            EmployeeDataGrid.ItemsSource = filteredEmployees;
        }


        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            var emp = (sender as Button)?.Tag as Employee;
            if (emp == null) return;

            // Pass employee to edit window
            var editWindow = new EmployeeAddData(emp);

            if (editWindow.ShowDialog() == true)
            {
                // Update employee in the list
                var index = employees.FindIndex(x => x.EmployeeId == emp.EmployeeId);

                if (index != -1)
                {
                    employees[index] = editWindow.Employee;
                }

                ApplyFilter();

                // Save JSON
                SaveData();
            }
        }


        private void ConvertJsonToDat()
        {
            string jsonFile = Path.Combine(saversFolder, "employee.json");
            string datFile = Path.Combine(saversFolder, "employees.dat");

            if (!File.Exists(jsonFile))
            {
                MessageBox.Show("employee.json file not found!");
                return;
            }

            // JSON file read
            string json = File.ReadAllText(jsonFile);

            // Encrypt
            byte[] data = Encoding.UTF8.GetBytes(json);

            byte[] encrypted = ProtectedData.Protect(
                data,
                null,
                DataProtectionScope.CurrentUser);

            // Save as .dat
            File.WriteAllBytes(datFile, encrypted);

            MessageBox.Show("Data converted successfully!");
        }

    }
}
