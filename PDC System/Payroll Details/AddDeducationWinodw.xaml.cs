using Newtonsoft.Json;
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
    /// Interaction logic for AddDeducationWinodw.xaml
    /// </summary>
    public partial class AddDeducationWinodw : Window
    {

       
        public event Action<Deducation> DeducationSaved;
        public AddDeducationWinodw()
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
                var selectedDate = DeducationPicker.SelectedDate ?? DateTime.Now;

                if (decimal.TryParse(DeducationAmount.Text, out decimal deducationamount))
                {
                    var newLoan = new Deducation
                    {
                        EmployeeId = emp.EmployeeId,
                        EmployeeName = emp.Name,
                        Status = "Deducation",
                        DeducationDescription = DeducationDescription.Text,
                        DeducationAmount = deducationamount,
                        DeducationDate = DateOnly.FromDateTime(selectedDate), // FIXED LINE
                    };

                    DeducationSaved?.Invoke(newLoan);
                    this.Close();
                }
                else
                {
                    CustomMessageBox.Show("Please enter valid numbers for loan and monthly pay.");
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
