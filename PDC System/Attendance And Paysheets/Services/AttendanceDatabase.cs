using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using PDC_System.Models;

namespace PDC_System.Services
{
    public class AttendanceDatabase
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

  


        public AttendanceDatabase(string basePath)
        {
            _dbPath = Path.Combine(basePath, "attendance.db");
            _connectionString = $"Data Source={_dbPath}";
            Initialize();
        }

        private void Initialize()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
@"
CREATE TABLE IF NOT EXISTS FingerprintData (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId TEXT NOT NULL,
    DateTime TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Devices
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT,
    IP TEXT,
    Port INTEGER,
    Username TEXT,
    Password TEXT
);

CREATE TABLE IF NOT EXISTS AttendanceEvents
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SerialNo INTEGER UNIQUE,
    EmployeeId TEXT,
    EventTime TEXT,
    VerifyMode INTEGER,
    Major INTEGER,
    Minor INTEGER
);


CREATE TABLE IF NOT EXISTS AttendanceRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId TEXT NOT NULL,
    Name TEXT,
    Email TEXT,
    Date TEXT NOT NULL,
    CheckIn TEXT,
    CheckOut TEXT,
    OverTime TEXT,
    DoubleOT TEXT,
    EarlyLeave TEXT,
    LateHours TEXT,
    Status TEXT,
    IsManualEdit INTEGER NOT NULL DEFAULT 0
);
";
            cmd.ExecuteNonQuery();
        }

        // ====== FINGERPRINT DATA (ivms.json replace) ======

        public List<FingerprintData> GetFingerprintData()
        {
            var list = new List<FingerprintData>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT EmployeeId, DateTime FROM FingerprintData";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var empId = reader.GetString(0);
                var dtStr = reader.GetString(1);
                if (DateTime.TryParse(dtStr, out var dt))
                {
                    list.Add(new FingerprintData
                    {
                        EmployeeID = empId,
                        DateTime = dt
                    });
                }
            }

            return list;
        }

        public void InsertFingerprintData(IEnumerable<FingerprintData> data)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var tran = connection.BeginTransaction();
            var cmd = connection.CreateCommand();
            cmd.Transaction = tran;

            cmd.CommandText = "DELETE FROM FingerprintData";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO FingerprintData (EmployeeId, DateTime) VALUES ($emp, $dt)";

            foreach (var item in data)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("$emp", item.EmployeeID);
                cmd.Parameters.AddWithValue("$dt", item.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }

            tran.Commit();
        }

        // ====== ATTENDANCE RECORDS (attendance.json replace) ======

        public List<AttendanceRecord> GetAttendanceRecords()
        {
            var list = new List<AttendanceRecord>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"SELECT EmployeeId, Name, Email, Date, CheckIn, CheckOut, OverTime, DoubleOT, EarlyLeave, LateHours, Status, IsManualEdit
                  FROM AttendanceRecords";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dateStr = reader.GetString(3);
                DateTime.TryParse(dateStr, out var date);

                list.Add(new AttendanceRecord
                {
                    EmployeeId = reader.GetString(0),
                    Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Date = date,
                    CheckIn = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                    CheckOut = reader.IsDBNull(5) ? "-" : reader.GetString(5),
                    OverTime = reader.IsDBNull(6) ? "0h 0m" : reader.GetString(6),
                    DoubleOT = reader.IsDBNull(7) ? "0h 0m" : reader.GetString(7),
                    EarlyLeave = reader.IsDBNull(8) ? "0h 0m" : reader.GetString(8),
                    LateHours = reader.IsDBNull(9) ? "0h 0m" : reader.GetString(9),
                    Status = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    IsManualEdit = reader.GetInt32(11) == 1
                });
            }

            return list;
        }

        public void SaveOrUpdateAttendanceRecord(AttendanceRecord record)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // First delete existing record (same EmployeeId + Date)
            var delCmd = connection.CreateCommand();
            delCmd.CommandText =
                @"DELETE FROM AttendanceRecords WHERE EmployeeId = $emp AND Date = $date";
            delCmd.Parameters.AddWithValue("$emp", record.EmployeeId);
            delCmd.Parameters.AddWithValue("$date", record.Date.ToString("yyyy-MM-dd"));
            delCmd.ExecuteNonQuery();

            // Insert new
            var insCmd = connection.CreateCommand();
            insCmd.CommandText =
                @"INSERT INTO AttendanceRecords
                  (EmployeeId, Name, Email, Date, CheckIn, CheckOut, OverTime, DoubleOT, EarlyLeave, LateHours, Status, IsManualEdit)
                  VALUES
                  ($emp, $name, $email, $date, $in, $out, $ot, $dot, $early, $late, $status, $manual)";

            insCmd.Parameters.AddWithValue("$emp", record.EmployeeId);
            insCmd.Parameters.AddWithValue("$name", record.Name ?? "");
            insCmd.Parameters.AddWithValue("$email", record.Email ?? "");
            insCmd.Parameters.AddWithValue("$date", record.Date.ToString("yyyy-MM-dd"));
            insCmd.Parameters.AddWithValue("$in", record.CheckIn ?? "-");
            insCmd.Parameters.AddWithValue("$out", record.CheckOut ?? "-");
            insCmd.Parameters.AddWithValue("$ot", record.OverTime ?? "0h 0m");
            insCmd.Parameters.AddWithValue("$dot", record.DoubleOT ?? "0h 0m");
            insCmd.Parameters.AddWithValue("$early", record.EarlyLeave ?? "0h 0m");
            insCmd.Parameters.AddWithValue("$late", record.LateHours ?? "0h 0m");
            insCmd.Parameters.AddWithValue("$status", record.Status ?? "");
            insCmd.Parameters.AddWithValue("$manual", record.IsManualEdit ? 1 : 0);

            insCmd.ExecuteNonQuery();
        }

        public void SaveAllAttendanceRecords(List<AttendanceRecord> records)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var tran = connection.BeginTransaction();
            var cmd = connection.CreateCommand();
            cmd.Transaction = tran;

            cmd.CommandText = "DELETE FROM AttendanceRecords";
            cmd.ExecuteNonQuery();

            cmd.CommandText =
                @"INSERT INTO AttendanceRecords
                  (EmployeeId, Name, Email, Date, CheckIn, CheckOut, OverTime, DoubleOT, EarlyLeave, LateHours, Status, IsManualEdit)
                  VALUES
                  ($emp, $name, $email, $date, $in, $out, $ot, $dot, $early, $late, $status, $manual)";

            foreach (var record in records)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("$emp", record.EmployeeId);
                cmd.Parameters.AddWithValue("$name", record.Name ?? "");
                cmd.Parameters.AddWithValue("$email", record.Email ?? "");
                cmd.Parameters.AddWithValue("$date", record.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$in", record.CheckIn ?? "-");
                cmd.Parameters.AddWithValue("$out", record.CheckOut ?? "-");
                cmd.Parameters.AddWithValue("$ot", record.OverTime ?? "0h 0m");
                cmd.Parameters.AddWithValue("$dot", record.DoubleOT ?? "0h 0m");
                cmd.Parameters.AddWithValue("$early", record.EarlyLeave ?? "0h 0m");
                cmd.Parameters.AddWithValue("$late", record.LateHours ?? "0h 0m");
                cmd.Parameters.AddWithValue("$status", record.Status ?? "");
                cmd.Parameters.AddWithValue("$manual", record.IsManualEdit ? 1 : 0);

                cmd.ExecuteNonQuery();
            }

            tran.Commit();
        }

        public void DeleteManualEdits(DateTime start, DateTime end)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"DELETE FROM AttendanceRecords
                  WHERE IsManualEdit = 1
                  AND Date >= $start
                  AND Date <= $end";

            cmd.Parameters.AddWithValue("$start", start.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$end", end.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }

        public void DeleteManualEditForRecord(string employeeId, DateTime date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"DELETE FROM AttendanceRecords
                  WHERE IsManualEdit = 1 AND EmployeeId = $emp AND Date = $date";

            cmd.Parameters.AddWithValue("$emp", employeeId);
            cmd.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }


        public void SaveAttendanceEvent(AttendanceEvent item)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();

            cmd.CommandText =
            @"INSERT OR IGNORE INTO AttendanceEvents
    (SerialNo, EmployeeId, EventTime, VerifyMode, Major, Minor)
    VALUES
    ($serial,$emp,$time,$verify,$major,$minor)";

            cmd.Parameters.AddWithValue("$serial", item.SerialNo);
            cmd.Parameters.AddWithValue("$emp", item.EmployeeId);
            cmd.Parameters.AddWithValue("$time", item.EventTime.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$verify", item.VerifyMode);
            cmd.Parameters.AddWithValue("$major", item.Major);
            cmd.Parameters.AddWithValue("$minor", item.Minor);

            cmd.ExecuteNonQuery();
        }

        public List<AttendanceEvent> GetAttendanceEvents()
        {
            var list = new List<AttendanceEvent>();

            using var connection = new SqliteConnection(_connectionString);

            connection.Open();

            var cmd = connection.CreateCommand();
           
            cmd.CommandText =
            @"SELECT SerialNo,
             EmployeeId,
             EventTime,
             VerifyMode,
             Major,
             Minor
      FROM AttendanceEvents
      ORDER BY SerialNo";

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new AttendanceEvent
                {
                    SerialNo = reader.GetInt32(0),
                    EmployeeId = reader.GetString(1),
                    EventTime = DateTime.Parse(reader.GetString(2)),
                    VerifyMode = reader.GetInt32(3),
                    Major = reader.GetInt32(4),
                    Minor = reader.GetInt32(5)
                });
            }

            return list;
        }


        public List<FingerprintData> GetFingerprintDataFromEvents()
        {
            return GetAttendanceEvents()
                .Select(x => new FingerprintData
                {
                    EmployeeID = x.EmployeeId,
                    DateTime = x.EventTime
                })
                .ToList();
        }


        public void SaveFingerprintData(string employeeId, DateTime dateTime)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();

            cmd.CommandText =
            @"INSERT INTO FingerprintData(EmployeeId, DateTime)
      SELECT $emp, $dt
      WHERE NOT EXISTS
      (
          SELECT 1
          FROM FingerprintData
          WHERE EmployeeId=$emp
          AND DateTime=$dt
      );";

            cmd.Parameters.AddWithValue("$emp", employeeId);
            cmd.Parameters.AddWithValue("$dt", dateTime.ToString("yyyy-MM-dd HH:mm:ss"));

            cmd.ExecuteNonQuery();
        }



    }
}
