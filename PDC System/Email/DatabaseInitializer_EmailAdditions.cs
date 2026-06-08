// ─────────────────────────────────────────────────────────────────────────────
// ADD THE FOLLOWING TABLES TO YOUR DatabaseInitializer.cs Initialize() METHOD
// Insert them after your existing CREATE TABLE statements.
// ─────────────────────────────────────────────────────────────────────────────

/*
CREATE TABLE IF NOT EXISTS Email (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Folder        TEXT    NOT NULL DEFAULT 'Inbox',
    FromAddress   TEXT    NOT NULL DEFAULT '',
    ToAddress     TEXT    NOT NULL DEFAULT '',
    Subject       TEXT    NOT NULL DEFAULT '',
    BodyHtml      TEXT    NOT NULL DEFAULT '',
    BodyText      TEXT    NOT NULL DEFAULT '',
    DateReceived  TEXT    NOT NULL,
    IsRead        INTEGER NOT NULL DEFAULT 0,
    HasAttachments INTEGER NOT NULL DEFAULT 0,
    UniqueId      TEXT    NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS EmailAttachments (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    EmailId     INTEGER NOT NULL,
    FileName    TEXT    NOT NULL DEFAULT '',
    ContentType TEXT    NOT NULL DEFAULT '',
    Data        BLOB    NOT NULL,
    Size        INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (EmailId) REFERENCES Email(Id) ON DELETE CASCADE
);

-- Settings table must already exist; insert defaults if keys absent:
INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('PDCEmail', '');
INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('PDCAppPassword', '');
INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('EmailSignature', '');
*/

// ─────────────────────────────────────────────────────────────────────────────
// HOW TO WIRE UP EmailWindow IN YOUR MAIN SHELL / NAVIGATION
// ─────────────────────────────────────────────────────────────────────────────

/*
// 1. In your shell or MainWindow, where you initialize services:

var connectionString = $"Data Source={dbPath}";
var emailDb  = new PDC_System.Services.EmailDatabase(connectionString);
var emailSvc = new PDC_System.Services.EmailService(emailDb);

// 2. When navigating to the Email tab / page:

var emailWindow = new PDC_System.Email.EmailWindow();
emailWindow.Initialize(emailDb, emailSvc);

// Add to your content area, e.g.:
//   MainContentArea.Content = emailWindow;
// or place <local:EmailWindow x:Name="EmailView"/> in XAML and call
//   EmailView.Initialize(emailDb, emailSvc);  from the constructor.
*/

// ─────────────────────────────────────────────────────────────────────────────
// NUGET PACKAGES REQUIRED  (add to your .csproj)
// ─────────────────────────────────────────────────────────────────────────────

/*
<PackageReference Include="MailKit"            Version="4.*" />
<PackageReference Include="MimeKit"            Version="4.*" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="8.*" />
*/

// ─────────────────────────────────────────────────────────────────────────────
// GMAIL SETUP NOTES
// ─────────────────────────────────────────────────────────────────────────────

/*
For Gmail:
  1. Enable 2-Step Verification on the Google account.
  2. Go to Google Account → Security → App Passwords.
  3. Create an App Password (select "Mail" + device).
  4. Store the 16-char password in Settings.PDCAppPassword.
  5. Store the full Gmail address in Settings.PDCEmail.

For other providers change ImapHost/SmtpHost constants in EmailService.cs.
*/
