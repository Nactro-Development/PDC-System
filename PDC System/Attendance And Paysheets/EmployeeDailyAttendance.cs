using Google.Apis.PeopleService.v1.Data;
using Newtonsoft.Json;
using PDC_System.Models;
using PDC_System.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PDC_System
{
    public class EmployeeDailyAttendance
    {



        public static async Task CheckTodayAttendanceAsync()
        {
            if (!Properties.Settings.Default.SendAttendanceEmails)
                return;

            string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
            string saversFolder = Path.Combine(baseFolder, "Savers");
            Directory.CreateDirectory(saversFolder);

            string sentLogFile = Path.Combine(saversFolder, "SentDates.txt");

            var db = new AttendanceDatabase(saversFolder);
            var attendances = db.GetAttendanceRecords();
            if (attendances == null || !attendances.Any())
                return;

            // Load sent keys + last sent date per employee
            var sentRecords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lastSentByEmp = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(sentLogFile))
            {
                foreach (var line in File.ReadAllLines(sentLogFile))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;

                    sentRecords.Add(trimmed);

                    var parts = trimmed.Split('|');
                    if (parts.Length == 2 && DateTime.TryParse(parts[1], out var d))
                    {
                        var empId = parts[0].Trim();
                        if (!lastSentByEmp.TryGetValue(empId, out var existing) || d.Date > existing.Date)
                            lastSentByEmp[empId] = d.Date;
                    }
                }
            }

            DateTime today = DateTime.Now.Date;        // run time day
            DateTime maxDateToSend = today.AddDays(-1); // ✅ avoid sending "today" partial attendance

            // ✅ Send from "last sent date" onwards (per employee), until last saved dates in DB
            var pending = attendances
                .Where(a =>
                {
                    var empId = a.EmployeeId?.Trim() ?? "";
                    var date = a.Date.Date;

                    // don’t send today
                    if (date > maxDateToSend) return false;

                    // per-employee last sent date
                    DateTime lastSent = lastSentByEmp.TryGetValue(empId, out var v) ? v.Date : DateTime.MinValue.Date;
                    if (date <= lastSent) return false;

                    // final duplicate guard
                    string key = $"{empId}|{date:yyyy-MM-dd}";
                    return !sentRecords.Contains(key);
                })
                .OrderBy(a => a.Date)
                .ThenBy(a => a.EmployeeId)
                .ToList();

            if (!pending.Any())
                return;

            var mailService = new MailService();

            foreach (var a in pending)
            {
                string recordKey = $"{a.EmployeeId?.Trim()}|{a.Date:yyyy-MM-dd}";

                string recipientEmail = a.Email;
                if (string.IsNullOrWhiteSpace(recipientEmail))
                    continue;

                string subject = $"Daily Attendance {a.Name}";

                string body = $@"
<html>
<head>
  <style>
    .card {{
      font-family: Arial, sans-serif;
      border-radius: 10px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      padding: 20px;
      max-width: 400px;
      background-color: #ffffff;
    }}
    .header {{
      font-size: 18px;
      font-weight: bold;
      color: #1e1e9c;
      margin-bottom: 15px;
    }}
    .employee {{
      border: 1px solid #d6d9f3;
      border-radius: 8px;
      padding: 10px;
      margin-bottom: 15px;
      background-color: #f8f9ff;
    }}
    .label {{ font-size: 12px; color: #888888; }}
    .check-in {{
      background-color: #e6f9e6; padding: 10px; border-radius: 6px;
      display: inline-block; margin-right: 10px; font-weight: bold;
    }}
    .check-out {{
      background-color: #fff4e6; padding: 10px; border-radius: 6px;
      display: inline-block; font-weight: bold;
    }}
    .status {{
      margin-top: 15px; display: inline-block; padding: 6px 12px;
      border-radius: 20px; background-color: #d4f1d4;
      font-weight: bold; color: #2b7a2b;
    }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='header'>🕒 PDC System</div>

    <div class='employee'>
      <div>{a.Name}</div>
      <div class='label'>ID: {a.EmployeeId}</div>
    </div>

    <div class='label'>Date</div>
    <div>{a.Date:MMMM dd, yyyy}</div>

    <div style='margin-top:10px;'>
      <span class='check-in'>Check-in: {a.CheckIn}</span>
      <span class='check-out'>Check-out: {a.CheckOut}</span>
    </div>

    <div class='status'>{a.Status}</div>
  </div>
</body>
</html>";

                bool emailSent = await mailService.SendEmailAsync(recipientEmail, new List<string>(), subject, body);

                if (emailSent)
                {
                    File.AppendAllText(sentLogFile, recordKey + Environment.NewLine);
                    sentRecords.Add(recordKey);

                    // update cache so same run doesn't resend
                    var empId = a.EmployeeId?.Trim() ?? "";
                    var d = a.Date.Date;
                    if (!lastSentByEmp.TryGetValue(empId, out var existing) || d > existing)
                        lastSentByEmp[empId] = d;
                }
            }
        }

    }
}