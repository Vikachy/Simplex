using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SimplexSolver
{
    public class BoolToOptimalTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOptimal && isOptimal)
                return "✓ Таблица оптимальна";
            return "🔄 Итерация продолжается";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOptimal && isOptimal)
                return new SolidColorBrush(Color.FromRgb(0, 100, 0)); // Темно-зеленый
            return new SolidColorBrush(Color.FromRgb(0, 0, 0)); // Черный
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}