using System.Windows;
using System.Windows.Controls;

namespace CalendarMaker.Views
{
    public sealed class CalendarTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement fe)
            {
                int index = (int)(fe.GetValue(ItemsControl.AlternationIndexProperty) ?? 0);
                string key = index < 7 ? "WeekdayTemplate" : "DayCellTemplate";
                return fe.FindResource(key) as DataTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
