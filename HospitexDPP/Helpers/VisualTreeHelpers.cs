using System.Windows;
using System.Windows.Media;

namespace HospitexDPP.Helpers
{
    public static class VisualTreeHelpers
    {
        public static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && parent is not T)
                parent = VisualTreeHelper.GetParent(parent);
            return parent as T;
        }
    }
}
