using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WPF_Explorer_Tree
{
    #region HeaderToImageConverter
    /// <summary>
    /// Required component for the file browser in the GUI
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public class HeaderToImageConverter : IValueConverter
    {
        private static HeaderToImageConverter Instance = new HeaderToImageConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value as string).Contains(@"\"))
            {
                Uri uri = new Uri("pack://application:,,,/Images/diskdrive.png");
                BitmapImage source = new BitmapImage(uri);
                return source;
            }
            else
            {
                Uri uri = new Uri("pack://application:,,,/Images/treefolder.png");
                BitmapImage source = new BitmapImage(uri);
                return source;
            }
			
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
           //Cannot convert back
            throw new NotSupportedException("Cannot convert back");
        }
    }

    #endregion // DoubleToIntegerConverter
}