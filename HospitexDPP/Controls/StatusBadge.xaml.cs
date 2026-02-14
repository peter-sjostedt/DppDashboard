using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HospitexDPP.Controls
{
    public partial class StatusBadge : UserControl
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(int), typeof(StatusBadge),
                new PropertyMetadata(1, OnStateChanged));

        public StatusBadge()
        {
            InitializeComponent();
            UpdateVisual();
        }

        public int IsActive
        {
            get => (int)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusBadge badge) badge.UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (IsActive == 1)
            {
                BadgeBorder.Background = new SolidColorBrush(Color.FromRgb(0xD1, 0xE7, 0xDD));
                Dot.Fill = new SolidColorBrush(Color.FromRgb(0x19, 0x87, 0x54));
                BadgeText.Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x5B, 0x32));
                BadgeText.Text = TryFindResource("Status_Active") as string ?? "Aktiv";
            }
            else
            {
                BadgeBorder.Background = new SolidColorBrush(Color.FromRgb(0xE9, 0xEC, 0xEF));
                Dot.Fill = new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
                BadgeText.Foreground = new SolidColorBrush(Color.FromRgb(0x49, 0x50, 0x57));
                BadgeText.Text = TryFindResource("Status_Inactive") as string ?? "Inaktiv";
            }
        }
    }
}
