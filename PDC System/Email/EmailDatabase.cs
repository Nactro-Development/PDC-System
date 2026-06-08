using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using PDC_System.Models;

namespace PDC_System.Services
{
    /// <summary>
    /// Handles all Email-related SQLite operations.
    /// Add the following DDL to DatabaseInitializer.cs Initialize() method:
    ///
    ///   CREATE TABLE IF NOT EXISTS Email (
    ///       Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ///       Folder TEXT NOT NULL DEFAULT 'Inbox',
    ///       FromAddress TEXT NOT NULL DEFAULT '',
    ///       ToAddress TEXT NOT NULL DEFAULT '',
    ///       Subject TEXT NOT NULL DEFAULT '',
    ///       BodyHtml TEXT NOT NULL DEFAULT '',
    ///       BodyText TEXT NOT NULL DEFAULT '',
    ///       DateReceived TEXT NOT NULL,
    ///       IsRead INTEGER NOT NULL DEFAULT 0,
    ///       HasAttachments INTEGER NOT NULL DEFAULT 0,
    ///       UniqueId TEXT NOT NULL DEFAULT ''
    ///   );
    ///
    ///   CREATE TABLE IF NOT EXISTS EmailAttachments (
    ///       Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ///       EmailId INTEGER NOT NULL,
    ///       FileName TEXT NOT NULL DEFAULT '',
    ///       ContentType TEXT NOT NULL DEFAULT '',
    ///       Data BLOB NOT NULL,
    ///       Size INTEGER NOT NULL DEFAULT 0,
    ///       FOREIGN KEY (EmailId) REFERENCES Email(Id) ON DELETE CASCADE
    ///   );
    /// </summary>
    public class EmailDatabase
    {
        private readonly string _connectionString;

        public EmailDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ── Settings ──────────────────────────────────────────────────────────

        public string GetSetting(string key)
        {
            using var con = Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key = $k LIMIT 1";
            cmd.Parameters.AddWithValue("$k", key);
            return cmd.ExecuteScalar()?.ToString() ?? "";
        }

        // ── Emails ────────────────────────────────────────────────────────────

        public List<EmailMessage> GetEmails(string folder)
        {
            var list = new List<EmailMessage>();
            using var con = Open();

            var cmd = con.CreateCommand();
            cmd.CommandText =
                @"SELECT Id, Folder, FromAddress, ToAddress, Subject, BodyHtml, BodyText,
                         DateReceived, IsRead, HasAttachments, UniqueId
                  FROM Email WHERE Folder = $folder
                  ORDER BY DateReceived DESC";
            cmd.Parameters.AddWithValue("$folder", folder);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(ReadEmail(r));
            }
            return list;
        }

        public EmailMessage? GetEmailById(int id)
        {
            using var con = Open();
            var cmd = con.CreateCommand();
            cmd.CommandText =
                @"SELECT Id, Folder, FromAddress, ToAddress, Subject, BodyHtml, BodyText,
                         DateReceived, IsRead, HasAttachments, UniqueId
                  FROM Email WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadEmail(r) : null;
        }

        public bool UniqueIdExists(string uid, string folder)
        {
            using var con = Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM Email WHERE UniqueId = $uid AND Folder = $folder";
            cmd.Parameters.AddWithValue("$uid", uid);
            cmd.Parameters.AddWithValue("$folder", folder);
            return Convert.ToInt64(cmd.ExecuteScalar()!) > 0;
        }

        public int InsertEmail(EmailMessage msg)
        {
            using var con = Open();
            using var tran = con.BeginTransaction();

            var cmd = con.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText =
                @"INSERT INTO Email (Folder, FromAddress, ToAddress, Subject, BodyHtml, BodyText,
                                     DateReceived, IsRead, HasAttachments, UniqueId)
                  VALUES ($folder, $from, $to, $subject, $bodyHtml, $bodyText,
                          $date, $isRead, $hasAtt, $uid);
                  SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("$folder", msg.Folder);
            cmd.Parameters.AddWithValue("$from", msg.FromAddress);
            cmd.Parameters.AddWithValue("$to", msg.ToAddress);
            cmd.Parameters.AddWithValue("$subject", msg.Subject);
            cmd.Parameters.AddWithValue("$bodyHtml", msg.BodyHtml);
            cmd.Parameters.AddWithValue("$bodyText", msg.BodyText);
            cmd.Parameters.AddWithValue("$date", msg.DateReceived.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$isRead", msg.IsRead ? 1 : 0);
            cmd.Parameters.AddWithValue("$hasAtt", msg.HasAttachments ? 1 : 0);
            cmd.Parameters.AddWithValue("$uid", msg.UniqueId);

            int newId = Convert.ToInt32(cmd.ExecuteScalar());

            foreach (var att in msg.Attachments)
            {
                InsertAttachment(con, tran, newId, att);
            }

            tran.Commit();
            return newId;
        }

        public void MarkAsRead(int id)
        {
            using var con = Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE Email SET IsRead = 1 WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteEmail(int id)
        {
            using var con = Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Email WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        // ── Attachments ───────────────────────────────────────────────────────

        public List<EmailAttachment> GetAttachments(int emailId)
        {
            var list = new List<EmailAttachment>();
            using var con = Open();

            var cmd = con.CreateCommand();
            cmd.CommandText =
                "SELECT Id, EmailId, FileName, ContentType, Data, Size FROM EmailAttachments WHERE EmailId = $eid";
            cmd.Parameters.AddWithValue("$eid", emailId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new EmailAttachment
                {
                    Id = r.GetInt32(0),
                    EmailId = r.GetInt32(1),
                    FileName = r.GetString(2),
                    ContentType = r.GetString(3),
                    Data = (byte[])r["Data"],
                    Size = r.GetInt64(5)
                });
            }
            return list;
        }

        // ── Signature ─────────────────────────────────────────────────────────

        public string GetSignature()
        {
            using var con = Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key = 'EmailSignature' LIMIT 1";
            return cmd.ExecuteScalar()?.ToString() ?? "";
        }

        public void SaveSignature(string html)
        {
            using var con = Open();
            var cmd = con.CreateCommand();
            cmd.CommandText =
                @"INSERT INTO Settings (Key, Value) VALUES ('EmailSignature', $v)
                  ON CONFLICT(Key) DO UPDATE SET Value = $v";
            cmd.Parameters.AddWithValue("$v", html);
            cmd.ExecuteNonQuery();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static EmailMessage ReadEmail(SqliteDataReader r)
        {
            DateTime.TryParse(r.GetString(7), out var dt);
            return new EmailMessage
            {
                Id = r.GetInt32(0),
                Folder = r.GetString(1),
                FromAddress = r.GetString(2),
                ToAddress = r.GetString(3),
                Subject = r.GetString(4),
                BodyHtml = r.GetString(5),
                BodyText = r.GetString(6),
                DateReceived = dt,
                IsRead = r.GetInt32(8) == 1,
                HasAttachments = r.GetInt32(9) == 1,
                UniqueId = r.GetString(10)
            };
        }

        private static void InsertAttachment(SqliteConnection con, SqliteTransaction tran,
                                              int emailId, EmailAttachment att)
        {
            var cmd = con.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText =
                @"INSERT INTO EmailAttachments (EmailId, FileName, ContentType, Data, Size)
                  VALUES ($eid, $fn, $ct, $data, $size)";
            cmd.Parameters.AddWithValue("$eid", emailId);
            cmd.Parameters.AddWithValue("$fn", att.FileName);
            cmd.Parameters.AddWithValue("$ct", att.ContentType);
            cmd.Parameters.AddWithValue("$data", att.Data);
            cmd.Parameters.AddWithValue("$size", att.Size);
            cmd.ExecuteNonQuery();
        }

        private SqliteConnection Open()
        {
            var con = new SqliteConnection(_connectionString);
            con.Open();
            return con;
        }
    }
}
