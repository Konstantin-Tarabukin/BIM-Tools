using System;
using System.Globalization;
using System.Windows.Data;

namespace RevitCopyParams
{
    public class ChangeStatusToTextConverter : IValueConverter
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
                    return "👤 Моё изменение";

                case ChangeStatus.New:
                    return "🔵 Новое";

                case ChangeStatus.Read:
                    return "⚪ Прочитано";

                default:
                    return "";
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