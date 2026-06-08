using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using MimeKit.Text;
using PDC_System.Models;

namespace PDC_System.Services
{
    public class EmailService
    {
        private readonly EmailDatabase _db;
        private string _email = "";
        private string _appPassword = "";

        // Gmail defaults — override if needed
        private const string ImapHost = "imap.gmail.com";
        private const int ImapPort = 993;
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const long MaxAttachmentBytes = 25 * 1024 * 1024; // 25 MB

        public EmailService(EmailDatabase db)
        {
            _db = db;
        }

        public void LoadCredentials()
        {
            _email = Properties.Settings.Default.PDCEmail;
            _appPassword = Properties.Settings.Default.PDCAppPassword;
        }

        public bool HasCredentials => !string.IsNullOrWhiteSpace(_email) &&
                                      !string.IsNullOrWhiteSpace(_appPassword);

        // ── Send ──────────────────────────────────────────────────────────────

        public async Task SendEmailAsync(string to, string subject, string bodyHtml,
                                          List<(string FileName, byte[] Data, string ContentType)> attachments,
                                          CancellationToken ct = default)
        {
            long totalSize = attachments.Sum(a => (long)a.Data.Length);
            if (totalSize > MaxAttachmentBytes)
                throw new InvalidOperationException(
                    $"Total attachment size ({totalSize / 1024 / 1024:F1} MB) exceeds the 25 MB limit.");

            string signature = _db.GetSignature();
            string fullHtml = string.IsNullOrWhiteSpace(signature)
                ? bodyHtml
                : bodyHtml + "<br/><hr/>" + signature;

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_email));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = fullHtml };
            foreach (var (fileName, data, contentType) in attachments)
            {
                builder.Attachments.Add(fileName, data, ContentType.Parse(contentType));
            }
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpHost, SmtpPort, MailKit.Security.SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_email, _appPassword, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            // Persist to Sent
            var sent = new EmailMessage
            {
                Folder = "Sent",
                FromAddress = _email,
                ToAddress = to,
                Subject = subject,
                BodyHtml = fullHtml,
                BodyText = HtmlToPlain(fullHtml),
                DateReceived = DateTime.Now,
                IsRead = true,
                HasAttachments = attachments.Count > 0,
                UniqueId = Guid.NewGuid().ToString()
            };
            foreach (var (fileName, data, contentType) in attachments)
            {
                sent.Attachments.Add(new EmailAttachment
                {
                    FileName = fileName,
                    ContentType = contentType,
                    Data = data,
                    Size = data.Length
                });
            }
            _db.InsertEmail(sent);
        }

        // ── Fetch Inbox ───────────────────────────────────────────────────────

        public async Task FetchInboxAsync(CancellationToken ct = default)
        {
            using var client = new ImapClient();
            await client.ConnectAsync(ImapHost, ImapPort, true, ct);
            await client.AuthenticateAsync(_email, _appPassword, ct);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

            // Fetch last 100 messages at most
            int total = inbox.Count;
            int start = Math.Max(0, total - 100);

            var uids = await inbox.SearchAsync(SearchQuery.All, ct);
            var fetchUids = uids.Skip(Math.Max(0, uids.Count - 100)).ToList();

            foreach (var uid in fetchUids)
            {
                if (ct.IsCancellationRequested) break;
                string uidStr = uid.ToString();
                if (_db.UniqueIdExists(uidStr, "Inbox")) continue;

                var mime = await inbox.GetMessageAsync(uid, ct);
                var msg = ParseMimeMessage(mime, "Inbox", uidStr);
                _db.InsertEmail(msg);
            }

            await client.DisconnectAsync(true, ct);
        }

        // ── Parse ─────────────────────────────────────────────────────────────

        private static EmailMessage ParseMimeMessage(MimeMessage mime, string folder, string uid)
        {
            var msg = new EmailMessage
            {
                Folder = folder,
                FromAddress = mime.From.ToString(),
                ToAddress = mime.To.ToString(),
                Subject = mime.Subject ?? "(no subject)",
                BodyHtml = mime.HtmlBody ?? mime.TextBody ?? "",
                BodyText = mime.TextBody ?? HtmlToPlain(mime.HtmlBody ?? ""),
                DateReceived = mime.Date.LocalDateTime,
                IsRead = false,
                HasAttachments = false,
                UniqueId = uid
            };

            foreach (var part in mime.BodyParts)
            {
                if (part is MimePart mp && mp.IsAttachment)
                {
                    msg.HasAttachments = true;
                    using var ms = new MemoryStream();
                    mp.Content.DecodeTo(ms);
                    byte[] data = ms.ToArray();
                    msg.Attachments.Add(new EmailAttachment
                    {
                        FileName = mp.FileName ?? "attachment",
                        ContentType = mp.ContentType.MimeType,
                        Data = data,
                        Size = data.Length
                    });
                }
            }

            return msg;
        }

        private static string HtmlToPlain(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "").Trim();
        }
    }
}
