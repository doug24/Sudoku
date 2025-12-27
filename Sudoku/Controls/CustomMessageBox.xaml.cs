using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sudoku
{
    /// <summary>
    /// A message box with additional button options and customizations
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        private readonly MessageBoxViewModel vm = new MessageBoxViewModel();

        private CustomMessageBox()
        {
            InitializeComponent();

            DataContext = vm;

            cancelButton.Visibility = Visibility.Collapsed;
            okButton.Visibility = Visibility.Collapsed;
            noButton.Visibility = Visibility.Collapsed;
            yesButton.Visibility = Visibility.Collapsed;

            SourceInitialized += (s, e) => Native.RemoveIcon(this);
        }

        public MessageBoxResult MessageBoxResult { get; private set; } = MessageBoxResult.None;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                switch (btn.Name)
                {
                    case nameof(okButton):
                        MessageBoxResult = MessageBoxResult.OK;
                        break;
                    case nameof(cancelButton):
                        MessageBoxResult = MessageBoxResult.Cancel;
                        break;
                    case nameof(noButton):
                        MessageBoxResult = MessageBoxResult.No;
                        break;
                    case nameof(yesButton):
                        MessageBoxResult = MessageBoxResult.Yes;
                        break;
                }
            }
            Close();
        }

        private void SetButtonVisibility(MessageBoxButton button, MessageBoxResult defaultResult)
        {
            switch (button)
            {
                case MessageBoxButton.OK:
                    okButton.Visibility = Visibility.Visible;
                    okButton.IsDefault = true;
                    break;
                case MessageBoxButton.OKCancel:
                    cancelButton.Visibility = Visibility.Visible;
                    okButton.Visibility = Visibility.Visible;
                    switch (defaultResult)
                    {
                        case MessageBoxResult.OK:
                        default:
                            okButton.IsDefault = true;
                            break;
                        case MessageBoxResult.Cancel:
                            cancelButton.IsDefault = true;
                            break;
                    }
                    break;
                case MessageBoxButton.YesNo:
                    noButton.Visibility = Visibility.Visible;
                    yesButton.Visibility = Visibility.Visible;
                    switch (defaultResult)
                    {
                        case MessageBoxResult.Yes:
                        default:
                            yesButton.IsDefault = true;
                            break;
                        case MessageBoxResult.No:
                            noButton.IsDefault = true;
                            break;
                    }
                    break;
                case MessageBoxButton.YesNoCancel:
                    cancelButton.Visibility = Visibility.Visible;
                    noButton.Visibility = Visibility.Visible;
                    yesButton.Visibility = Visibility.Visible;
                    switch (defaultResult)
                    {
                        case MessageBoxResult.Yes:
                        default:
                            yesButton.IsDefault = true;
                            break;
                        case MessageBoxResult.No:
                            noButton.IsDefault = true;
                            break;
                        case MessageBoxResult.Cancel:
                            cancelButton.IsDefault = true;
                            break;
                    }
                    break;
            }
        }

        private void SetButtonText(MessageBoxCustoms customs)
        {
            if (!string.IsNullOrEmpty(customs.OKButtonText))
            {
                okButton.Content = customs.OKButtonText;
            }
            if (!string.IsNullOrEmpty(customs.CancelButtonText))
            {
                cancelButton.Content = customs.CancelButtonText;
            }
            if (!string.IsNullOrEmpty(customs.NoButtonText))
            {
                noButton.Content = customs.NoButtonText;
            }
            if (!string.IsNullOrEmpty(customs.YesButtonText))
            {
                yesButton.Content = customs.YesButtonText;
            }
        }

        public static MessageBoxResult Show(string messageBoxText)
        {
            return Show(messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }
        public static MessageBoxResult Show(Window owner, string messageBoxText)
        {
            return Show(owner, messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption)
        {
            return Show(owner, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButton button)
        {
            return Show(messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButton button)
        {
            return Show(owner, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon)
        {
            return Show(messageBoxText, caption, button, icon, MessageBoxResult.None, MessageBoxOptions.None);
        }
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon)
        {
            return Show(owner, messageBoxText, caption, button, icon, MessageBoxResult.None, MessageBoxOptions.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return Show(null, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);
        }
        public static MessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return Show(owner, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            return Show(null, messageBoxText, caption, button, icon, defaultResult, options);
        }

        public static MessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            return Show(owner, messageBoxText, caption, button, icon, defaultResult, MessageBoxCustoms.None, options);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult,
            MessageBoxCustoms customs, MessageBoxOptions options)
        {
            return Show(null, messageBoxText, caption, button, icon, defaultResult, customs, options);
        }

        public static MessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult,
            MessageBoxCustoms customs, MessageBoxOptions options)
        {
            var dlg = new CustomMessageBox();

            if (options.HasFlag(MessageBoxOptions.RtlReading))
            {
                dlg.FlowDirection = FlowDirection.RightToLeft;
            }
            if (options.HasFlag(MessageBoxOptions.RightAlign))
            {
                dlg.mbText.HorizontalAlignment = HorizontalAlignment.Right;
            }

            dlg.mbText.Text = messageBoxText;
            dlg.Title = caption;
            dlg.SetButtonVisibility(button, defaultResult);
            dlg.SetButtonText(customs);

            if (customs.Icon != null)
            {
                dlg.mbIcon.Source = customs.Icon;
            }
            else if (icon == MessageBoxImage.None)
            {
                dlg.mbIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                dlg.mbIcon.Source = FromWindowForms(icon);
            }


            if (owner != null)
            {
                dlg.Owner = owner;
            }
            else
            {
                var activeWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
                if (activeWindow != null)
                {
                    dlg.Owner = activeWindow;
                }
            }

            if (dlg.Owner != null)
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            dlg.ShowDialog();

            return dlg.MessageBoxResult;
        }

        private static ImageSource? FromWindowForms(MessageBoxImage icon)
        {
            System.Drawing.Bitmap? bitmap = null;
            switch (icon)
            {
                case MessageBoxImage.Error:
                    bitmap = System.Drawing.SystemIcons.Error.ToBitmap();
                    break;
                case MessageBoxImage.Warning:
                    bitmap = System.Drawing.SystemIcons.Warning.ToBitmap();
                    break;
                case MessageBoxImage.Question:
                    bitmap = System.Drawing.SystemIcons.Question.ToBitmap();
                    break;
                case MessageBoxImage.Information:
                    bitmap = System.Drawing.SystemIcons.Information.ToBitmap();
                    break;
            }

            if (bitmap != null)
            {
                return Native.ToImageSource(bitmap);
            }

            return null;
        }
    }

    public partial class MessageBoxViewModel : ObservableObject
    {
        public MessageBoxViewModel()
        {
            bool isDarkMode = Properties.Settings.Default.DarkMode;

            WindowBackground = isDarkMode ?
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF141414")) :
                Brushes.White;
            WindowForeground = isDarkMode ? Brushes.White : Brushes.Black;

            ButtonBackground = isDarkMode ?
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFBABABA")) :
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFDFDFD"));

            ButtonBorder = isDarkMode ?
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4C4C9B")) :
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D0D0"));

            ButtonPanelBackground = isDarkMode ?
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF262626")) :
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0F0F0"));

        }

        [ObservableProperty]
        private Brush windowBackground = Brushes.White;

        [ObservableProperty]
        private Brush windowForeground = Brushes.Black;

        [ObservableProperty]
        private Brush buttonBackground = Brushes.White;

        [ObservableProperty]
        private Brush buttonBorder = Brushes.White;

        [ObservableProperty]
        private Brush buttonForeground = Brushes.Black;

        [ObservableProperty]
        private Brush buttonPanelBackground = Brushes.White;
    }

    public class MessageBoxCustoms
    {
        public static readonly MessageBoxCustoms None = new();

        /// <summary>
        /// Gets or sets a custom icon for the message box (see class for example)
        /// </summary>
        /// <example>
        /// var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
        /// bitmapImage.BeginInit();
        /// bitmapImage.UriSource = new Uri("pack://application:,,,/dnGrep;component/images/dnGrep48.png");
        /// bitmapImage.EndInit();
        /// var customs = new MessageBoxCustoms() { Icon = bitmapImage };
        /// </example>
        public ImageSource? Icon { get; set; }
        public string OKButtonText { get; set; } = string.Empty;
        public string CancelButtonText { get; set; } = string.Empty;
        public string NoButtonText { get; set; } = string.Empty;
        public string YesButtonText { get; set; } = string.Empty;
    }


}
