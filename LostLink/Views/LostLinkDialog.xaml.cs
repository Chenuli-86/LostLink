
// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: LostLinkDialog.xaml.cs
//
// Purpose:
// Creates a customisable popup dialog for LostLink for reuse.
// Delineates success, error, warning, information and
// confirmation messages in a consistent manner to the application.
// ============================================================

using System.Windows;
using System.Windows.Media;

namespace LostLink.Views
{
    // Identifies the type of notification displayed to the user.
    public enum LostLinkDialogType
    {
        Success,
        Error,
        Warning,
        Information,
        Confirmation
    }

    public partial class LostLinkDialog : Window
    {
        // Saves the user's confirmation of the requested action.
        public bool IsConfirmed { get; private set; }

        public LostLinkDialog(
            string title,
            string message,
            LostLinkDialogType dialogType)
        {
            InitializeComponent();

            DialogTitleTextBlock.Text = title;
            DialogMessageTextBlock.Text = message;

            ConfigureDialog(dialogType);
        }

        // Defines the icon, colors and buttons for the various types of dialogs.
        private void ConfigureDialog(LostLinkDialogType dialogType)
        {
            switch (dialogType)
            {
                case LostLinkDialogType.Success:
                    DialogIcon.Text = "✓";
                    SetIconColours("#D7F2EF", "#087C78");
                    ConfirmButton.Content = "OK";
                    break;

                case LostLinkDialogType.Error:
                    DialogIcon.Text = "!";
                    SetIconColours("#FCE8E8", "#B42318");
                    ConfirmButton.Content = "OK";
                    break;

                case LostLinkDialogType.Warning:
                    DialogIcon.Text = "!";
                    SetIconColours("#FFF0E6", "#B45A20");
                    ConfirmButton.Content = "OK";
                    break;

                case LostLinkDialogType.Information:
                    DialogIcon.Text = "i";
                    SetIconColours("#E4F3FA", "#286D91");
                    ConfirmButton.Content = "OK";
                    break;

                case LostLinkDialogType.Confirmation:
                    DialogIcon.Text = "?";
                    SetIconColours("#FFF0E6", "#B45A20");

                    // Confirmation dialogs provide user canceling.
                    CancelButton.Visibility = Visibility.Visible;
                    ConfirmButton.Content = "Confirm";
                    break;
            }
        }

        // Sets the WPF brush with a colour value from hexadecimal.
        private void SetIconColours(
            string backgroundColour,
            string foregroundColour)
        {
            IconBackground.Background =
                (Brush)new BrushConverter().ConvertFromString(
                    backgroundColour)!;

            DialogIcon.Foreground =
                (Brush)new BrushConverter().ConvertFromString(
                    foregroundColour)!;
        }

        // Records and closes the dialog.
        private void ConfirmButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            IsConfirmed = true;
            DialogResult = true;
        }

        // Aborts an action, and does not ask for confirmation.
        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
        }

        // X does not confirm any action if it is used to close an action.
        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
        }


        // Shows a styled notification that has an OK button.
        // The owner is optional because startup errors can occur before the application creates its login window.
        public static void ShowMessage(
            Window? owner,
            string title,
            string message,
            LostLinkDialogType dialogType)
        {
            LostLinkDialog dialog = new LostLinkDialog(
                title,
                message,
                dialogType);

            if (owner != null)
            {
                // Keep the popup centred over its parent window.
                dialog.Owner = owner;
            }
            else
            {
                // Centre startup notifications on the screen when no parent window is available.
                dialog.WindowStartupLocation =
                    WindowStartupLocation.CenterScreen;
            }

            dialog.ShowDialog();
        }


        // Only returns true if the user confirms the action.
        public static bool ShowConfirmation(
            Window owner,
            string title,
            string message)
        {
            LostLinkDialog dialog = new LostLinkDialog(
                title,
                message,
                LostLinkDialogType.Confirmation);

            dialog.Owner = owner;
            dialog.ShowDialog();

            return dialog.IsConfirmed;
        }
    }
}
