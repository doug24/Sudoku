using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Sudoku
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Brushes.Red : Brushes.DimGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
