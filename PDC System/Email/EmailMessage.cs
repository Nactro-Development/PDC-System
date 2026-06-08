using System;
using System.Collections.Generic;

namespace PDC_System.Models
{
    public class EmailMessage
    {
        public int Id { get; set; }
        public string Folder { get; set; } = "Inbox"; // "Inbox" or "Sent"
        public string FromAddress { get; set; } = "";
        public string ToAddress { get; set; } = "";
        public string Subject { get; set; } = "";
        public string BodyHtml { get; set; } = "";
        public string BodyText { get; set; } = "";
        public DateTime DateReceived { get; set; }
        public bool IsRead { get; set; }
        public bool HasAttachments { get; set; }
        public string UniqueId { get; set; } = ""; // IMAP UID
        public List<EmailAttachment> Attachments { get; set; } = new();
    }

    public class EmailAttachment
    {
        public int Id { get; set; }
        public int EmailId { get; set; }
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public long Size { get; set; }
    }
}
