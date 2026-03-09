using System.Windows;
using System.Windows.Controls;

namespace PDC_System
{
    public partial class AddCustomerWindow : Window
    {
        public Customerinfo Customer { get; private set; }

        private readonly bool _isEditMode;

#pragma warning disable CS8618
        public AddCustomerWindow()
#pragma warning restore CS8618
        {
            InitializeComponent();
        }

        // Edit mode constructor
#pragma warning disable CS8618
        public AddCustomerWindow(Customerinfo existingCustomer) : this()
#pragma warning restore CS8618
        {
            _isEditMode = true;

            // Pre-fill all fields with existing customer data
            NameTextBox.Text = existingCustomer.Name;
            AddressTextBox.Text = existingCustomer.Address;
            ContactNoTextBox.Text = existingCustomer.ContactNo;
            EmailTextBox.Text = existingCustomer.Email;

            if (existingCustomer.Type?.ToLower() == "company")
            {
                CP.IsChecked = true;
                CompanyTextBox.Text = existingCustomer.companyname;
                CompanyLabel.Visibility = Visibility.Visible;
                CompanyTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                PersonRB.IsChecked = true;
            }

            // Update title to reflect edit mode
            this.Title = "Edit Customer";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // ✅ Validate Name field
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                CustomMessageBox.Show("Please enter a Customer Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            // ✅ Validate Contact Number
            if (string.IsNullOrWhiteSpace(ContactNoTextBox.Text))
            {
                CustomMessageBox.Show("Please enter a Contact Number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContactNoTextBox.Focus();
                return;
            }

            // ✅ Validate Type selection (Company or Person)
            if (CP.IsChecked != true && PersonRB.IsChecked != true)
            {
                CustomMessageBox.Show("Please select Customer Type (Company or Person).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ Validate Company Name if Company is selected
            if (CP.IsChecked == true && string.IsNullOrWhiteSpace(CompanyTextBox.Text))
            {
                CustomMessageBox.Show("Please enter a Company Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CompanyTextBox.Focus();
                return;
            }

            Customer = new Customerinfo
            {
                Name = NameTextBox.Text,
                Address = AddressTextBox.Text,
                ContactNo = ContactNoTextBox.Text,
                Email = EmailTextBox.Text,
                Type = CP.IsChecked == true ? "Company" : PersonRB.IsChecked == true ? "Person" : "",
                companyname = CP.IsChecked == true ? CompanyTextBox.Text : null
            };
            DialogResult = true;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (CP.IsChecked == true)
            {
                CompanyLabel.Visibility = Visibility.Visible;
                CompanyTextBox.Visibility = Visibility.Visible;
            }
        }

        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CP.IsChecked != true)
            {
                CompanyLabel.Visibility = Visibility.Collapsed;
                CompanyTextBox.Visibility = Visibility.Collapsed;
                CompanyTextBox.Text = string.Empty;
            }
        }

        #region Close Button

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        private void ContactNoTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void ContactNoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ContactNoTextBox.Text.Length > 10)
            {
                ContactNoTextBox.Text = ContactNoTextBox.Text.Substring(0, 10);
                ContactNoTextBox.SelectionStart = ContactNoTextBox.Text.Length;
            }
        }
    }
}