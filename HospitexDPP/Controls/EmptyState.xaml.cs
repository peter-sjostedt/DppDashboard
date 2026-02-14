using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HospitexDPP.Controls
{
    public partial class EmptyState : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState),
                new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(EmptyState),
                new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(EmptyState),
                new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(EmptyState),
                new PropertyMetadata(null, OnTextChanged));

        public EmptyState()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public ICommand ActionCommand
        {
            get => (ICommand)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmptyState es) es.UpdateContent();
        }

        private void UpdateContent()
        {
            TitleText.Text = Title;
            DescText.Text = Description;
            ActionBtn.Content = ActionText;
            ActionBtn.Command = ActionCommand;
            ActionBtn.Visibility = ActionCommand != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
