using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PDC_System
{
    /// <summary>
    /// Converts a boolean value to Visibility, inverting the result (True = Hidden, False = Visible)
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Hidden : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Hidden;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts user action visibility based on admin status and username
    /// </summary>
    public class UserActionVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = Isadmin (bool)
            // values[1] = Username (string)

            if (values.Length >= 2)
            {
                bool isAdmin = values[0] is bool admin && admin;
                string username = values[1] as string;

                // Show actions if NOT admin or username is empty/null
                if (!isAdmin || string.IsNullOrEmpty(username))
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}