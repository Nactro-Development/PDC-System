# PDC System - User Manual
<img width="1919" height="1078" alt="image" src="https://github.com/user-attachments/assets/2dbb644e-c8e1-4347-9121-20fe912cedb5" />

![PDC System](https://img.shields.io/badge/PDC%20System-v2025-blue.svg)
![WPF](https://img.shields.io/badge/WPF-.NET%208-green.svg)
![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)

---
<img width="1916" height="1028" alt="image" src="https://github.com/user-attachments/assets/fa08aeac-1a62-4d74-ad3d-2d332f895688" />
<img width="1914" height="1022" alt="image" src="https://github.com/user-attachments/assets/0ccd9dfd-d6cf-4efb-8225-c5e2337d9d04" />
<img width="1912" height="1027" alt="image" src="https://github.com/user-attachments/assets/bb34ff04-1042-484b-bf1a-0d2a548d77fd" />

## 📖 Table of Contents

1. [System Overview](#system-overview)
2. [Getting Started](#getting-started)
3. [User Authentication & Access Control](#user-authentication--access-control)
4. [Main Dashboard](#main-dashboard)
5. [Customer Management](#customer-management)
6. [Job Card Management](#job-card-management)
7. [Employee Management](#employee-management)
8. [Attendance Management](#attendance-management)
9. [Payroll & Paysheet Management](#payroll--paysheet-management)
10. [Quotation Management](#quotation-management)
11. [Order Management](#order-management)
12. [Outsourcing Management](#outsourcing-management)
13. [Backup & Data Security](#backup--data-security)
14. [System Settings](#system-settings)
15. [Troubleshooting](#troubleshooting)

---

## 📋 System Overview

**PDC System** is a comprehensive **Printing and Document Center Management System** built with **C# WPF** and **.NET 8**. It's designed to manage all aspects of a printing business including customer management, job tracking, employee management, payroll, attendance, and financial operations.

### Key Features:
- 🏢 **Complete Business Management**: Customer to payroll in one system
- 👥 **Multi-User Support**: Role-based access control
- 📊 **Real-time Reporting**: Attendance, payroll, and business reports
- 🔒 **Secure Data**: Encrypted user data and automatic backups
- 📧 **Email Integration**: Automated reports and paysheet delivery
- 🎨 **Modern UI**: Professional WPF interface with themes

---

## 🚀 Getting Started

### System Requirements
- **Operating System**: Windows 10/11
- **Framework**: .NET 8 Runtime
- **Database**: SQL Server (for attendance data)
- **Email**: SMTP configuration for reports
- **Storage**: Minimum 1GB free space

### First Time Setup

1. **Launch the Application**
   - Double-click `PDC System.exe`
   - The system will create necessary folders and files on first run

2. **Default Login Credentials**
   - **Username**: `admin`
   - **Password**: `admin`
   - ⚠️ **Important**: Change default credentials immediately after first login

3. **Initial Configuration**
   - System will automatically seed admin user
   - Configure settings through the Settings menu
   - Set up email configurations for reports

---

## 🔐 User Authentication & Access Control

### Login Process
1. Enter username and password on the login screen
2. System will validate credentials against encrypted user database
3. If startup checks are enabled, the system will verify:
   - iVMS-4200 Framework (for attendance)
   - SQL Server connection
   - Network connectivity

### User Roles & Permissions

The system supports granular permission control for different modules:

| Module | Description | Permission Required |
|--------|-------------|-------------------|
| **Dashboard** | Home screen access | `Dashboard` |
| **Order Check** | Order management | `OrderCheck` |
| **Job Card** | Job card creation/management | `Jobcard` |
| **Customer** | Customer database | `Customer` |
| **Outsourcing** | External job management | `Outsourcing` |
| **Quotation** | Quote generation | `Quotation` |
| **Employee** | Employee management | `Employee` |
| **Attendance** | Time tracking | `Attendance` |
| **Payroll** | Salary calculations | `Payroll` |
| **Paysheet** | Payslip generation | `Paysheet` |
| **User Manager** | User account management | `UserManager` |

### Managing Users (Admin Only)

1. **Navigate to User Manager**
   - Click the "User Manager" tab (visible only to users with UserManager permission)

2. **Create New User**
   - Click "Create User" button
   - Enter username and password
   - Select permissions for each module
   - Click "Save"

3. **Edit Existing User**
   - Select user from the list
   - Click "Edit" button
   - Modify permissions as needed
   - Click "Update User"

4. **Delete User**
   - Select user from the list
   - Click "Delete" button
   - Confirm deletion

---

## 🏠 Main Dashboard

The dashboard provides a central hub for all system operations.

### Dashboard Features

1. **Real-time Clock**
   - 12-hour format display
   - Automatic updates every second

2. **Employee Birthday Tracker**
   - Shows upcoming birthdays (30-day window)
   - Countdown to next birthday
   - Automatic notifications

3. **Quick Access Menu**
   - Navigate to all modules with single click
   - Access based on user permissions

4. **Google Account Integration**
   - View logged-in Google account info
   - Profile picture display
   - Account management options

5. **System Tray Icon**
   - Minimize to system tray
   - Quick access menu from tray
   - Background operation support

### Navigation Menu

| Button | Function | Shortcut |
|--------|----------|----------|
| 🏠 Dashboard | Return to home screen | Home |
| 👥 Customer | Customer management | F1 |
| 📄 Job Card | Job tracking | F2 |
| 💰 Quotation | Quote generation | F3 |
| 👤 Employee | Staff management | F4 |
| ⏰ Attendance | Time tracking | F5 |
| 💼 Payroll | Salary management | F6 |
| 📊 Paysheet | Payslip generation | F7 |
| 📋 Orders | Order tracking | F8 |
| 🏭 Outsourcing | External jobs | F9 |
| ⚙️ Settings | System configuration | F10 |

---

## 👥 Customer Management

Manage your customer database with comprehensive contact information.

### Adding New Customer

1. **Navigate to Customer Module**
   - Click "Customer" from the main menu

2. **Add Customer**
   - Click "Add Customer" button
   - Fill in the customer information form:
     - **Name**: Customer full name (required)
     - **Company Name**: Business name (optional)
     - **Address**: Full address
     - **Contact No**: Primary phone number
     - **Email**: Email address
     - **Type**: Customer type (Company/Individual)

3. **Save Customer**
   - Click "Save" to add to database
   - Customer will appear in the customer list

### Managing Customers

1. **View Customer List**
   - All customers displayed in data grid
   - Search functionality available
   - Sort by any column

2. **Edit Customer**
   - Double-click customer row
   - Modify information in edit form
   - Click "Save" to update

3. **Delete Customer**
   - Select customer from list
   - Click "Delete" button
   - Confirm deletion in dialog

### Customer Data Export
- Customer data automatically saved to `Savers/customers.json`
- Regular backups included in system backup

---

## 📄 Job Card Management

Track printing jobs from order to completion with detailed specifications.

### Creating Job Card

1. **Navigate to Job Card Module**
   - Click "Job Card" from main menu

2. **New Job Card**
   - Click "Add Job Card" button
   - Fill in job details:

#### Basic Information
- **Customer Name**: Select from existing customers or add new
- **Job Description**: Detailed job specifications
- **Job Number**: Auto-generated unique identifier
- **Type**: Select job type:
  - **Company**: Corporate printing
  - **Person**: Individual customer
  - **Digital**: Digital printing
  - **Offset**: Offset printing

#### Paper Specifications
- **Paper Size**: A4, A3, A5, Letter, etc.
- **GSM**: Paper weight (80, 120, 150, etc.)
- **Paper Type**: Matte, Glossy, Bond, etc.

#### Printing Details
- **Quantity**: Total copies needed
- **Printed**: Completed quantity (for tracking progress)
- **Duplex**: Single/Double sided
- **Laminate**: Lamination requirements

#### Additional Options
- **Special Notes**: Custom requirements
- **Screenshot**: Attach reference images
- **Plate Quantity**: For offset printing
- **Company Details**: Outsourcing company info

3. **Save Job Card**
   - Click "Save" to create job card
   - System generates unique job number

### Job Card Features

1. **Job Types**

   **Digital Printing**
   - Full specification tracking
   - Digital company assignment
   - Progress monitoring

   **Offset Printing**
   - Simplified view (Name, Description, Quantity)
   - Plate quantity tracking
   - Plate company details

2. **Progress Tracking**
   - Mark jobs as seen/unseen
   - Track completion percentage
   - Status updates

3. **Integration Features**
   - **Create Quotation**: Generate quote from job
   - **Create Invoice**: Bill customer directly
   - **Print Job Card**: Physical job card printing

### Job Card Viewing

1. **Job Card List**
   - View all job cards in data grid
   - Filter by customer, date, type
   - Search functionality

2. **Job Card Details**
   - Double-click job to view details
   - Full specification display
   - Attached images/screenshots

3. **Export Options**
   - Print job card
   - Export to PDF
   - Email job details

---

## 👤 Employee Management

Comprehensive employee database with personal and payroll information.

### Adding New Employee

1. **Navigate to Employee Module**
   - Click "Employee" from main menu

2. **Add Employee**
   - Click "Add Employee" button
   - Complete employee information form:

#### Personal Information
- **Name**: Full name (required)
- **Employee ID**: Unique identifier (required)
- **Email**: Employee email address
- **Job Role**: Position/designation
- **Department**: Department assignment
- **Birthday**: Date of birth

#### Address Information
- **Address 1**: Primary address line
- **Address 2**: Secondary address line
- **City**: Select from dropdown list
- **Province**: Province/state

#### Contact Information
- **Contact 1**: Primary phone number
- **Contact 2**: Secondary phone number
- **National ID**: NIC number

#### Employment Details
- **Valid From**: Employment start date
- **Valid To**: Employment end date
- **Working Days**: Select working days:
  - Monday through Sunday checkboxes
  - Individual day selection

#### Salary Information
- **Basic Salary**: Base salary amount
- **Total Salary**: Complete salary package
- **Overtime Rate**: Hourly overtime rate
- **Double Overtime Rate**: Holiday/Sunday rate
- **Absent Rate**: Daily absent deduction
- **No-pay Rate**: No-pay leave rate

#### Working Hours
- **Check-in Time**: Standard check-in (HH:MM)
- **Check-out Time**: Standard check-out (HH:MM)
- **Saturday Check-in**: Weekend check-in time
- **Saturday Check-out**: Weekend check-out time

3. **Profile Picture**
   - Click "Import Image" to add photo
   - Supports JPG, PNG, BMP formats
   - Auto-numbered storage system

4. **Save Employee**
   - Click "Save" to add employee
   - System validates all required fields

### Managing Employees

1. **Employee List**
   - View all employees in data grid
   - Search by name or ID
   - Filter capabilities

2. **Edit Employee**
   - Select employee from list
   - Click "Edit" button
   - Modify information
   - System shows changed fields summary

3. **Employee Profile**
   - View complete employee profile
   - Access attendance records
   - Generate reports

### Data Validation

The system includes comprehensive validation:
- **Email Format**: Valid email address required
- **Salary Fields**: Positive numbers only
- **Time Format**: Valid HH:MM format
- **ID Uniqueness**: No duplicate employee IDs
- **Required Fields**: Name, ID, basic salary mandatory

---

## ⏰ Attendance Management

Advanced attendance tracking with integration to fingerprint systems.

### Attendance System Overview

The attendance system integrates with **iVMS-4200 Framework** for fingerprint data and provides:
- Real-time attendance calculation
- Overtime tracking
- Late/early departure monitoring
- Holiday management
- Manual attendance editing

### Attendance Data Sources

1. **Fingerprint Integration**
   - Connects to iVMS-4200 database
   - Automatic punch data retrieval
   - Real-time fingerprint processing

2. **Manual Entry**
   - Manual attendance records
   - Corrections and adjustments
   - Override automatic calculations

### Viewing Attendance

1. **Navigate to Attendance Module**
   - Click "Attendance" from main menu

2. **Attendance Records**
   - View attendance in data grid format
   - Columns displayed:
     - **Employee ID**: Unique identifier
     - **Name**: Employee name
     - **Date**: Attendance date
     - **Check-in**: First fingerprint time
     - **Check-out**: Last fingerprint time
     - **Overtime**: Calculated overtime hours
     - **Double OT**: Holiday/Sunday overtime
     - **Early Leave**: Early departure time
     - **Late Hours**: Late arrival time
     - **Status**: Attendance status

### Attendance Calculation Rules

#### Normal Working Days
- **Check-in**: Compared to scheduled time
- **Check-out**: Compared to scheduled time
- **Overtime**: Time before/after scheduled hours
- **Late Allowance**: Configurable grace period
- **Early Leave**: Departure before scheduled time

#### Special Days
- **Saturdays**: Different schedule if configured as working day
- **Sundays**: All hours count as double overtime
- **Holidays**: All hours count as double overtime
- **Non-working Days**: All hours count as double overtime

#### Status Indicators
- **OK**: Normal attendance
- **Absent**: No fingerprint records
- **Missing Finger Print**: Incomplete records
- **Holiday**: Designated holiday
- **Non-Working Day**: Employee's off day

### Manual Attendance Editing

1. **Edit Attendance**
   - Select attendance record
   - Click "Edit" button
   - Modify attendance details:
     - Check-in time
     - Check-out time
     - Override calculations

2. **Manual Edit Indicator**
   - Records marked as "Manual Edit"
   - Higher priority than automatic calculations
   - Preserved during recalculations

3. **Reset Manual Edits**
   - Option to reset to automatic calculation
   - Date range selection for bulk reset
   - Individual record reset

### Attendance Reports

1. **Daily Reports**
   - Automated daily attendance emails
   - Present/absent employee lists
   - First check-in times
   - HTML formatted reports

2. **Period Reports**
   - Custom date range selection
   - Attendance summary reports
   - Export to PDF/Excel

### Holiday Management

1. **Holiday Configuration**
   - Add national/company holidays
   - Holiday names and dates
   - Automatic overtime calculation on holidays

2. **Poya Days**
   - Buddhist calendar integration
   - Automatic holiday detection
   - Special overtime rates

---

## 💼 Payroll & Paysheet Management

Comprehensive payroll system with automated calculations and payslip generation.

### Payroll Structure

The payroll system handles:
- **Basic Salary**: Base pay amount
- **Overtime**: Regular overtime calculations
- **Double Overtime**: Holiday/Sunday premium rates
- **Earnings**: Additional income (bonuses, allowances)
- **Deductions**: Loans, EPF, penalties, custom deductions
- **Attendance**: Absent day penalties

### Creating Paysheet

1. **Navigate to Paysheet Module**
   - Click "Paysheet" from main menu

2. **New Paysheet**
   - Click "Add Paysheet" button
   - Select employee from dropdown
   - Choose date range (Start Date to End Date)
   - Select month for paysheet

3. **Paysheet Configuration**
   
   #### Include Options
   - ☑️ **Earnings**: Include additional earnings
   - ☑️ **Deductions**: Include custom deductions
   - ☑️ **Loan**: Include loan deductions
   - ☑️ **EPF**: Include EPF contributions

   #### Custom Settings
   - **No-pay Days**: Manual adjustment for no-pay leaves
   - **Loan Amount**: Custom loan deduction amount

4. **Automatic Calculations**
   
   System automatically calculates:
   
   #### Attendance Summary
   - Working days in period
   - Absent days
   - Overtime hours (formatted as HH:MM)
   - Double overtime hours
   - Late hours
   - Early leave hours

   #### Earnings Calculation
   ```
   Total Overtime = (Overtime Hours × OT Rate)
   Total Double OT = (Double OT Hours × Double OT Rate)
   Additional Earnings = Sum of selected earnings
   Total Earnings = Basic Salary + Overtime + Double OT + Additional Earnings
   ```

   #### Deductions Calculation
   ```
   Absent Deductions = (Absent Days - No-pay Days) × Absent Rate
   EPF Contribution = Employee EPF amount
   Loan Deduction = Monthly loan payment
   Custom Deductions = Sum of selected deductions
   Total Deductions = Absent + EPF + Loan + Custom Deductions
   ```

   #### Final Calculation
   ```
   Net Salary = Total Earnings - Total Deductions
   ```

### Payroll Components Management

#### Loans Management
1. **Add Loan**
   - Navigate to Payroll Details → Loans
   - Click "Add Loan"
   - Enter loan details:
     - Employee selection
     - Loan amount
     - Monthly installment
     - Interest rate (if applicable)

2. **Loan Tracking**
   - Automatic remaining balance calculation
   - Monthly deduction tracking
   - Loan history maintenance
   - Status: Active/Finished

3. **Loan History**
   - View payment history
   - Generate loan statements
   - Email loan reports to employees

#### Earnings Management
1. **Add Earnings**
   - Navigate to Payroll Details → Earnings
   - Click "Add Earning"
   - Enter earning details:
     - Employee selection
     - Description
     - Amount
     - Effective date

2. **Earning Types**
   - Bonuses
   - Allowances
   - Overtime payments
   - Commission
   - Special payments

#### Deductions Management
1. **Add Deductions**
   - Navigate to Payroll Details → Deductions
   - Click "Add Deduction"
   - Enter deduction details:
     - Employee selection
     - Description
     - Amount
     - Effective date

2. **Deduction Types**
   - Salary advances
   - Uniform costs
   - Damage charges
   - Fine penalties
   - Other deductions

#### EPF (Employee Provident Fund) Management
1. **Setup EPF**
   - Navigate to Payroll Details → EPF
   - Click "Add EPF"
   - Configure EPF details:
     - Employee selection
     - Basic salary for EPF
     - Employee contribution (8%)
     - Employer contribution (12%)

2. **EPF Calculations**
   ```
   Employee Contribution = Basic Salary × 8%
   Employer Contribution = Basic Salary × 12%
   Total EPF = Employee + Employer Contribution
   ```

3. **EPF Reports**
   - Monthly EPF summaries
   - Employee contribution tracking
   - Employer liability reports
   - Export to accounting systems

### Paysheet Generation

#### Save Paysheet
1. Click "Save Paysheet" to save data only
2. Paysheet stored in system database
3. Available for future editing

#### Generate PDF Paysheet
1. Click "Save & Generate PDF"
2. Professional payslip creation
3. Two-page document:
   - **Page 1**: Summary payslip
   - **Page 2**: Detailed attendance records

#### PDF Content
**Page 1 - Payslip Summary**
- Employee information
- Salary breakdown
- Earnings itemization
- Deductions itemization
- Net salary calculation

**Page 2 - Attendance Details**
- Daily attendance records
- Check-in/check-out times
- Overtime calculations
- Status for each day

#### Email Delivery
1. Enable "Send Email" checkbox
2. Automatic email to employee
3. PDF attachment included
4. Professional email template

### Paysheet Management

#### View Paysheets
- List all generated paysheets
- Search by employee or month
- Filter by date range

#### Edit Paysheets
- Select existing paysheet
- Modify calculations
- Regenerate PDF
- Update loan/EPF records

#### Paysheet History
- Complete payroll history
- Employee paysheet tracking
- Audit trail maintenance

---

## 💰 Quotation Management

Professional quotation system for customer proposals and pricing.

### Creating Quotations

1. **Navigate to Quotation Module**
   - Click "Quotation" from main menu

2. **New Quotation**
   - Click "Add Quotation" button
   - System auto-generates quotation number

3. **Quotation Setup**
   
   #### Customer Selection
   - Choose from existing customer database
   - Auto-fills customer information:
     - Name and address
     - Contact details
     - Company information

   #### Date Configuration
   - **Issue Date**: Quotation creation date
   - **Valid Date**: Quotation expiration date
   - **Validity Period**: Auto-calculated days

### Quotation Items

#### Adding Items
1. **Regular Items**
   - Click "Add Item"
   - Enter item details:
     - **Description**: Service/product description
     - **Quantity**: Number of units
     - **Unit Price**: Price per unit
     - **Total**: Auto-calculated (Qty × Unit Price)

2. **Section Titles**
   - Click "Add Title"
   - Create section headers for organization
   - No pricing for titles

#### Item Management
- **Edit Items**: Click on item to modify
- **Delete Items**: Remove unwanted items
- **Reorder Items**: Drag and drop functionality

### Quotation Features

#### Automatic Calculations
- Real-time total calculation
- Currency formatting (LKR)
- Running total display

#### Professional Layout
- Company branding
- Customer information display
- Itemized pricing table
- Terms and conditions

### Quotation Output

#### PDF Generation
1. Click "Preview & Save PDF"
2. Professional PDF creation
3. Company letterhead
4. Structured pricing table
5. Terms and validity notice

#### Email Delivery
1. Enter customer email address
2. Click "Send Email"
3. Automatic PDF attachment
4. Professional email template

#### PDF Content Structure
```
QUOTATION

Quotation No: [Auto-generated]
Issue Date: [Selected date]
Valid Date: [Selected date]

CUSTOMER INFORMATION          |  COMPANY INFORMATION
Name: [Customer name]         |  PRIYANTHA DIE CUTTING
Address: [Customer address]   |  No.07, Waidaya Vidayala Mawatha
Contact: [Phone number]       |  Rajagiriya
                             |  072 297 8667 | 011 864 267
                             |  priyanthadiecutting@gmail.com

ITEMS
+-------------+-----+-----------+-----------+
| Description | Qty | Unit(LKR) | Total(LKR)|
+-------------+-----+-----------+-----------+
| [Item 1]    | [#] | [Price]   | [Total]   |
| [Item 2]    | [#] | [Price]   | [Total]   |
+-------------+-----+-----------+-----------+
                     Total Amount: [Total] LKR

Terms:
- Price valid for [X] days
- Cheque payable to "PRIYANTHA DIE CUTTING"

Prepared By: Priyantha De Costa
Generated on: [Date/Time]
```

### Quotation Management

#### View Quotations
- List all quotations
- Search by number/customer
- Filter by date range
- Status tracking

#### Edit Quotations
- Modify existing quotations
- Update pricing
- Regenerate PDFs
- Resend emails

#### Quotation Integration
- **Convert to Job**: Create job card from quotation
- **Generate Invoice**: Create invoice from accepted quote
- **Track Status**: Quote acceptance/rejection tracking

---

## 📋 Order Management

Track customer orders with deadline management and progress monitoring.

### Order Creation

1. **Navigate to Order Module**
   - Click "Orders" from main menu

2. **Add New Order**
   - Click "Add Order"
   - Enter order information:
     - **Customer Name**: Select or type customer
     - **Order Description**: Detailed requirements
     - **Due Date**: Delivery deadline
     - **Notes**: Special instructions

### Order Tracking

#### Order Status
- **In Progress**: Active orders
- **Completed**: Finished orders
- **Overdue**: Past deadline orders

#### Deadline Management
- **Countdown Timer**: Days, hours, minutes remaining
- **Priority Indicators**: Color-coded urgency
- **Deadline Alerts**: Automatic notifications

#### Progress Updates
- Mark orders as complete
- Add progress notes
- Update completion percentage

### Order Features

#### Time Calculations
```
Time Remaining = Due Date - Current Date/Time
Status = "X days, Y hours, Z minutes remaining"
Overdue = "X days overdue" (red indicator)
Completed = "Finished" (green indicator)
```

#### Order Integration
- **Link to Job Cards**: Connect orders to production
- **Customer Communication**: Email updates
- **Invoice Generation**: Bill completed orders

---

## 🏭 Outsourcing Management

Manage external printing jobs and partner companies.

### Outsourcing Setup

1. **Navigate to Outsourcing Module**
   - Click "Outsourcing" from main menu

2. **Partner Management**
   - Add outsourcing companies
   - Company contact details
   - Service capabilities
   - Pricing agreements

### Job Outsourcing

#### Sending Jobs
1. Select job for outsourcing
2. Choose partner company
3. Set delivery timeline
4. Add special instructions

#### Tracking Outsourced Jobs
- Job status monitoring
- Delivery tracking
- Quality control
- Partner communication

### Outsourcing Features

#### Cost Management
- Partner pricing tracking
- Cost comparison
- Profit margin analysis
- Payment scheduling

#### Quality Control
- Partner rating system
- Delivery reliability
- Quality assessments
- Performance metrics

---

## 💾 Backup & Data Security

Comprehensive data protection and backup solutions.

### Automatic Backup System

#### Backup Schedule
- **Interval**: Configurable (default: 60 minutes)
- **Files Backed Up**:
  - `employee.json` - Employee database
  - `attendance.json` - Attendance records  
  - `customers.json` - Customer database
  - `jobcards.json` - Job card database
  - `orders.json` - Order tracking
  - `savedpdfs.json` - Generated documents
  - `PaysheetData.json` - Payroll data
  - `quoteSettings.json` - System settings

#### Backup Locations
- **Local**: `C:\Backups` folder
- **Cloud**: Google Drive integration
- **Network**: Configurable network paths

### Manual Backup

1. **Access Backup Window**
   - Right-click system tray icon
   - Select "Backups"

2. **Manual Backup**
   - Click "Manual Backup"
   - Instant backup of all data files
   - Status confirmation

3. **Restore Backup**
   - Click "Restore Backup"
   - Replace current data with backup
   - Confirmation required

### Data Security

#### User Data Encryption
- **Password Hashing**: SHA-256 encryption
- **User File**: `users.dat` encrypted storage
- **Secret Key**: Configurable encryption key

#### Data Integrity
- **JSON Validation**: File format verification
- **Backup Verification**: Integrity checking
- **Error Recovery**: Automatic error handling

#### Google Drive Integration
- **OAuth Authentication**: Secure Google login
- **Automatic Sync**: Scheduled cloud backups
- **Version Control**: Multiple backup versions
- **Encryption**: Encrypted cloud storage

### Backup Configuration

#### Settings Access
1. Navigate to Settings → Backup
2. Configure backup intervals
3. Set backup destinations
4. Enable/disable automatic backup

#### Google Drive Setup
1. **Authentication**
   - Click "Connect Google Drive"
   - Complete OAuth process
   - Grant necessary permissions

2. **Sync Settings**
   - Enable automatic cloud backup
   - Set sync frequency
   - Choose folders to sync

---

## ⚙️ System Settings

Configure system behavior and integration options.

### Accessing Settings
1. Click "Settings" from main menu
2. Or click gear icon in dashboard

### Configuration Sections

#### 1. Email Configuration
**SMTP Settings**
- **SMTP Server**: Mail server address
- **Port**: Server port (usually 587 for TLS)
- **Username**: Email account username
- **Password**: Email account password
- **Enable SSL/TLS**: Security encryption

**Daily Reports**
- ☑️ **Send Daily Reports**: Enable automatic daily attendance emails
- **Report Time**: Schedule for daily email delivery
- **Recipients**: Email addresses for reports

**Email Templates**
- Customize email subjects
- Modify email body templates
- Add company branding

#### 2. Attendance Settings
**Working Hours**
- **Standard Check-in**: Default employee check-in time
- **Standard Check-out**: Default employee check-out time
- **Late Allowance**: Grace period for late arrivals (minutes)
- **OT Rounding**: Round overtime to nearest minutes (5, 10, 15, 30)

**Calculation Rules**
- **Overtime Calculation**: Before/after hours method
- **Double OT Rules**: Holiday and Sunday calculations
- **Late Penalties**: Late arrival deduction rules
- **Early Leave**: Early departure calculations

#### 3. System Integration
**iVMS Integration**
- **Enable iVMS**: Connect to fingerprint system
- **Database Connection**: iVMS database settings
- **Sync Frequency**: Data synchronization interval
- **Employee Mapping**: Match employee IDs

**SQL Server Settings**
- **Server Address**: Database server location
- **Database Name**: Attendance database name
- **Authentication**: Windows/SQL authentication
- **Connection String**: Custom connection parameters

#### 4. Application Settings
**Startup Behavior**
- ☑️ **Start with Windows**: Auto-start application
- ☑️ **Minimize to Tray**: Start minimized
- ☑️ **Run Startup Checks**: Verify system components

**Timer Settings**
- **Calculation Interval**: Automatic recalculation frequency
- **Backup Interval**: Automatic backup frequency
- **Notification Interval**: Alert frequency

**Theme Settings**
- **Light/Dark Mode**: UI theme selection
- **Accent Colors**: Custom color schemes
- **Font Settings**: UI font customization

#### 5. Company Information
**Business Details**
- **Company Name**: Business name for documents
- **Address**: Complete business address
- **Contact Numbers**: Phone/mobile numbers
- **Email**: Business email address
- **Website**: Company website URL

**Legal Information**
- **Registration Number**: Business registration
- **Tax Number**: Tax identification
- **Bank Details**: Banking information for invoices

### Settings Management

#### Save Settings
- Click "Save" to apply changes
- Settings immediately effective
- Configuration stored in `quoteSettings.json`

#### Load Default Settings
- Click "Load Defaults" to reset
- Restores factory settings
- Confirmation required

#### Export/Import Settings
- **Export**: Save settings to file
- **Import**: Load settings from file
- **Share**: Deploy settings across multiple installations

---

## 🔧 Troubleshooting

Common issues and their solutions.

### Login Issues

#### Problem: Cannot login with admin/admin
**Solution:**
1. Ensure caps lock is off
2. Check if `users.dat` file exists in `Savers` folder
3. Delete `users.dat` to reset to default admin account
4. Restart application

#### Problem: Forgot password
**Solution:**
1. Contact system administrator
2. Administrator can reset password through User Manager
3. Or delete user file to reset (data loss possible)

### Attendance Issues

#### Problem: No attendance data showing
**Possible Causes:**
1. **iVMS Not Running**: Ensure iVMS-4200 Framework is running
2. **SQL Connection**: Check SQL Server connectivity
3. **Employee Mapping**: Verify employee IDs match fingerprint system
4. **Date Range**: Check if viewing correct date range

**Solutions:**
1. Restart iVMS-4200 Framework
2. Check SQL Server service
3. Verify database connection string
4. Refresh attendance data manually

#### Problem: Incorrect overtime calculations
**Check:**
1. Employee working hours setup
2. Overtime calculation settings
3. Holiday configuration
4. Manual edits overriding calculations

### Email Issues

#### Problem: Cannot send emails
**Solutions:**
1. **SMTP Configuration**: Verify email settings
2. **Firewall**: Check firewall blocking SMTP
3. **Authentication**: Confirm email credentials
4. **SSL/TLS**: Ensure proper encryption settings

#### Problem: Emails going to spam
**Solutions:**
1. Add sender to recipient's contact list
2. Configure SPF/DKIM records for domain
3. Use established email service providers
4. Check email content for spam triggers

### Payroll Issues

#### Problem: Incorrect salary calculations
**Check:**
1. Employee salary configuration
2. Attendance data accuracy
3. Overtime rates setup
4. Deduction configurations
5. Date range selection

#### Problem: PDF generation fails
**Solutions:**
1. Ensure sufficient disk space
2. Check write permissions to output folder
3. Verify QuestPDF license configuration
4. Restart application if persistent

### Performance Issues

#### Problem: Slow application performance
**Solutions:**
1. **Large Data Sets**: Archive old data
2. **Memory**: Increase system RAM
3. **Database**: Optimize database indexes
4. **Background Processes**: Reduce concurrent operations

#### Problem: High memory usage
**Solutions:**
1. Restart application regularly
2. Reduce data display limits
3. Clear temporary files
4. Optimize image file sizes

### Data Issues

#### Problem: Data corruption or loss
**Recovery Steps:**
1. **Recent Backup**: Restore from latest backup
2. **Cloud Sync**: Download from Google Drive
3. **Manual Recovery**: Check backup history files
4. **Contact Support**: For professional recovery

#### Problem: Duplicate entries
**Solutions:**
1. **Manual Cleanup**: Remove duplicates manually
2. **Data Validation**: Enable stricter validation
3. **Import Controls**: Check import procedures
4. **Database Constraints**: Add uniqueness constraints

### Network Issues

#### Problem: Google Drive sync failing
**Solutions:**
1. **Internet Connection**: Check network connectivity
2. **Authentication**: Re-authenticate Google account
3. **Permissions**: Verify Google Drive permissions
4. **Firewall**: Check network restrictions

### Getting Help

#### System Logs
1. Check `backup_history.txt` for backup status
2. Review application event logs
3. Monitor system tray notifications
4. Check error message dialogs

#### Support Information
- **Version**: Check About dialog for version info
- **System**: Document operating system version
- **Configuration**: Export settings for support
- **Screenshots**: Capture error messages

#### Contact Support
- Include system information
- Provide error screenshots
- Describe steps to reproduce issue
- Attach relevant log files

---

## 📞 Support & Resources

### System Information
- **Version**: PDC System 2025
- **Framework**: .NET 8 with WPF
- **License**: Apache 2.0 Open Source
- **GitHub**: [PDC-System Repository](https://github.com/kavidu-kaushalya/PDC-System)

### Documentation
- **User Manual**: This document
- **API Documentation**: Available in source code
- **Video Tutorials**: Available on request
- **Quick Reference**: Keyboard shortcuts and tips

### Community
- **GitHub Issues**: Report bugs and feature requests
- **Discussions**: Community support and questions
- **Contributions**: Welcome improvements and translations

---

## 📋 Appendix

### Keyboard Shortcuts
| Shortcut | Function |
|----------|----------|
| `F1` | Customer Management |
| `F2` | Job Card Management |
| `F3` | Quotation Management |
| `F4` | Employee Management |
| `F5` | Attendance Management |
| `F6` | Payroll Management |
| `F7` | Paysheet Management |
| `F8` | Order Management |
| `F9` | Outsourcing Management |
| `F10` | Settings |
| `Ctrl+S` | Save (context-dependent) |
| `Ctrl+N` | New (context-dependent) |
| `Escape` | Close current dialog |
| `Enter` | Confirm action |

### File Locations
| File | Purpose | Location |
|------|---------|----------|
| `users.dat` | Encrypted user accounts | `Savers/` |
| `employee.json` | Employee database | `Savers/` |
| `attendance.json` | Attendance records | `Savers/` |
| `customers.json` | Customer database | `Savers/` |
| `jobcards.json` | Job card database | `Savers/` |
| `orders.json` | Order tracking | `Savers/` |
| `loan.json` | Loan records | `Savers/` |
| `EPF.json` | EPF configurations | `Savers/` |
| `Paysheets.json` | Generated paysheets | `Savers/` |
| `backup_history.txt` | Backup log | Root directory |

### Default Settings
| Setting | Default Value |
|---------|---------------|
| Admin Username | `admin` |
| Admin Password | `admin` |
| Backup Interval | 60 minutes |
| Timer Interval | 5 minutes |
| Late Allow Minutes | 15 minutes |
| OT Round Minutes | 15 minutes |
| Working Hours | 08:00 - 17:00 |
| Saturday Hours | 08:00 - 12:00 |

---

**© 2025 PDC System - Comprehensive Printing & Document Center Management Solution**

*This user manual covers version 2025 of PDC System. For the latest updates and features, please refer to the GitHub repository.*
