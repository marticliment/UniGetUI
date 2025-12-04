using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.Services;
using Windows.ApplicationModel.DataTransfer;

namespace UniGetUI.Pages.Dialogs
{
    public sealed partial class FeedbackDialog : ContentDialog
    {
        private string _generatedBody = string.Empty;
        private readonly FeedbackService _feedbackService;
        private readonly GitHubIssueHelper _githubHelper;

        public FeedbackDialog()
        {
            InitializeComponent();
            _feedbackService = FeedbackService.Instance;
            _githubHelper = new GitHubIssueHelper();
        }

        private async void GeneratePreview_Click(object sender, RoutedEventArgs e)
        {
            await GenerateIssueBodyAsync();
        }

        private async Task GenerateIssueBodyAsync()
        {
            try
            {
                LoadingRing.IsActive = true;
                GeneratePreviewButton.IsEnabled = false;

                var description = DescriptionTextBox.Text;
                if (string.IsNullOrWhiteSpace(description))
                {
                    description = "No description provided.";
                }

                _generatedBody = await _feedbackService.CreateIssueBodyAsync(
                    description,
                    IncludeLogsCheckBox.IsChecked == true,
                    IncludeSystemInfoCheckBox.IsChecked == true
                );

                PreviewTextBlock.Text = _generatedBody;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating preview: {ex.Message}");
                PreviewTextBlock.Text = $"Error: {ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
                GeneratePreviewButton.IsEnabled = true;
            }
        }

        private Task EnsureGeneratedBodyAsync()
        {
            if (!string.IsNullOrEmpty(_generatedBody))
                return Task.CompletedTask;

            return GenerateIssueBodyAsync();
        }

        private async void PrimaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                await ShowErrorAsync("Please enter a title for the issue.");
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                await ShowErrorAsync("Please enter a description for the issue.");
                return;
            }

            try
            {
                IsPrimaryButtonEnabled = false;
                LoadingRing.IsActive = true;

                await EnsureGeneratedBodyAsync();

                var issueType = (IssueTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "bug";
                var titlePrefix = issueType switch
                {
                    "bug" => "[BUG] ",
                    "feature" => "[FEATURE REQUEST] ",
                    "enhancement" => "[ENHANCEMENT] ",
                    _ => ""
                };

                var fullTitle = titlePrefix + TitleTextBox.Text;

                _githubHelper.OpenIssuePage(issueType, fullTitle);

                var copyDialog = new ContentDialog
                {
                    Title = "Issue Data Copied",
                    Content = "The issue template will open in your browser. The full issue body (including system info and logs) has been copied to your clipboard. Please paste it into the appropriate field.",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };

                CopyToClipboard(_generatedBody);
                await copyDialog.ShowAsync();

                Hide();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error submitting feedback: {ex.Message}");
                await ShowErrorAsync($"Failed to open GitHub: {ex.Message}");
            }
            finally
            {
                IsPrimaryButtonEnabled = true;
                LoadingRing.IsActive = false;
            }
        }

        private async void SecondaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            await EnsureGeneratedBodyAsync();
            await CopyAndNotifyAsync();
        }

        private async Task CopyAndNotifyAsync()
        {
            CopyToClipboard(_generatedBody);

            var notifyDialog = new ContentDialog
            {
                Title = "Copied to Clipboard",
                Content = "Issue content has been copied. You can paste it into GitHub manually.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await notifyDialog.ShowAsync();
        }

        private void CopyToClipboard(string text)
        {
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(text);
                Clipboard.SetContent(dataPackage);
                Logger.Info("Issue content copied to clipboard");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to copy to clipboard: {ex.Message}");
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await errorDialog.ShowAsync();
        }
    }
}
