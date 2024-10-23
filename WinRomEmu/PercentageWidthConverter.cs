using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EmulatorManager
{
    public class PercentageWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double actualWidth && parameter is string percentage)
            {
                if (double.TryParse(percentage.TrimEnd('%'), out double percentValue))
                {
                    return actualWidth * (percentValue / 100);
                }
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}