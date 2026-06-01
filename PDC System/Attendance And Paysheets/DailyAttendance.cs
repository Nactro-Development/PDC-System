using Newtonsoft.Json;
using PDC_System.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PDC_System
{
    public class DaliyAttendance
    {
        private static AttendanceDatabase _db;


        private static List<Employee> LoadEmployees(string baseFolder)
        {
            string file = Path.Combine(baseFolder, "employees.dat");

            if (!File.Exists(file))
                return new List<Employee>();

            byte[] encrypted = File.ReadAllBytes(file);

            byte[] decrypted = ProtectedData.Unprotect(
                encrypted,
                null,
                DataProtectionScope.CurrentUser);

            string json = Encoding.UTF8.GetString(decrypted);

            return JsonConvert.DeserializeObject<List<Employee>>(json)
                   ?? new List<Employee>();
        }




        public static async Task CheckTodayAttendance()
        {
            if (!Properties.Settings.Default.SendDailyReport)
                return; // Exit if the setting is false

            string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
            string saversFolder = Path.Combine(baseFolder, "Savers");
            string logPath = Path.Combine(saversFolder, "AttendanceMailLog.txt");
            string employeePath = Path.Combine(saversFolder, "employee.json");

            // Initialize SQLite database instead of reading ivms.json
            _db = new AttendanceDatabase(saversFolder);

            // Read the log file to check if email was already sent
            HashSet<string> sentDates = new HashSet<string>();
            if (File.Exists(logPath))
                sentDates = new HashSet<string>(File.ReadAllLines(logPath));

            DateTime today = DateTime.Today;
            string todayString = today.ToString("yyyy-MM-dd");

            if (sentDates.Contains(todayString))
            {
                // Already sent today
                return;
            }

            var employees = LoadEmployees(saversFolder);

            // Get fingerprint data from SQLite database instead of JSON file
            var ivmsData = _db.GetFingerprintData();   // returns List<FingerprintData>

            // Build HTML email body (same as before)
            var htmlBody = new StringBuilder();
            htmlBody.AppendLine("<!DOCTYPE html>");
            htmlBody.AppendLine("<html>");
            htmlBody.AppendLine("<head>");
            htmlBody.AppendLine("<style>");
            htmlBody.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background-color: #f4f4f4; }");
            htmlBody.AppendLine(".container { background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            htmlBody.AppendLine("h2 { color: #333; border-bottom: 2px solid #1469FF; padding-bottom: 10px; }");
            htmlBody.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            htmlBody.AppendLine("th { background-color: #1469FF; color: white; padding: 12px; text-align: left; }");
            htmlBody.AppendLine("td { padding: 10px; border-bottom: 1px solid #ddd; }");
            htmlBody.AppendLine("tr:hover { background-color: #f5f5f5; }");
            htmlBody.AppendLine(".present { color: #4CAF50; font-weight: bold; }");
            htmlBody.AppendLine(".absent { color: #f44336; font-weight: bold; }");
            htmlBody.AppendLine(".footer { margin-top: 20px; font-size: 12px; color: #666; text-align: center; }");
            htmlBody.AppendLine("</style>");
            htmlBody.AppendLine("</head>");
            htmlBody.AppendLine("<body>");
            htmlBody.AppendLine("<div class='container'>");
            htmlBody.AppendLine($"<h2>Daily Attendance Report - {today:dddd, MMMM dd, yyyy}</h2>");
            htmlBody.AppendLine("<table>");
            htmlBody.AppendLine("<tr><th>Employee ID</th><th>Employee Name</th><th>Status</th><th>First Check-in Time</th></tr>");

            foreach (var emp in employees)
            {
                var todayRecords = ivmsData
                   .Where(x => x.EmployeeID == emp.EmployeeId && x.DateTime.Date == today)
                   .OrderBy(x => x.DateTime)
                   .ToList();

                if (todayRecords.Any())
                {
                    var firstCheckIn = todayRecords.First().DateTime;
                    htmlBody.AppendLine("<tr>");
                    htmlBody.AppendLine($"<td>{emp.EmployeeId}</td>");
                    htmlBody.AppendLine($"<td>{emp.Name}</td>");
                    htmlBody.AppendLine($"<td class='present'>Present ✅</td>");
                    htmlBody.AppendLine($"<td>{firstCheckIn:hh:mm tt}</td>");
                    htmlBody.AppendLine("</tr>");
                }
                else
                {
                    htmlBody.AppendLine("<tr>");
                    htmlBody.AppendLine($"<td>{emp.EmployeeId}</td>");
                    htmlBody.AppendLine($"<td>{emp.Name}</td>");
                    htmlBody.AppendLine($"<td class='absent'>Employee not coming ❌</td>");
                    htmlBody.AppendLine($"<td>-</td>");
                    htmlBody.AppendLine("</tr>");
                }
            }

            htmlBody.AppendLine("</table>");
            htmlBody.AppendLine("<div class='footer'>");
            htmlBody.AppendLine($"<p>Generated automatically by PDC System on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            htmlBody.AppendLine("</div>");
            htmlBody.AppendLine("</div>");
            htmlBody.AppendLine("</body>");
            htmlBody.AppendLine("</html>");

            var mailService = new MailService();
            string email = Properties.Settings.Default.SystemAppEmail;
            string recipientEmail = email;
            List<string> ccList = new List<string>();
            string subject = $"Daily Attendance Report - {today:yyyy-MM-dd}";
            string body = htmlBody.ToString();

            bool result = await mailService.SendEmailAsync(recipientEmail, ccList, subject, body);

            // Log the result
            if (!Directory.Exists(saversFolder))
                Directory.CreateDirectory(saversFolder);

            using (var writer = new StreamWriter(logPath, true))
            {
                if (result)
                {
                    writer.WriteLine(todayString); // Mark as sent

                    CustomMessageBox.Show("✅ Email sent successfully!", "Mail Service", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    writer.WriteLine($"{todayString}-FAILED"); // Mark as failed
                    CustomMessageBox.Show("❌ Failed to send email!", "Mail Service", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class IVMSRecord
    {
        public string EmployeeID { get; set; }
        public DateTime DateTime { get; set; }
    }
}