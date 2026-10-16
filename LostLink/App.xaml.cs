// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: App.xaml.cs
//
// Purpose: Initializes the database, default accounts and application start-up.
// ============================================================

using System;
using System.Windows;
using LostLink.Data;
using LostLink.Services;
using LostLink.Views;

namespace LostLink
{
    public partial class App : Application
    {
        protected override async void OnStartup(
            StartupEventArgs e)
        {
            base.OnStartup(e);

            // Prevent shutdown while the database
            // is being initialized.
            ShutdownMode =
                ShutdownMode.OnExplicitShutdown;

            try
            {
                // Initialize existing SQLite database.
                var databaseService =
                    new DatabaseService();

                await databaseService.InitializeDatabaseAsync();

                // Ensure the initial Administrator and Staff accounts exist.
                // Existing accounts and their passwords are preserved.
                var authenticationService =
                    new AuthenticationService();

                await authenticationService.CreateInitialAdminAsync();

                // Open normal LostLink login window.
                var loginWindow =
                    new LoginWindow();

                Application.Current.MainWindow =
                    loginWindow;

                loginWindow.Show();

                // Closing the active main window
                // will now close the application.
                ShutdownMode =
                    ShutdownMode.OnMainWindowClose;
            }

            catch (Exception ex)
            {
                // Display a custom error notification even if
                // the login window has not been created yet.
                LostLinkDialog.ShowMessage(
                    null,
                    "Startup Error",
                    "LostLink could not start.\n\n" +
                    ex.Message,
                    LostLinkDialogType.Error);

                // Close the application after the error is acknowledged.
                Shutdown();
            }
        }
    }
}
