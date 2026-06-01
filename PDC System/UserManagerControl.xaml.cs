using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PDC_System.Models;
using PDC_System.Services;

namespace PDC_System
{
    public partial class UserManagerControl : UserControl
    {
        #region Fields

        private List<User> users;
        private User editingUser;
        private bool isEditMode = false;

        #endregion

        #region Constructor

        public UserManagerControl()
        {
            InitializeComponent();
            CreateUserPanel.Visibility = Visibility.Collapsed;
            LoadUsers();
            UpdateUserCountDisplay();
        }

        #endregion

        #region Data Loading

        private void LoadUsers()
        {
            users = UserService.Load();
            UsersDataGrid.ItemsSource = users;
            UpdateUserCountDisplay();
        }

        private void UpdateUserCountDisplay()
        {
            if (users != null)
            {
                UserCountText.Text = $"{users.Count} Users";
            }
        }

        #endregion

        #region Button Click Events

        private void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            ShowCreateUserPanel();
            ResetToCreateMode();
            UpdatePanelHeader("➕ Create New User", "Fill in the details to create a new user account");
        }

        private void BtnManageUsers_Click(object sender, RoutedEventArgs e)
        {
            ShowManageUsersPanel();
        }

        private void CreateSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtUser.Text))
            {
                CustomMessageBox.Show("⚠️ Please enter a username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtPass.Password) && !isEditMode)
            {
                CustomMessageBox.Show("⚠️ Please enter a password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if username already exists (when creating or editing different user)
            if (users.Any(u => u.Username == TxtUser.Text && (!isEditMode || u.Username != editingUser?.Username)))
            {
                CustomMessageBox.Show("⚠️ Username already exists. Please choose a different username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (isEditMode)
            {
                // Update existing user
                editingUser.Username = TxtUser.Text;
                if (!string.IsNullOrWhiteSpace(TxtPass.Password))
                {
                    editingUser.PasswordHash = UserService.Hash(TxtPass.Password);
                }
                UpdateUserPermissions(editingUser);

                CustomMessageBox.Show("✅ User updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Create new user
                var newUser = new User
                {
                    Username = TxtUser.Text,
                    PasswordHash = UserService.Hash(TxtPass.Password)
                };
                UpdateUserPermissions(newUser);

                users.Add(newUser);
                CustomMessageBox.Show("✅ User created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            UserService.Save(users);
            LoadUsers();
            ClearForm();
            ShowManageUsersPanel();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            ShowManageUsersPanel();
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is User user)
            {
                // Block editing admin users
                if (user.Isadmin)
                {
                    CustomMessageBox.Show("⚠️ Admin users cannot be edited.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                editingUser = user;
                isEditMode = true;

                ShowCreateUserPanel();
                UpdatePanelHeader("✏️ Edit User", $"Modify details for user: {user.Username}");

                // Populate form with user data
                TxtUser.Text = user.Username;
                TxtPass.Password = ""; // Don't show existing password

                // Set permissions
                Dashbord.IsChecked = user.Dashbord;
                ChkOderCheck.IsChecked = user.OderCheck;
                ChkJobcard.IsChecked = user.Jobcard;
                ChkCustomer.IsChecked = user.Customer;
                ChkOutsourcing.IsChecked = user.Outsourcing;
                ChkQuotation.IsChecked = user.Quotation;
                ChkEmployee.IsChecked = user.Employee;
                ChkAttendance.IsChecked = user.Attendance;
                ChkPayroll.IsChecked = user.Payroll;
                ChkPaysheet.IsChecked = user.Paysheet;
                ChkUserManager.IsChecked = user.UserManager;



                // Update UI for edit mode
                BtnCreateSave.Content = "💾 Update User";
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is User user)
            {
                // Block deleting admin users
                if (user.Isadmin)
                {
                    CustomMessageBox.Show("⚠️ Admin users cannot be deleted.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = CustomMessageBox.Show(
                    $"⚠️ Are you sure you want to delete user '{user.Username}'?\n\nThis action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    users.Remove(user);
                    UserService.Save(users);
                    LoadUsers();
                    CustomMessageBox.Show("✅ User deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Keep the original Create_Click for backward compatibility
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            CreateSave_Click(sender, e);
        }

        #endregion

        #region UI Panel Management

        private void ShowCreateUserPanel()
        {
            CreateUserPanel.Visibility = Visibility.Visible;
            ManageUsersPanel.Opacity = 0.5;
            ManageUsersPanel.IsEnabled = false;
            ClearForm();
        }

        private void ShowManageUsersPanel()
        {
            CreateUserPanel.Visibility = Visibility.Collapsed;
            ManageUsersPanel.Opacity = 1.0;
            ManageUsersPanel.IsEnabled = true;
            LoadUsers();
        }

        private void UpdatePanelHeader(string title, string description)
        {
            // Find the header TextBlocks in the CreateUserPanel
            var headerBorder = CreateUserPanel.Child as ScrollViewer;
            if (headerBorder?.Content is StackPanel stackPanel)
            {
                var border = stackPanel.Children[0] as Border;
                if (border?.Child is StackPanel headerStack)
                {
                    if (headerStack.Children[0] is TextBlock titleBlock)
                        titleBlock.Text = title;
                    if (headerStack.Children[1] is TextBlock descBlock)
                        descBlock.Text = description;
                }
            }
        }

        #endregion

        #region Form Helpers

        private void UpdateUserPermissions(User user)
        {
            user.Dashbord = Dashbord.IsChecked == true;
            user.OderCheck = ChkOderCheck.IsChecked == true;
            user.Jobcard = ChkJobcard.IsChecked == true;
            user.Customer = ChkCustomer.IsChecked == true;
            user.Outsourcing = ChkOutsourcing.IsChecked == true;
            user.Quotation = ChkQuotation.IsChecked == true;
            user.Employee = ChkEmployee.IsChecked == true;
            user.Attendance = ChkAttendance.IsChecked == true;
            user.Payroll = ChkPayroll.IsChecked == true;
            user.Paysheet = ChkPaysheet.IsChecked == true;
            user.UserManager = ChkUserManager.IsChecked == true;

        }

        private void ClearForm()
        {
            TxtUser.Text = "";
            TxtPass.Password = "";

            // Reset checkboxes (keep Dashboard checked and disabled)
            Dashbord.IsChecked = true;
            ChkOderCheck.IsChecked = false;
            ChkJobcard.IsChecked = false;
            ChkCustomer.IsChecked = false;
            ChkOutsourcing.IsChecked = false;
            ChkQuotation.IsChecked = false;
            ChkEmployee.IsChecked = false;
            ChkAttendance.IsChecked = false;
            ChkPayroll.IsChecked = false;
            ChkPaysheet.IsChecked = false;
            ChkUserManager.IsChecked = false;

        }

        private void ResetToCreateMode()
        {
            isEditMode = false;
            editingUser = null;
            BtnCreateSave.Content = "✅ Create User";
        }

        #endregion
    }
}