using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PDC_System.Models;
using PDC_System.Services;

namespace PDC_System.Email
{
    public partial class EmailReader : UserControl
    {
        // ── Dependency Properties ──────────────────────────────────────────

        public static readonly DependencyProperty EmailProperty =
            DependencyProperty.Register(nameof(Email), typeof(EmailMessage), typeof(EmailReader),
                new PropertyMetadata(null, OnEmailChanged));

        public static readonly DependencyProperty DownloadCommandProperty =
            DependencyProperty.Register(nameof(DownloadCommand), typeof(ICommand), typeof(EmailReader));

        public EmailMessage? Email
        {
            get => (EmailMessage?)GetValue(EmailProperty);
            set => SetValue(EmailProperty, value);
        }

        public ICommand? DownloadCommand
        {
            get => (ICommand?)GetValue(DownloadCommandProperty);
            set => SetValue(DownloadCommandProperty, value);
        }

        // For the empty-state binding
        public bool HasEmail => Email != null;

        public EmailReader()
        {
            InitializeComponent();
        }

        private static void OnEmailChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmailReader reader)
            {
                reader.RenderEmail(e.NewValue as EmailMessage);
                reader.SetValue(HasEmailPropertyKey, e.NewValue != null);
            }
        }

        private static readonly DependencyPropertyKey HasEmailPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasEmail), typeof(bool), typeof(EmailReader),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasEmailDependencyProperty =
            HasEmailPropertyKey.DependencyProperty;

        // ── Render ─────────────────────────────────────────────────────────

        private void RenderEmail(EmailMessage? msg)
        {
            if (msg == null)
            {
                HtmlViewer.NavigateToString("<html><body></body></html>");
                AttachmentsStrip.Visibility = Visibility.Collapsed;
                SubjectText.Text = "";
                FromText.Text = "";
                ToText.Text = "";
                DateText.Text = "";
                return;
            }

            SubjectText.Text = msg.Subject;
            FromText.Text = msg.FromAddress;
            ToText.Text = msg.ToAddress;
            DateText.Text = msg.DateReceived.ToString("ddd, MMM d yyyy  HH:mm");

            // Render attachments
            AttachmentItems.ItemsSource = msg.Attachments;
            AttachmentsStrip.Visibility =
                msg.Attachments.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Render HTML body (with embedded image support via data URIs)
            string html = BuildHtmlDocument(msg);
            HtmlViewer.NavigateToString(html);
        }

        private static string BuildHtmlDocument(EmailMessage msg)
        {
            string body = msg.BodyHtml;

            // Inject embedded attachment images as data URIs
            foreach (var att in msg.Attachments)
            {
                if (att.ContentType.StartsWith("image/") && att.Data.Length > 0)
                {
                    string b64 = Convert.ToBase64String(att.Data);
                    string dataUri = $"data:{att.ContentType};base64,{b64}";
                    body = body.Replace($"cid:{att.FileName}", dataUri, StringComparison.OrdinalIgnoreCase);
                }
            }

            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; font-size: 14px;
         color: #202124; margin: 20px; line-height: 1.6; }}
  a    {{ color: #1A73E8; }}
  img  {{ max-width: 100%; }}
  blockquote {{ border-left: 3px solid #DADCE0; margin: 0; padding-left: 12px; color: #5F6368; }}
  pre  {{ background: #F8F9FA; padding: 12px; border-radius: 4px; overflow-x: auto; }}
  hr   {{ border: none; border-top: 1px solid #EEEEEE; margin: 16px 0; }}
</style>
</head>
<body>{body}</body>
</html>";
        }

        // ── Attachment chip click ──────────────────────────────────────────

        private void AttachmentChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is EmailAttachment att)
            {
                DownloadCommand?.Execute(att);
            }
        }
    }
}
