using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using MarkdownEditor.Services;

namespace MarkdownEditor.Services
{
    public sealed class HeadingLevelToMarginConverter : IValueConverter
    {
        public static readonly HeadingLevelToMarginConverter Instance = new HeadingLevelToMarginConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int level = value is int i ? i : 1;
            return new Thickness((level - 1) * 14, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
