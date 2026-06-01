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
    public partial class AddLoanWindow : Window
    {
        private string loanFile = "Savers/loan.json";
        
        public event Action<Loan> LoanSaved;

        // ✅ Edit mode: holds the existing loan being edited
        private Loan _editingLoan = null;

        // ✅ Default constructor: Add mode
        public AddLoanWindow()
        {
            InitializeComponent();
            LoadEmployees();
        }

        // ✅ Edit constructor: pre-fills fields with existing loan data
        public AddLoanWindow(Loan existingLoan) : this()
        {
            _editingLoan = existingLoan;
            PDCtitel.Text = "Edit Loan";

            // Pre-fill fields
            LoanAmountBox.Text = existingLoan.LoanAmount.ToString();
            MonthlyPayBox.Text = existingLoan.MonthlyPay.ToString();
            LoanDatePicker.SelectedDate = existingLoan.LoanDate;

            // Pre-select the employee in the ComboBox and disable it (can't change employee in edit)
            Loaded += (s, e) =>
            {
                foreach (Employee emp in EmployeeCombo.Items)
                {
                    if (emp.EmployeeId == existingLoan.EmployeeId)
                    {
                        EmployeeCombo.SelectedItem = emp;
                        break;
                    }
                }
                EmployeeCombo.IsEnabled = false;
            };
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
                var existingLoans = File.Exists(loanFile)
                    ? JsonConvert.DeserializeObject<List<Loan>>(File.ReadAllText(loanFile))
                    : new List<Loan>();

                // ✅ Edit mode: update existing loan
                if (_editingLoan != null)
                {
                    if (decimal.TryParse(LoanAmountBox.Text, out decimal loanAmt) &&
                        decimal.TryParse(MonthlyPayBox.Text, out decimal monthly))
                    {
                        var loanToUpdate = existingLoans.FirstOrDefault(l => l.LoanId == _editingLoan.LoanId);
                        if (loanToUpdate != null)
                        {
                            loanToUpdate.LoanAmount = loanAmt;
                            loanToUpdate.MonthlyPay = monthly;
                            loanToUpdate.LoanDate = LoanDatePicker.SelectedDate ?? _editingLoan.LoanDate;

                            File.WriteAllText(loanFile, JsonConvert.SerializeObject(existingLoans, Formatting.Indented));
                            LoanSaved?.Invoke(loanToUpdate);
                            this.Close();
                        }
                    }
                    else
                    {
                        CustomMessageBox.Show("Please enter valid numbers for loan and monthly pay.");
                    }
                    return;
                }

                // ✅ Add mode: block if employee already has a loan
                bool hasAnyLoan = existingLoans.Any(l => l.EmployeeId == emp.EmployeeId);
                if (hasAnyLoan)
                {
                    CustomMessageBox.Show("This employee already has a loan. Loan End and Can Create Loan");
                    return;
                }

                var selectedDate = LoanDatePicker.SelectedDate ?? DateTime.Now;

                if (decimal.TryParse(LoanAmountBox.Text, out decimal newLoanAmt) &&
                    decimal.TryParse(MonthlyPayBox.Text, out decimal newMonthly))
                {
                    var newLoan = new Loan
                    {
                        EmployeeId = emp.EmployeeId,
                        Name = emp.Name,
                        LoanAmount = newLoanAmt,
                        MonthlyPay = newMonthly,
                        LoanDate = selectedDate,
                        Status = "Active"
                    };

                    existingLoans.Add(newLoan);
                    File.WriteAllText(loanFile, JsonConvert.SerializeObject(existingLoans, Formatting.Indented));

                    LoanSaved?.Invoke(newLoan);
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

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        #endregion
    }
}