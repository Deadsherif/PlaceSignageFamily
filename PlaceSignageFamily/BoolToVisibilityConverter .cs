using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PlaceSignageFamily
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
                // Use the line below if you want to use Visibility.Hidden instead of Visibility.Collapsed
                // return boolValue ? Visibility.Visible : Visibility.Hidden;
            }
            return Visibility.Collapsed; // or Visibility.Hidden, depending on your preference
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
