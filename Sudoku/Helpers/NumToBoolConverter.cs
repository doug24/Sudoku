using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Sudoku;

public class NumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string parameterString)
            return DependencyProperty.UnsetValue;

        if (!int.TryParse(parameterString, out int paramValue))
            return DependencyProperty.UnsetValue;

        return paramValue.Equals(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool flag && flag &&
            parameter is string parameterString &&
            int.TryParse(parameterString, out int paramValue))
        {
            return paramValue;
        }

        return int.MinValue;
    }
}
