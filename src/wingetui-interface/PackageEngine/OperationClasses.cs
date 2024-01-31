using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace ModernWindow.PackageEngine
{

    public enum OperationStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }
    public class InstallPackageOperation
    {

        private Button ActionButton;
        private Button OutputViewewBlock;
        private ProgressBar Progress;
        private Image PackageIcon;
        private TextBlock OperationDescription;

        private Color DefaultProgressbarColor = Colors.AntiqueWhite;

        private string __button_text;
        protected string ButtonText
        {   get { return __button_text; }
            set { __button_text = value; if(ActionButton != null) ActionButton.Content = __button_text; } }


        private string __line_info_text = "Please wait...";
        protected string LineInfoText
        {
            get { return __line_info_text; }
            set { __line_info_text = value; if (OutputViewewBlock != null) OutputViewewBlock.Content = __line_info_text; }
        }

        private Uri __icon_source = new Uri("ms-appx://wingetui/resources/package_color.png");
        protected Uri IconSource
        {
            get { return __icon_source; }
            set { __icon_source = value; if (PackageIcon != null) PackageIcon.Source = new BitmapImage(__icon_source); }
        }

        private string __operation_description = "$Package Install";
        protected string OperationTitle
        {
            get { return __operation_description; }
            set { __operation_description = value; if (OperationDescription != null) OperationDescription.Text = __operation_description; }
        }

        private Color? __progressbar_color = null;
        protected Color? ProgressBarColor
        {
            get { return __progressbar_color; }
            set { __progressbar_color = value; if (Progress != null) Progress.Foreground = (__progressbar_color != null)? new SolidColorBrush((Color)__progressbar_color): null; }
        }

        private OperationStatus __status = OperationStatus.Pending;
        public OperationStatus Status
        {
            get { return __status; }
            set { 
                __status = value;
                switch (__status)
                {
                    case OperationStatus.Pending:
                        ProgressBarColor = Colors.Gray;
                        ButtonText = "Cancel";
                        break;
                    case OperationStatus.Running:
                        ProgressBarColor = DefaultProgressbarColor;
                        ButtonText = "Cancel";
                        break;
                    case OperationStatus.Completed:
                        ProgressBarColor = Colors.Green;
                        ButtonText = "Close";
                        break;
                    case OperationStatus.Failed:
                        ProgressBarColor = Colors.Red;
                        ButtonText = "Close";
                        break;
                    case OperationStatus.Cancelled:
                        ProgressBarColor = Colors.Yellow;
                        ButtonText = "Close";
                        break;
                }
            }
        }

        public void ActionButtonClicked(object sender, EventArgs args)
        { }

        public InstallPackageOperation()
        { 
            this.Status = OperationStatus.Pending;
        }


        public void ImageIcon_Loaded(object sender, RoutedEventArgs e)
        {
            PackageIcon = sender as Image;
            PackageIcon.Source = new BitmapImage(__icon_source);
        }

        public void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            OperationDescription = sender as TextBlock;
            OperationDescription.Text = __operation_description;
        }

        public void ProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            Progress = sender as ProgressBar;
            Progress.Foreground = (__progressbar_color != null) ? new SolidColorBrush((Color)__progressbar_color) : null;
        }

        public void ViewLogButton_Loaded(object sender, RoutedEventArgs e)
        {
            OutputViewewBlock = sender as Button;
            OutputViewewBlock.Content = __line_info_text;
        }

        public void ActionButton_Loaded(object sender, RoutedEventArgs e)
        {
            ActionButton = sender as Button;
            ActionButton.Content = __button_text;
        }
    }
}
