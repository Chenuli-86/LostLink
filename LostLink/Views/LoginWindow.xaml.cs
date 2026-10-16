// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: LoginWindow.xaml.cs
//
// Purpose: Handles user login, input validation and authentication feedback.
// ============================================================

using System;
using System.Windows;
using LostLink.Models;
using LostLink.Services;

namespace LostLink.Views
{
    public partial class LoginWindow : Window
    {
        // Handles authentication against the LostLink database.
        private readonly AuthenticationService _authenticationService;

        // Tracks whether the password is visible.
        private bool _isPasswordVisible;

        public LoginWindow()
        {
            InitializeComponent();

            _authenticationService =
                new AuthenticationService();

            // Focus on username when the login page opens.
            UsernameTextBox.Focus();
        }

        // LOGIN BUTTON
        private async void LoginButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideError();

            string username =
                UsernameTextBox.Text.Trim();

            string password =
                _isPasswordVisible
                    ? VisiblePasswordTextBox.Text
                    : PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ShowError(
                    "Please enter both username and password.");

                return;
            }

            try
            {
                LoginButton.IsEnabled = false;

                LoginButton.Content =
                    "Signing in...";

                User? user =
                    await _authenticationService.LoginAsync(
                        username,
                        password);

                // Invalid credentials.
                if (user is null)
                {
                    ShowError(
                        "Invalid username or password.");

                    return;
                }

                // Authentication successful.
                // Open the dashboard with the authenticated user.
                MainWindow mainWindow =
                    new MainWindow(user);

                // IMPORTANT:
                // Set Dashboard as the application's main window
                // BEFORE closing the Login window.
                //
                // This prevents WPF from shutting down
                // when the Login window closes.
                Application.Current.MainWindow =
                    mainWindow;

                mainWindow.Show();

                // Close only the Login window.
                Close();
            }
            catch (Exception)
            {
                ShowError(
                    "LostLink could not complete the sign-in. Please try again.");
            }
            finally
            {
                LoginButton.IsEnabled = true;

                LoginButton.Content =
                    "Sign In   →";
            }
        }

        // SHOW / HIDE PASSWORD
        private void TogglePasswordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                PasswordBox.Password =
                    VisiblePasswordTextBox.Text;

                VisiblePasswordTextBox.Visibility =
                    Visibility.Collapsed;

                PasswordBox.Visibility =
                    Visibility.Visible;

                TogglePasswordButton.Content =
                    "◉";

                PasswordBox.Focus();
            }
            else
            {
                VisiblePasswordTextBox.Text =
                    PasswordBox.Password;

                PasswordBox.Visibility =
                    Visibility.Collapsed;

                VisiblePasswordTextBox.Visibility =
                    Visibility.Visible;

                TogglePasswordButton.Content =
                    "○";

                VisiblePasswordTextBox.Focus();

                VisiblePasswordTextBox.CaretIndex =
                    VisiblePasswordTextBox.Text.Length;
            }

            _isPasswordVisible =
                !_isPasswordVisible;
        }

        // DISPLAY LOGIN ERROR
        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;

            ErrorBorder.Visibility =
                Visibility.Visible;
        }

        // HIDE LOGIN ERROR
        private void HideError()
        {
            ErrorBorder.Visibility =
                Visibility.Collapsed;
        }
    }
}
