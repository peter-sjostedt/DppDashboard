using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HospitexDPP.Controls
{
    public partial class ApiKeyDisplay : UserControl
    {
        public static readonly DependencyProperty ApiKeyProperty =
            DependencyProperty.Register(nameof(ApiKey), typeof(string), typeof(ApiKeyDisplay),
                new PropertyMetadata(string.Empty, OnApiKeyChanged));

        public static readonly DependencyProperty RegenerateCommandProperty =
            DependencyProperty.Register(nameof(RegenerateCommand), typeof(ICommand), typeof(ApiKeyDisplay));

        private bool _isKeyVisible;

        public ApiKeyDisplay()
        {
            InitializeComponent();
            UpdateDisplay();
        }

        public string ApiKey
        {
            get => (string)GetValue(ApiKeyProperty);
            set => SetValue(ApiKeyProperty, value);
        }

        public ICommand RegenerateCommand
        {
            get => (ICommand)GetValue(RegenerateCommandProperty);
            set => SetValue(RegenerateCommandProperty, value);
        }

        private static void OnApiKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ApiKeyDisplay display) display.UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                KeyText.Text = "(ingen nyckel)";
                ToggleBtn.Content = FindResource("Button_ShowKey") as string ?? "Visa";
                return;
            }

            KeyText.Text = _isKeyVisible ? ApiKey : new string('\u2022', 20);
            ToggleBtn.Content = _isKeyVisible
                ? (FindResource("Button_HideKey") as string ?? "Dölj")
                : (FindResource("Button_ShowKey") as string ?? "Visa");
        }

        private void ToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isKeyVisible = !_isKeyVisible;
            UpdateDisplay();
        }

        private async void CopyKey_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ApiKey)) return;
            Clipboard.SetText(ApiKey);
            CopyStatus.Text = FindResource("Label_Copied") as string ?? "Kopierad!";
            await Task.Delay(2000);
            CopyStatus.Text = string.Empty;
        }

        private void RegenerateKey_Click(object sender, RoutedEventArgs e)
        {
            if (RegenerateCommand?.CanExecute(null) == true)
                RegenerateCommand.Execute(null);
        }
    }
}
