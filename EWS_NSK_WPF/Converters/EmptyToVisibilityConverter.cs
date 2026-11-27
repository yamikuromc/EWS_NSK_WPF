using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using EWS_NSK_WPF.Views;

namespace EWS_NSK_WPF.Converters
{
    /// <summary>
    /// Конвертер для скрытия подсказки поиска, когда в поле есть текст.
    /// </summary>
    public class EmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}