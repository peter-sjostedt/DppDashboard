using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace HospitexDPP.Controls
{
    public partial class DrawerPanel : UserControl
    {
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(DrawerPanel),
                new PropertyMetadata(false, OnIsOpenChanged));

        public static readonly DependencyProperty DrawerContentProperty =
            DependencyProperty.Register(nameof(DrawerContent), typeof(object), typeof(DrawerPanel));

        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(DrawerPanel));

        public static readonly DependencyProperty DrawerWidthProperty =
            DependencyProperty.Register(nameof(DrawerWidth), typeof(double), typeof(DrawerPanel),
                new PropertyMetadata(460.0, OnDrawerWidthChanged));

        public DrawerPanel()
        {
            InitializeComponent();
            DrawerTranslate.X = DrawerWidth;
        }

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public object DrawerContent
        {
            get => GetValue(DrawerContentProperty);
            set => SetValue(DrawerContentProperty, value);
        }

        public ICommand CloseCommand
        {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        public double DrawerWidth
        {
            get => (double)GetValue(DrawerWidthProperty);
            set => SetValue(DrawerWidthProperty, value);
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DrawerPanel panel)
                panel.AnimateDrawer((bool)e.NewValue);
        }

        private static void OnDrawerWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DrawerPanel panel && !panel.IsOpen)
                panel.DrawerTranslate.X = (double)e.NewValue;
        }

        private void AnimateDrawer(bool open)
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(200));
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            if (open)
            {
                Overlay.Visibility = Visibility.Visible;
                DrawerBorder.Visibility = Visibility.Visible;
            }

            var anim = new DoubleAnimation
            {
                To = open ? 0 : DrawerWidth,
                Duration = duration,
                EasingFunction = ease
            };

            if (!open)
            {
                anim.Completed += (_, _) =>
                {
                    Overlay.Visibility = Visibility.Collapsed;
                    DrawerBorder.Visibility = Visibility.Collapsed;
                };
            }

            DrawerTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim);
        }

        private void Overlay_Click(object sender, MouseButtonEventArgs e)
        {
            if (CloseCommand?.CanExecute(null) == true)
                CloseCommand.Execute(null);
        }
    }
}
