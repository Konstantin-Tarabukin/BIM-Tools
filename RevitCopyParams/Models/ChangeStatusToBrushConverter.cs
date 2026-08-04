using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RevitCopyParams
{
    public class ChangeStatusToBrushConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            ChangeStatus status =
                (ChangeStatus)value;

            switch (status)
            {
                case ChangeStatus.Mine:
                    return new SolidColorBrush(
                        Color.FromRgb(90, 90, 90));

                case ChangeStatus.New:
                    return new SolidColorBrush(
                        Color.FromRgb(58, 134, 209));

                case ChangeStatus.Read:
                    return new SolidColorBrush(
                        Color.FromRgb(160, 160, 160));

                default:
                    return Brushes.Black;
            }
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}