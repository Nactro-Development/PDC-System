using PDC_System.Models;
using Newtonsoft.Json;
using Newtonsoft.Json;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PDC_System.Payroll_Details
{
    /// <summary>
    /// Interaction logic for PayrollDetailsWindow.xaml
    /// </summary>
    public partial class PayrollDetailsWindow : UserControl
    {

        private string oldLoanFile = "Savers/OldLoans.json";
        private string loanHistoryFile = "Savers/LoanHistory.json";
        private List<EPFHistory> epfHistoryList = new List<EPFHistory>();

        private string loanFile = "Savers/loan.json";
        private string deducationanFile = "Savers/Deducation.json";
        private string earningFile = "Savers/Earning.json";
        private string EPFFile = "Savers/EPF.json";
        private List<Deducation> dedecations = new List<Deducation>();
        private List<Earning> earnings = new List<Earning>();
        private List<Loan> loans = new List<Loan>();
        private List<EPF> etfs = new List<EPF>();
        public PayrollDetailsWindow()
        {
            InitializeComponent();
            LoadLoans();
            LoadOldLoans();

            UpdateDeductionSummary();
            UpdateEarningSummary();


            if (File.Exists("Savers/EPFHistory.json"))
            {
                string json = File.ReadAllText("Savers/EPFHistory.json");
                epfHistoryList = JsonConvert.DeserializeObject<List<EPFHistory>>(json)
                                 ?? new List<EPFHistory>();

                LoadEPFGrid(epfHistoryList);
            }




        }

        private void LoadEPFGrid(List<EPFHistory> list)
        {
            var data = list.Select(h => new
            {
                EmployeeId = h.EmployeeId,
                EmployeeName = h.EmployeeName,
                Month = h.Month,
                BasicSalary = h.BasicSalary,
                // Use property names that match the XAML bindings
                EmployeeAmount = h.EmployeeAmount,
                EmployerAmount = h.EmployerAmount,
                Total = h.EmployeeAmount + h.EmployerAmount
            }).ToList();

            EPFCombinedGrid.ItemsSource = data;

            TotalEmployeeTxt.Text = list.Sum(x => x.EmployeeAmount).ToString("N2");
            TotalEmployerTxt.Text = list.Sum(x => x.EmployerAmount).ToString("N2");
            TotalEPFTxt.Text = list.Sum(x => x.EmployeeAmount + x.EmployerAmount).ToString("N2");
        }


       
            private void EditETFs_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new EditEpfWindow
            {
                Owner = Window.GetWindow(this)
            };

            var result = dlg.ShowDialog();
            if (result == true)
            {
                // Settings changed and saved. Reload EPF data from disk and refresh UI.

                try
                {
                    // 1) Reload EPF records (Savers/EPF.json) used by EPFGrid
                    if (File.Exists(EPFFile))
                    {
                        string epfJson = File.ReadAllText(EPFFile);
                        etfs = JsonConvert.DeserializeObject<List<EPF>>(epfJson) ?? new List<EPF>();
                        EPFGrid.ItemsSource = etfs;
                        EPFGrid.Items.Refresh();
                    }
                    else
                    {
                        etfs = new List<EPF>();
                        EPFGrid.ItemsSource = etfs;
                        EPFGrid.Items.Refresh();
                    }

                    // 2) Reload EPF history (Savers/EPFHistory.json) used by EPFCombinedGrid & totals
                    string epfHistoryPath = "Savers/EPFHistory.json";
                    if (File.Exists(epfHistoryPath))
                    {
                        string json = File.ReadAllText(epfHistoryPath);
                        epfHistoryList = JsonConvert.DeserializeObject<List<EPFHistory>>(json) ?? new List<EPFHistory>();
                    }
                    else
                    {
                        epfHistoryList = new List<EPFHistory>();
                    }

                    // Repopulate combined grid and totals
                    LoadEPFGrid(epfHistoryList);

                    // 3) Also update displayed percentages (if you want to show current settings)
                    // Keep the percent display as before but also show numeric totals from history
                    try
                    {
                        // if you prefer to show percent instead of totals in those labels, adjust accordingly.
                        // Here we set both: percent text (for settings) and totals are already set by LoadEPFGrid.
                        // Example: show percent in small label (reuse existing TotalEmployeeTxt/TotalEmployerTxt if desired)
                        TotalEmployeeTxt.Text = Properties.Settings.Default.EPFEmployee.ToString("N2") + " %";
                        TotalEmployerTxt.Text = Properties.Settings.Default.EPFEmployer.ToString("N2") + " %";
                    }
                    catch
                    {
                        // ignore formatting errors
                    }
                }
                catch (Exception ex)
                {
                    // Non-fatal — user should be notified if refresh fails
                    CustomMessageBox.Show($"EPF values saved but failed to refresh UI: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }


        private void FilterEPF_Click(object sender, RoutedEventArgs e)
        {
            DateTime? start = StartDatePicker.SelectedDate;
            DateTime? end = EndDatePicker.SelectedDate;
            string empId = SearchEmployeeId.Text.Trim();

            var filtered = epfHistoryList;

            // Filter by date range
            if (start != null && end != null)
            {
                filtered = filtered.Where(x =>
                {
                    DateTime monthDate = DateTime.Parse(x.Month); // Month = "2025-01"
                    return monthDate >= start && monthDate <= end;
                }).ToList();
            }

            // Filter by employee ID
            if (!string.IsNullOrEmpty(empId))
            {
                filtered = filtered.Where(x => x.EmployeeId.Contains(empId, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            LoadEPFGrid(filtered);
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            SearchEmployeeId.Clear();

            LoadEPFGrid(epfHistoryList);
        }


        private void LoadLoans()
        {
            if (File.Exists(loanFile))
            {
                string json = File.ReadAllText(loanFile);
                loans = JsonConvert.DeserializeObject<List<Loan>>(json) ?? new List<Loan>();
            }
            LoanGrid.ItemsSource = loans;

            UpdateLoanSummary(); // ✅ ADD THIS LINE

            if (File.Exists(deducationanFile))
            {
                string json = File.ReadAllText(deducationanFile);
                dedecations = JsonConvert.DeserializeObject<List<Deducation>>(json) ?? new List<Deducation>();
            }
            EDeductionGrid.ItemsSource = dedecations;

            if (File.Exists(earningFile))
            {
                string json = File.ReadAllText(earningFile);
                earnings = JsonConvert.DeserializeObject<List<Earning>>(json) ?? new List<Earning>();
                EEarningGrid.ItemsSource = earnings;
            }
            if (File.Exists(earningFile))
            {
                string json = File.ReadAllText(EPFFile);
                etfs = JsonConvert.DeserializeObject<List<EPF>>(json) ?? new List<EPF>();
                EPFGrid.ItemsSource = etfs;
            }
        }

        private void AddLoan_Click(object sender, RoutedEventArgs e)
        {
            AddLoanWindow addLoan = new AddLoanWindow();
            addLoan.LoanSaved += new Action<Loan>(AddLoan_LoanSaved);
            addLoan.ShowDialog();
        }
        private void AddErning(object sender, RoutedEventArgs e)
        {
            AddErningWindow addearining = new AddErningWindow();
            addearining.Earingsaved += new Action<Earning>(AddLoan_ErningSaved);
            addearining.ShowDialog();
        }
        private void AddDeducation(object sender, RoutedEventArgs e)
        {
            AddDeducationWinodw adddeducation = new AddDeducationWinodw();
            adddeducation.DeducationSaved += new Action<Deducation>(AddLoan_DeducationSaved);
            adddeducation.ShowDialog();
        }
        private void AddEPF(object sender, RoutedEventArgs e)
        {
            AddEPFWindow adddeducation = new AddEPFWindow();
            adddeducation.ETFsaved += new Action<EPF>(AddLoan_EPFSaved);
            adddeducation.ShowDialog();
        }


        private void AddLoan_LoanSaved(Loan newLoan)
        {
            loans.Add(newLoan);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(loanFile));
            File.WriteAllText(loanFile, JsonConvert.SerializeObject(loans, Newtonsoft.Json.Formatting.Indented));
            LoanGrid.Items.Refresh();
            UpdateLoanSummary(); // ✅ ADD
        }
        private void AddLoan_EPFSaved(EPF newEPF)
        {
            etfs.Add(newEPF);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(EPFFile));
            File.WriteAllText(EPFFile, JsonConvert.SerializeObject(etfs, Newtonsoft.Json.Formatting.Indented));
            EPFGrid.Items.Refresh();
        }
        private void AddLoan_ErningSaved(Earning newEarning)
        {
            earnings.Add(newEarning);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(earningFile));
            File.WriteAllText(earningFile, JsonConvert.SerializeObject(earnings, Newtonsoft.Json.Formatting.Indented));
            EEarningGrid.Items.Refresh();
        }
        private void AddLoan_DeducationSaved(Deducation newDeducation)
        {
            dedecations.Add(newDeducation);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(deducationanFile));
            File.WriteAllText(deducationanFile, JsonConvert.SerializeObject(dedecations, Newtonsoft.Json.Formatting.Indented));
            EDeductionGrid.Items.Refresh();
        }

        private void OpenLoanHistory(object sender, RoutedEventArgs e)
        {
            if (LoanGrid.SelectedItem is Loan ln)
            {
                var historyWindow = new LoanHistoryWindow(ln.EmployeeId, ln.Name);
                historyWindow.Closed += (s, args) =>
                {
                    // ✅ Refresh the loan grid after history window closes
                    LoadLoans(); // This will recalculate Remeining property
                };
                historyWindow.ShowDialog();
            }
            else
            {

                CustomMessageBox.Show("Select a loan first!");
            }
        }



        private void EndLoan_Click(object sender, RoutedEventArgs e)
        {
            if (LoanGrid.SelectedItem is not Loan selectedLoan)
            {
                CustomMessageBox.Show("Select a loan to end.");
                return;
            }

            // ✔ Allow ending only if status = Finished
            if (selectedLoan.Status != "Finished")
            {
                CustomMessageBox.Show("You can only end this loan after the status is set to 'Finished'.");
                return;
            }

            if (CustomMessageBox.Show(
                "Loan has no remaining amount. Do you want to finish this loan?",
                "Confirm Finish",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            // 🔥 CHANGE 2: Set Status to Finished
            selectedLoan.Status = "Finished";

            // 1. Load old loans
            List<Loan> oldLoans = new List<Loan>();
            if (File.Exists(oldLoanFile))
            {
                string jsonOld = File.ReadAllText(oldLoanFile);
                oldLoans = JsonConvert.DeserializeObject<List<Loan>>(jsonOld) ?? new List<Loan>();
            }

            // 2. Move loan to old loans
            oldLoans.Add(selectedLoan);
            File.WriteAllText(oldLoanFile, JsonConvert.SerializeObject(oldLoans, Formatting.Indented));

            // 3. Remove from active loan.json
            loans.Remove(selectedLoan);
            File.WriteAllText(loanFile, JsonConvert.SerializeObject(loans, Formatting.Indented));

            // 4. Remove loan history of this employee
            if (File.Exists(loanHistoryFile))
            {
                string jsonHistory = File.ReadAllText(loanHistoryFile);
                var historyList = JsonConvert.DeserializeObject<List<LoanHistoryService.LoanHistoryEntry>>(jsonHistory)
                                  ?? new List<LoanHistoryService.LoanHistoryEntry>();

                // 🔥 CHANGE 3: Remove only this employee's loan history
                historyList = historyList.Where(h => h.EmployeeId != selectedLoan.EmployeeId).ToList();

                File.WriteAllText(loanHistoryFile, JsonConvert.SerializeObject(historyList, Formatting.Indented));
            }

            LoanGrid.Items.Refresh();
            UpdateLoanSummary(); // ✅ ADD
            CustomMessageBox.Show("Loan successfully marked as FINISHED.");
        }

        private void DeleteEPF_Click(object sender, RoutedEventArgs e)
        {
            if (EPFGrid.SelectedItem is not EPF selectedEPF)
            {
                CustomMessageBox.Show("Select an EPF record to delete!");
                return;
            }

            if (CustomMessageBox.Show(
                "Are you sure you want to delete this EPF record?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            // Remove from list
            etfs.Remove(selectedEPF);

            // Save updated file
            File.WriteAllText(EPFFile, JsonConvert.SerializeObject(etfs, Formatting.Indented));

            // Refresh grid 
            EPFGrid.Items.Refresh();

            CustomMessageBox.Show("EPF record deleted successfully!");
        }




        private void LoadOldLoans()
        {
            if (File.Exists(oldLoanFile))
            {
                string json = File.ReadAllText(oldLoanFile);
                var oldLoans = JsonConvert.DeserializeObject<List<Loan>>(json);
                OldLoanGrid.ItemsSource = oldLoans;
            }
        }


        private void DeleteDeduction_Click(object sender, RoutedEventArgs e)
        {
            if (EDeductionGrid.SelectedItem is not Deducation selected)
            {
                CustomMessageBox.Show("Select a deduction to delete!");
                return;
            }

            if (CustomMessageBox.Show(
                "Are you sure you want to delete this deduction?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            dedecations.Remove(selected);

            File.WriteAllText(deducationanFile,
                JsonConvert.SerializeObject(dedecations, Formatting.Indented));

            EDeductionGrid.Items.Refresh();

            CustomMessageBox.Show("Deduction deleted successfully!");
            UpdateEarningSummary();
        }


        private void DeleteEarning_Click(object sender, RoutedEventArgs e)
        {
            if (EEarningGrid.SelectedItem is not Earning selected)
            {
                CustomMessageBox.Show("Select an earning to delete!");
                return;
            }

            if (CustomMessageBox.Show(
                "Are you sure you want to delete this earning?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            earnings.Remove(selected);

            File.WriteAllText(earningFile,
                JsonConvert.SerializeObject(earnings, Formatting.Indented));

            EEarningGrid.Items.Refresh();

            CustomMessageBox.Show("Earning deleted successfully!");
            UpdateEarningSummary();
        }


        private void EditLoan_Click(object sender, RoutedEventArgs e)
        {
            // Get the loan from the clicked row's DataContext
            var button = sender as Button;
            var selectedLoan = button?.DataContext as Loan;

            if (selectedLoan == null)
            {
                CustomMessageBox.Show("Select a loan to edit.");
                return;
            }

            var editWindow = new AddLoanWindow(selectedLoan);
            editWindow.LoanSaved += (updatedLoan) =>
            {
                // Reload from file to reflect updated data
                LoadLoans();
            };
            editWindow.ShowDialog();
        }




        private void UpdateDeductionSummary()
        {
            var today = DateTime.Today;

            var todayTotal = dedecations
                .Where(x => x.DeducationDate.ToDateTime(TimeOnly.MinValue).Date == today)
                .Sum(x => x.DeducationAmount);

            var total = dedecations.Sum(x => x.DeducationAmount);

            TodayDeductionTxt.Text = $"LKR {todayTotal:N2}";
            TotalDeductionTxt.Text = $"LKR {total:N2}";
        }


        private void UpdateEarningSummary()
        {
            var today = DateTime.Today;

            var todayTotal = earnings
                .Where(x => x.EarningDate.ToDateTime(TimeOnly.MinValue).Date == today)
                .Sum(x => x.EarningAmount);

            var total = earnings.Sum(x => x.EarningAmount);

            TodayEarningTxt.Text = $"LKR {todayTotal:N2}";
            TotalEarningTxt.Text = $"LKR {total:N2}";
        }



        private void UpdateLoanSummary()
        {
            double totalLoan = loans.Sum(x => (double)x.LoanAmount);
            double totalRemaining = loans.Sum(x => (double)x.Remeining);

            TotalLoanAmountTxt.Text = $"LKR {totalLoan:N2}";
            TotalRemainingLoanTxt.Text = $"LKR {totalRemaining:N2}"; // ✅ ADD THIS LINE
        }


        #region Paysheet

        #endregion
    }
}
