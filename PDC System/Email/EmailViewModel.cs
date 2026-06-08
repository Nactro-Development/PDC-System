using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using PDC_System.Models;
using PDC_System.Services;

namespace PDC_System.Email
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public event EventHandler? CanExecuteChanged;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object? p) => _canExecute?.Invoke(p) ?? true;
        public void Execute(object? p) => _execute(p);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;
        public event EventHandler? CanExecuteChanged;
        public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object? p) => !_isExecuting && (_canExecute?.Invoke(p) ?? true);
        public async void Execute(object? p)
        {
            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try { await _execute(p); }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class EmailViewModel : INotifyPropertyChanged
    {
        private readonly EmailDatabase _db;
        private readonly EmailService _svc;
        private CancellationTokenSource _cts = new();
        private System.Timers.Timer? _refreshTimer;

        // ── Observable Collections ────────────────────────────────────────────
        public ObservableCollection<EmailMessage> InboxEmails { get; } = new();
        public ObservableCollection<EmailMessage> SentEmails { get; } = new();

        // ── Compose Fields ────────────────────────────────────────────────────
        private string _composeTo = "";
        private string _composeSubject = "";
        private string _composeBodyHtml = "";
        private string _statusMessage = "";
        private bool _isBusy;
        private bool _isComposeOpen;
        private EmailMessage? _selectedInboxEmail;
        private EmailMessage? _selectedSentEmail;
        private List<(string FileName, byte[] Data, string ContentType)> _pendingAttachments = new();
        private string _attachmentSummary = "No attachments";
        private bool _hasCredentialError;
        private string _signatureHtml = "";
        private bool _isSignatureEditorOpen;

        public string ComposeTo { get => _composeTo; set { _composeTo = value; OnPropertyChanged(); } }
        public string ComposeSubject { get => _composeSubject; set { _composeSubject = value; OnPropertyChanged(); } }
        public string ComposeBodyHtml { get => _composeBodyHtml; set { _composeBodyHtml = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool IsComposeOpen { get => _isComposeOpen; set { _isComposeOpen = value; OnPropertyChanged(); } }
        public bool IsSignatureEditorOpen { get => _isSignatureEditorOpen; set { _isSignatureEditorOpen = value; OnPropertyChanged(); } }
        public string AttachmentSummary { get => _attachmentSummary; set { _attachmentSummary = value; OnPropertyChanged(); } }
        public bool HasCredentialError { get => _hasCredentialError; set { _hasCredentialError = value; OnPropertyChanged(); } }
        public string SignatureHtml { get => _signatureHtml; set { _signatureHtml = value; OnPropertyChanged(); } }

        public EmailMessage? SelectedInboxEmail
        {
            get => _selectedInboxEmail;
            set
            {
                _selectedInboxEmail = value;
                OnPropertyChanged();
                if (value != null) LoadEmailDetails(value);
            }
        }

        public EmailMessage? SelectedSentEmail
        {
            get => _selectedSentEmail;
            set
            {
                _selectedSentEmail = value;
                OnPropertyChanged();
                if (value != null) LoadEmailDetails(value);
            }
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand OpenComposeCommand { get; }
        public ICommand CloseComposeCommand { get; }
        public ICommand SendEmailCommand { get; }
        public ICommand AddAttachmentCommand { get; }
        public ICommand ClearAttachmentsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenSignatureEditorCommand { get; }
        public ICommand SaveSignatureCommand { get; }
        public ICommand DownloadAttachmentCommand { get; }

        public EmailViewModel(EmailDatabase db, EmailService svc)
        {
            _db = db;
            _svc = svc;
            _svc.LoadCredentials();

            OpenComposeCommand = new RelayCommand(_ => OpenCompose());
            CloseComposeCommand = new RelayCommand(_ => CloseCompose());
            SendEmailCommand = new AsyncRelayCommand(async _ => await SendAsync());
            AddAttachmentCommand = new RelayCommand(_ => AddAttachment());
            ClearAttachmentsCommand = new RelayCommand(_ => ClearAttachments());
            RefreshCommand = new AsyncRelayCommand(async _ => await RefreshAllAsync());
            OpenSignatureEditorCommand = new RelayCommand(_ => { IsSignatureEditorOpen = true; SignatureHtml = _db.GetSignature(); });
            SaveSignatureCommand = new RelayCommand(_ => { _db.SaveSignature(SignatureHtml); IsSignatureEditorOpen = false; StatusMessage = "Signature saved."; });
            DownloadAttachmentCommand = new RelayCommand(p => DownloadAttachment(p as EmailAttachment));

            LoadFromDatabase();
            StartAutoRefresh();
        }

        // ── Init ──────────────────────────────────────────────────────────────

        private void LoadFromDatabase()
        {
            InboxEmails.Clear();
            foreach (var m in _db.GetEmails("Inbox")) InboxEmails.Add(m);
            SentEmails.Clear();
            foreach (var m in _db.GetEmails("Sent")) SentEmails.Add(m);
        }

        private void LoadEmailDetails(EmailMessage msg)
        {
            if (!msg.IsRead)
            {
                _db.MarkAsRead(msg.Id);
                msg.IsRead = true;
            }
            msg.Attachments = _db.GetAttachments(msg.Id);
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new System.Timers.Timer(5000);
            _refreshTimer.Elapsed += async (_, _) => await RefreshAllAsync();
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
        }

        public void StopAutoRefresh()
        {
            _refreshTimer?.Stop();
            _cts.Cancel();
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private async Task RefreshAllAsync()
        {
            if (!_svc.HasCredentials)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    HasCredentialError = true;
                    StatusMessage = "⚠ Email credentials not configured. Set PDCEmail and PDCAppPassword in Settings.";
                });
                return;
            }

            HasCredentialError = false;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _svc.FetchInboxAsync(cts.Token);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadFromDatabase();
                    StatusMessage = $"Last synced: {DateTime.Now:HH:mm:ss}";
                });
            }
            catch (OperationCanceledException) { /* timeout — silent */ }
            catch (MailKit.Security.AuthenticationException ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    HasCredentialError = true;
                    StatusMessage = $"⚠ Authentication failed: {ex.Message}";
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    StatusMessage = $"⚠ Sync error: {ex.Message}");
            }
        }

        // ── Compose ───────────────────────────────────────────────────────────

        private void OpenCompose()
        {
            ComposeTo = "";
            ComposeSubject = "";
            ComposeBodyHtml = "";
            _pendingAttachments.Clear();
            UpdateAttachmentSummary();
            IsComposeOpen = true;
        }

        private void CloseCompose()
        {
            IsComposeOpen = false;
        }

        private async Task SendAsync()
        {
            if (string.IsNullOrWhiteSpace(ComposeTo))
            { StatusMessage = "⚠ Please enter a recipient."; return; }
            if (!_svc.HasCredentials)
            { StatusMessage = "⚠ Email credentials not configured."; return; }

            IsBusy = true;
            StatusMessage = "Sending…";
            try
            {
                await _svc.SendEmailAsync(ComposeTo, ComposeSubject, ComposeBodyHtml,
                                           _pendingAttachments);
                LoadFromDatabase();
                CloseCompose();
                StatusMessage = $"✓ Email sent to {ComposeTo}";
            }
            catch (InvalidOperationException ex) // attachment size
            {
                StatusMessage = $"⚠ {ex.Message}";
            }
            catch (MailKit.Security.AuthenticationException)
            {
                StatusMessage = "⚠ Authentication failed. Check PDCEmail / PDCAppPassword in Settings.";
                HasCredentialError = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"⚠ Failed to send: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        // ── Attachments ───────────────────────────────────────────────────────

        private void AddAttachment()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Title = "Select Attachments"
            };
            if (dlg.ShowDialog() != true) return;

            long currentSize = _pendingAttachments.Sum(a => (long)a.Data.Length);
            foreach (var path in dlg.FileNames)
            {
                var data = File.ReadAllBytes(path);
                currentSize += data.Length;
                if (currentSize > 25 * 1024 * 1024)
                {
                    StatusMessage = "⚠ Adding this file would exceed the 25 MB attachment limit.";
                    return;
                }
                string ext = Path.GetExtension(path).ToLowerInvariant();
                string mime = ext switch
                {
                    ".pdf" => "application/pdf",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".zip" => "application/zip",
                    _ => "application/octet-stream"
                };
                _pendingAttachments.Add((Path.GetFileName(path), data, mime));
            }
            UpdateAttachmentSummary();
        }

        private void ClearAttachments()
        {
            _pendingAttachments.Clear();
            UpdateAttachmentSummary();
        }

        private void UpdateAttachmentSummary()
        {
            if (_pendingAttachments.Count == 0)
            { AttachmentSummary = "No attachments"; return; }
            long total = _pendingAttachments.Sum(a => (long)a.Data.Length);
            AttachmentSummary = $"{_pendingAttachments.Count} file(s) — {total / 1024.0 / 1024.0:F1} MB";
        }

        private void DownloadAttachment(EmailAttachment? att)
        {
            if (att == null) return;
            var dlg = new Microsoft.Win32.SaveFileDialog { FileName = att.FileName };
            if (dlg.ShowDialog() != true) return;
            File.WriteAllBytes(dlg.FileName, att.Data);
            StatusMessage = $"✓ Saved {att.FileName}";
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
