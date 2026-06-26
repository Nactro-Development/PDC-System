using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using PDC_System;
using PDC_System.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace PDC_System.Services
{
    public class AttendanceManager
    {
        string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savers");

        private readonly AttendanceDatabase _db;

        public AttendanceManager()
        {
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            string employeeDat = Path.Combine(basePath, "employees.dat");

            if (!File.Exists(employeeDat))
            {
                byte[] encrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes("[]"),
                    null,
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(employeeDat, encrypted);
            }


            EnsureJsonFile("holiday.json");

            _db = new AttendanceDatabase(basePath);
        }

        private void EnsureJsonFile(string fileName)
        {
            string path = Path.Combine(basePath, fileName);
            if (!File.Exists(path))
                File.WriteAllText(path, "[]");
        }



        private List<Employee> LoadEmployees()
        {
            string datFile = Path.Combine(basePath, "employees.dat");

            if (!File.Exists(datFile))
                return new List<Employee>();

            byte[] encrypted = File.ReadAllBytes(datFile);

            byte[] decrypted = ProtectedData.Unprotect(
                encrypted,
                null,
                DataProtectionScope.CurrentUser);

            string json = Encoding.UTF8.GetString(decrypted);

            return JsonConvert.DeserializeObject<List<Employee>>(json)
                   ?? new List<Employee>();
        }



        // ✅ Load all attendance (read-only, NO auto-save)
        public List<AttendanceRecord> LoadAttendance()
        {
            var data = _db.GetFingerprintDataFromEvents();

            var employees = EmployeeStorage.Load();

            var holidays = JsonConvert.DeserializeObject<List<Holiday>>(
                File.ReadAllText(Path.Combine(basePath, "holiday.json")));

            var records = new List<AttendanceRecord>();
            var grouped = data.GroupBy(x => new { x.EmployeeId, Date = x.EventTime.Date });
            var savedRecords = LoadSavedAttendanceRecords();

            foreach (var emp in employees)
            {
                DateTime startDate = DateTime.Today.AddDays(-30);
                DateTime endDate = DateTime.Today;

                // ✅ Manual edits තියෙනවා නම් past සහ future දෙකම extend කරනවා
                var empManualEdits = savedRecords
                    .Where(r => r.EmployeeId == emp.EmployeeId && r.IsManualEdit)
                    .ToList();

                if (empManualEdits.Any())
                {
                    var earliestManual = empManualEdits.Min(r => r.Date);
                    var latestManual = empManualEdits.Max(r => r.Date);

                    // ✅ Past - startDate extend
                    if (earliestManual.Date < startDate.Date)
                        startDate = earliestManual.Date;

                    // ✅ Future - endDate extend
                    if (latestManual.Date > endDate.Date)
                        endDate = latestManual.Date;
                }

                for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
                {
                    var savedRecord = savedRecords.FirstOrDefault(r =>
                        r.EmployeeId == emp.EmployeeId &&
                        r.Date.Date == day.Date);

                    if (savedRecord != null && savedRecord.IsManualEdit)
                    {
                        records.Add(savedRecord);
                        continue;
                    }

                    var grp = grouped.FirstOrDefault(g =>
                        g.Key.EmployeeId == emp.EmployeeId && g.Key.Date == day);
                    var times = grp?.OrderBy(x => x.EventTime).Select(x => x.EventTime).ToList();

                    if (grp != null)
                    {
                       
                    }

                    var calculatedRecord = CalculateAttendanceRecord(emp, day, times, holidays);
                    records.Add(calculatedRecord);
                }
            }

            return records.OrderBy(r => r.Date).ThenBy(r => r.EmployeeId).ToList();
        }


        // ✅ Load with date range - now generates temporary absence records
        public List<AttendanceRecord> LoadAttendanceWithDateRange(DateTime startDate, DateTime endDate)
        {
            var data = _db.GetFingerprintDataFromEvents();
            var employees = EmployeeStorage.Load();
            var holidays = JsonConvert.DeserializeObject<List<Holiday>>(File.ReadAllText(Path.Combine(basePath, "holiday.json")));

            var records = new List<AttendanceRecord>();
            var dateRange = new List<DateTime>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                dateRange.Add(date);
            }

            var grouped = data.GroupBy(x => new { x.EmployeeId, Date = x.EventTime.Date });
            var savedRecords = LoadSavedAttendanceRecords();

            foreach (var emp in employees)
            {
                foreach (var day in dateRange)
                {
                    var savedRecord = savedRecords.FirstOrDefault(r =>
                        r.EmployeeId == emp.EmployeeId &&
                        r.Date.Date == day.Date);

                    if (savedRecord != null && savedRecord.IsManualEdit)
                    {
                        records.Add(savedRecord);
                        continue;
                    }

                    var grp = grouped.FirstOrDefault(g => g.Key.EmployeeId == emp.EmployeeId && g.Key.Date == day);
                    var times = grp?.OrderBy(x => x.EventTime).Select(x => x.EventTime).ToList();

                    var calculatedRecord = CalculateAttendanceRecord(emp, day, times, holidays);
                    records.Add(calculatedRecord);
                }
            }

            return records.OrderBy(r => r.Date).ThenBy(r => r.EmployeeId).ToList();
        }

        // ✅ Save single attendance record (manual or calculated)
        public void SaveAttendanceRecord(AttendanceRecord record)
        {
            try
            {
                _db.SaveOrUpdateAttendanceRecord(record);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving attendance record: {ex.Message}");
            }
        }

        public void SaveAllAttendanceRecords(List<AttendanceRecord> records)
        {
            try
            {
                _db.SaveAllAttendanceRecords(records);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving all attendance records: {ex.Message}");
            }
        }

        private List<AttendanceRecord> LoadSavedAttendanceRecords()
        {
            return _db.GetAttendanceRecords();
        }

        private AttendanceRecord CalculateAttendanceRecord(Employee emp, DateTime day, List<DateTime> times, List<Holiday> holidays)
        {
            string checkInStr = "-";
            string checkOutStr = "-";
            string overtime = "0h 0m";
            string doubleOtStr = "0h 0m";
            string earlyLeave = "0h 0m";
            string lateHours = "0h 0m";
            string status = "";

            bool isSunday = day.DayOfWeek == DayOfWeek.Sunday;
            bool isSaturday = day.DayOfWeek == DayOfWeek.Saturday;
            bool isWorkingDay = IsWorkingDay(emp, day);
            var holiday = holidays.FirstOrDefault(h => h.Date.Date == day);
            bool isHoliday = holiday != null;
            bool isNonWorkingDay = !isWorkingDay && !isHoliday;

            TimeSpan workStart = isSaturday ? emp.SaturdayCheckIn : emp.CheckIn;
            TimeSpan workEnd = isSaturday ? emp.SaturdayCheckOut : emp.CheckOut;

            if (isHoliday)
            {
                status = $"{holiday.Name} (Holiday)";

                if (times != null && times.Count >= 2)
                {
                    var first = times.First();
                    var last = times.Last();
                    checkInStr = first.ToString("HH:mm");
                    checkOutStr = last.ToString("HH:mm");

                    var totalWorked = last - first;
                    var doubleOt = TimeSpan.FromMinutes(totalWorked.TotalMinutes * 1);
                    doubleOtStr = $"{(int)doubleOt.TotalHours}h {doubleOt.Minutes}m";
                }
            }


            else if (isNonWorkingDay)
            {
                if (times != null && times.Count >= 2)
                {
                    var first = times.First();
                    var last = times.Last();
                    checkInStr = first.ToString("HH:mm");
                    checkOutStr = last.ToString("HH:mm");

                    // ✅ NEW: Sunday non-working day = Double OT (not Normal OT)
                    if (isSunday)
                    {
                        var totalWorked = last - first;
                        var doubleOt = TimeSpan.FromMinutes(totalWorked.TotalMinutes);
                        doubleOtStr = $"{(int)doubleOt.TotalHours}h {doubleOt.Minutes}m";
                        lateHours = "0h 0m";
                        earlyLeave = "0h 0m";
                        overtime = "0h 0m";
                        status = "Sunday (Double OT)";
                    }
                    else
                    {
                        // Normal off-day worked = Normal OT
                        var totalWorked = last - first;
                        totalWorked = RoundToSettingMinutes(totalWorked);
                        overtime = $"{(int)totalWorked.TotalHours}h {totalWorked.Minutes}m";
                        lateHours = "0h 0m";
                        earlyLeave = "0h 0m";
                        doubleOtStr = "0h 0m";
                        status = "Worked on Off Day (Normal OT)";
                    }
                }
                else if (times != null && times.Count == 1)
                {
                    checkInStr = times.First().ToString("HH:mm");
                    status = "Off Day (Missing Out)";
                }
                else
                {
                    if (isSunday)
                    {
                        status = "Sunday Day";
                    }
                    else
                    {
                        status = "Non-Working Day";
                    }
                }
            }






            else if (times == null || times.Count == 0)
            {
                // This creates temporary absence records without saving to database
                status = "Absent (Temporary)";
            }





            else if (times.Count == 1)
            {
                checkInStr = times.First().ToString("HH:mm");
                status = "Missing Finger Print";
            }




            else
            {
                var first = times.First();
                var last = times.Last();
                checkInStr = first.ToString("HH:mm");
                checkOutStr = last.ToString("HH:mm");

                if (isSunday)
                {
                    var totalWorked = last - first;
                    var doubleOt = TimeSpan.FromMinutes(totalWorked.TotalMinutes * 1);
                    doubleOtStr = $"{(int)doubleOt.TotalHours}h {doubleOt.Minutes}m";
                    status = "Sunday (Double OT)";
                }
                else if (isSaturday)
                {
                    if (!emp.Saturday)
                    {
                        var totalWorked = last - first;
                        var doubleOt = TimeSpan.FromMinutes(totalWorked.TotalMinutes);
                        doubleOtStr = $"{(int)doubleOt.TotalHours}h {doubleOt.Minutes}m";
                        status = "Saturday (Double OT)";
                    }
                    else
                    {
                        TimeSpan ot = TimeSpan.Zero;

                        // 🔹 Early check-in OT
                        if (first.TimeOfDay < workStart)
                            ot += workStart - first.TimeOfDay;

                        // 🔹 Late check-out OT
                        if (last.TimeOfDay > workEnd)
                        {
                            var extra = last.TimeOfDay - workEnd;
                            if (extra.TotalMinutes >= 14)
                                extra = RoundToSettingMinutes(extra);

                            ot += extra;
                        }

                        ot = RoundToSettingMinutes(ot);
                        overtime = $"{(int)ot.TotalHours}h {ot.Minutes}m";

                        // 🔹 Early Leave
                        if (last.TimeOfDay < workEnd)
                        {
                            var early = workEnd - last.TimeOfDay;
                            earlyLeave = $"{(int)early.TotalHours}h {early.Minutes}m";
                        }

                        // 🔹 Late Hours
                        int allowedLate = Properties.Settings.Default.Late_Allow_Minutes;

                        if (first.TimeOfDay > workStart)
                        {
                            var late = first.TimeOfDay - workStart;

                            if (late.TotalMinutes > allowedLate)
                            {
                                var actualLate = late - TimeSpan.FromMinutes(allowedLate);
                                lateHours = $"{(int)actualLate.TotalHours}h {actualLate.Minutes}m";
                            }
                        }

                        status = "Saturday OK";
                    }
                }
                else
                {
                    // Normal working day
                    TimeSpan ot = TimeSpan.Zero;

                    if (first.TimeOfDay < workStart)
                        ot += workStart - first.TimeOfDay;

                    if (last.TimeOfDay > workEnd)
                    {
                        var extra = last.TimeOfDay - workEnd;
                        if (extra.TotalMinutes >= 14) extra = RoundToSettingMinutes(extra);
                        ot += extra;
                    }

                    ot = RoundToSettingMinutes(ot);
                    overtime = $"{(int)ot.TotalHours}h {ot.Minutes}m";

                    if (last.TimeOfDay < workEnd)
                    {
                        var early = workEnd - last.TimeOfDay;
                        earlyLeave = $"{(int)early.TotalHours}h {early.Minutes}m";
                    }

                    int allowedLate = Properties.Settings.Default.Late_Allow_Minutes;

                    if (first.TimeOfDay > workStart)
                    {
                        var late = first.TimeOfDay - workStart;

                        if (late.TotalMinutes <= allowedLate)
                        {
                            lateHours = "0h 0m";
                        }
                        else
                        {
                            var actualLate = late - TimeSpan.FromMinutes(allowedLate);
                            lateHours = $"{(int)actualLate.TotalHours}h {actualLate.Minutes}m";
                        }
                    }

                    status = "OK";
                }
            }

            return new AttendanceRecord
            {
                EmployeeId = emp.EmployeeId,
                Name = emp.Name,
                Email = emp.EmployeeEmail,
                Date = day,
                CheckIn = checkInStr,
                CheckOut = checkOutStr,
                OverTime = overtime,
                DoubleOT = doubleOtStr,
                EarlyLeave = earlyLeave,
                LateHours = lateHours,
                Status = status,
                IsManualEdit = false
            };
        }

        private TimeSpan RoundToSettingMinutes(TimeSpan time)
        {
            int roundMinutes = Properties.Settings.Default.OT_RoundMinutes;

            if (roundMinutes <= 1)
                return time;

            double totalMinutes = time.TotalMinutes;
            double roundedMinutes = Math.Floor(totalMinutes / roundMinutes) * roundMinutes;

            return TimeSpan.FromMinutes(roundedMinutes);
        }

        private bool IsWorkingDay(Employee emp, DateTime day)
        {
            switch (day.DayOfWeek)
            {
                case DayOfWeek.Monday: return emp.Monday;
                case DayOfWeek.Tuesday: return emp.Tuesday;
                case DayOfWeek.Wednesday: return emp.Wednesday;
                case DayOfWeek.Thursday: return emp.Thursday;
                case DayOfWeek.Friday: return emp.Friday;
                case DayOfWeek.Saturday: return emp.Saturday;
                case DayOfWeek.Sunday: return emp.Sunday;
                default: return false;
            }
        }

        // ✅ Save manual attendance record with IsManualEdit = true
        public void SaveManualAttendanceRecord(AttendanceRecord record)
        {
            try
            {
                record.IsManualEdit = true;
                record.Status = "Manual Edit";

                _db.SaveOrUpdateAttendanceRecord(record);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving manual attendance record: {ex.Message}");
            }
        }

        // ✅ Reset manual edits for date range
        public void ResetManualEdits(DateTime startDate, DateTime endDate)
        {
            try
            {
                _db.DeleteManualEdits(startDate, endDate);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error resetting manual edits: {ex.Message}");
            }
        }

        // ✅ Reset single manual edit
        public void ResetManualEditForRecord(AttendanceRecord record)
        {
            try
            {
                _db.DeleteManualEditForRecord(record.EmployeeId, record.Date);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error resetting manual edit for record: {ex.Message}");
            }
        }

        // Returns DataTable for raw IVMS logs (instead of ivms.json)
        public DataTable LoadRawPunchTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("EmployeeID", typeof(string));
            dt.Columns.Add("EmployeeName", typeof(string));
            dt.Columns.Add("DateTime", typeof(DateTime));

            var db = new AttendanceDatabase(basePath);
            var fp = db.GetFingerprintDataFromEvents();
            var employees = EmployeeStorage.Load();

            foreach (var f in fp)
            {
                var emp = employees.FirstOrDefault(x => x.EmployeeId == f.EmployeeId);
                dt.Rows.Add(f.EmployeeId, emp?.Name ?? "-", f.EventTime);
            }

            return dt;
        }

        public class Holiday
        {
            public DateTime Date { get; set; }
            public string Name { get; set; }
        }
    }
}