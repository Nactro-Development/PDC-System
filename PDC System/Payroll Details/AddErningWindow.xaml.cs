using Newtonsoft.Json;
using PDC_System.Models;
using PDC_System.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PDC_System
{
    /// <summary>
    /// Interaction logic for AddErningWindow.xaml
    /// </summary>
    public partial class AddErningWindow : Window
    {

        
        public event Action<Earning> Earingsaved;

        public AddErningWindow()
        {
            InitializeComponent();
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            var employees = EmployeeStorage.Load();
            EmployeeCombo.ItemsSource = employees;
            
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeCombo.SelectedItem is Employee emp)
            {
                if (decimal.TryParse(EarningAmount.Text, out decimal Earningamount))
                {
                    // Get the selected date from DatePicker, fallback to today if none selected
                    DateTime selectedDateTime = EarningDate.SelectedDate ?? DateTime.Now;

                    var newEaring = new Earning
                    {
                        EmployeeId = emp.EmployeeId,
                        EmployeeName = emp.Name,
                        EarningAmount = Earningamount,
                        Status = "Earning",
                        EarningDescription = EarningDescription.Text,
                        EarningDate = DateOnly.FromDateTime(selectedDateTime)
                    };

                    Earingsaved?.Invoke(newEaring);
                    this.Close();
                }
                else
                {
                    CustomMessageBox.Show("Please enter valid numbers for earning amount.");
                }
            }
            else
            {
                CustomMessageBox.Show("Please select an employee.");
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

            Close();
        }

        #endregion



    }
}