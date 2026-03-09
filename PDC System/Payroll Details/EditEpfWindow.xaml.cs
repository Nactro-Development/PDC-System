using Newtonsoft.Json;
using PDC_System.Models;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;


using System;

using System.Collections.Generic;

namespace PDC_System.Payroll_Details
{
    public partial class EditEpfWindow : Window
    {
        public EditEpfWindow()
        {
            InitializeComponent();
            LoadValues();
        }

        private void LoadValues()
        {
            try
            {
                // Read from settings (ensure these settings exist in __Settings.settings__)
                var emp = Properties.Settings.Default.EPFEmployee;
                var er = Properties.Settings.Default.EPFEmployer;

                EmployeeTextBox.Text = emp.ToString("N2", CultureInfo.InvariantCulture);
                EmployerTextBox.Text = er.ToString("N2", CultureInfo.InvariantCulture);
            }
            catch
            {
                EmployeeTextBox.Text = "0.00";
                EmployerTextBox.Text = "0.00";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(EmployeeTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var emp))
            {
                MessageBox.Show("Enter a valid number for Employee EPF.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(EmployerTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var er))
            {
                MessageBox.Show("Enter a valid number for Employer EPF.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save to user settings and persist
            Properties.Settings.Default.EPFEmployee = (decimal)emp;
            Properties.Settings.Default.EPFEmployer = (decimal)er;
            Properties.Settings.Default.Save();

            // Recalculate and update EPF JSON records for existing employees
            try
            {
                UpdateEPFJsonFiles(Properties.Settings.Default.EPFEmployee, Properties.Settings.Default.EPFEmployer);
            }
            catch (Exception ex)
            {
                // non-fatal: inform user but still close the dialog
                MessageBox.Show($"Saved settings but failed to update EPF records: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Recalculates EmployeeAmount and EmployerAmount for every EPF record
        /// stored in the Savers EPF JSON file(s) and writes the updated list back.
        /// Handles both "Savers/EPF.json" and "Savers/epf.json" (some files in repo use different casing).
        /// </summary>
        private void UpdateEPFJsonFiles(decimal empPercent, decimal erPercent)
        {
            string[] possibleFiles = new[] { "Savers/EPF.json", "Savers/epf.json" };

            foreach (var file in possibleFiles)
            {
                if (!File.Exists(file))
                    continue;

                var json = File.ReadAllText(file);
                var list = JsonConvert.DeserializeObject<List<EPF>>(json) ?? new List<EPF>();

                bool changed = false;

                foreach (var e in list)
                {
                    // Recalculate using BasicSalary. If BasicSalary is not set, skip that record.
                    // Keep calculation consistent with AddEPFWindow and SettingsWindow.UpdateAllEPFRecords.
                    decimal basic = e.BasicSalary;
                    decimal newEmpAmount = Math.Round((basic * empPercent) / 100, 2);
                    decimal newErAmount = Math.Round((basic * erPercent) / 100, 2);
                    decimal newTotal = newEmpAmount + newErAmount;

                    if (e.EmployeeAmount != newEmpAmount || e.EmployerAmount != newErAmount || e.Total != newTotal)
                    {
                        e.EmployeeAmount = newEmpAmount;
                        e.EmployerAmount = newErAmount;
                        e.Total = newTotal;
                        changed = true;
                    }
                }

                if (changed)
                {
                    // Ensure directory exists (in case file path was provided without directory present)
                    var dir = Path.GetDirectoryName(file);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    File.WriteAllText(file, JsonConvert.SerializeObject(list, Formatting.Indented));
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
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