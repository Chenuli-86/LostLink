// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: ReportLostItemWindow.xaml.cs
//
// Purpose:
// Administers the lost item reporting interface in LostLink.
// Validates information required for items, lets users upload,
// preview and remove optional item photos, and saves
// lost-item reports using the ItemService.
// Catches errors and gives feedback to the user when they are submitted.
// ============================================================

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using LostLink.Models;
using LostLink.Services;

namespace LostLink.Views
{
    public partial class ReportLostItemWindow : Window
    {
        private readonly User _currentUser;
        private readonly ItemService _itemService;

        private string? _selectedPhotoPath;

        private const long MaximumPhotoSize =
            5 * 1024 * 1024;

        public ReportLostItemWindow(User currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;
            _itemService = new ItemService();

            DateLostPicker.SelectedDate = DateTime.Today;

            ItemNameTextBox.Focus();
        }

        /// <summary>
        /// Opens a file picker and previews the selected photo.
        /// </summary>
        private void UploadPhotoButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideError();

            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select an Item Photo",
                Filter =
                    "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                FileInfo fileInfo =
                    new FileInfo(dialog.FileName);

                if (fileInfo.Length > MaximumPhotoSize)
                {
                    ShowError(
                        "Please select an image no larger than 5 MB.");
                    return;
                }

                string extension =
                    fileInfo.Extension.ToLowerInvariant();

                if (extension != ".jpg" &&
                    extension != ".jpeg" &&
                    extension != ".png")
                {
                    ShowError(
                        "Only JPG, JPEG and PNG images are supported.");
                    return;
                }

                BitmapImage image = new BitmapImage();

                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(
                    dialog.FileName,
                    UriKind.Absolute);
                image.EndInit();
                image.Freeze();

                _selectedPhotoPath = dialog.FileName;

                ItemPhotoPreview.Source = image;
                ItemPhotoPreview.Visibility =
                    Visibility.Visible;

                PhotoPlaceholderPanel.Visibility =
                    Visibility.Collapsed;

                SelectedPhotoTextBlock.Text =
                    fileInfo.Name;

                RemovePhotoButton.IsEnabled = true;
            }
            catch (Exception)
            {
                ShowError(
                    "The selected file could not be opened as an image.");
            }
        }

        /// <summary>
        /// Removes the currently selected photo.
        /// </summary>
        private void RemovePhotoButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _selectedPhotoPath = null;

            ItemPhotoPreview.Source = null;
            ItemPhotoPreview.Visibility =
                Visibility.Collapsed;

            PhotoPlaceholderPanel.Visibility =
                Visibility.Visible;

            SelectedPhotoTextBlock.Text =
                "JPG, JPEG or PNG - Maximum 5 MB";

            RemovePhotoButton.IsEnabled = false;

            HideError();
        }

        /// <summary>
        /// Validates and saves the lost-item report.
        /// </summary>
        private async void SaveLostItemButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideError();

            string itemName =
                ItemNameTextBox.Text.Trim();

            string colour =
                ColourTextBox.Text.Trim();

            string location =
                LocationTextBox.Text.Trim();

            string description =
                DescriptionTextBox.Text.Trim();

            string? category =
                (CategoryComboBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString();

            if (string.IsNullOrWhiteSpace(itemName) ||
                string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(colour) ||
                string.IsNullOrWhiteSpace(location) ||
                DateLostPicker.SelectedDate is null ||
                string.IsNullOrWhiteSpace(description))
            {
                ShowError(
                    "Please complete all required fields marked with *.");
                return;
            }

            string? savedImagePath = null;

            try
            {
                SaveLostItemButton.IsEnabled = false;
                SaveLostItemButton.Content = "Saving...";

                if (!string.IsNullOrWhiteSpace(
                    _selectedPhotoPath))
                {
                    FileInfo sourceFile =
                        new FileInfo(_selectedPhotoPath);

                    // Recheck the file before saving.
                    if (!sourceFile.Exists ||
                        sourceFile.Length > MaximumPhotoSize)
                    {
                        throw new IOException(
                            "The selected photo is missing or too large.");
                    }

                    string extension =
                        sourceFile.Extension.ToLowerInvariant();

                    if (extension != ".jpg" &&
                        extension != ".jpeg" &&
                        extension != ".png")
                    {
                        throw new IOException(
                            "Unsupported image file type.");
                    }

                    string imagesFolder = Path.Combine(
                        AppContext.BaseDirectory,
                        "ItemImages");

                    Directory.CreateDirectory(imagesFolder);

                    string uniqueFileName =
                        $"{Guid.NewGuid():N}{extension}";

                    savedImagePath = Path.Combine(
                        imagesFolder,
                        uniqueFileName);

                    File.Copy(
                        _selectedPhotoPath,
                        savedImagePath);
                }

                int itemId =
                    await _itemService.CreateLostItemAsync(
                        itemName,
                        category,
                        colour,
                        location,
                        DateLostPicker.SelectedDate.Value,
                        description,
                        _currentUser.UserId,
                        savedImagePath);

                // Display a custom LostLink success notification
                // after the lost-item report has been saved.
                LostLinkDialog.ShowMessage(
                    this,
                    "Report Saved",
                    $"Lost report #{itemId} was saved successfully.",
                    LostLinkDialogType.Success);

                // Close the reporting window after acknowledgement.
                Close();
            }
            catch (Exception)
            {
                // Clean up the copied image if saving failed.
                if (savedImagePath != null)
                {
                    try
                    {
                        if (File.Exists(savedImagePath))
                        {
                            File.Delete(savedImagePath);
                        }
                    }
                    catch
                    {
                        // Preserve the original error message.
                    }
                }

                ShowError(
                    "LostLink could not save the report or photo. Please try again.");
            }
            finally
            {
                SaveLostItemButton.IsEnabled = true;
                SaveLostItemButton.Content =
                    "Save Lost Report";
            }
        }

        /// <summary>
        /// Closes the window without saving.
        /// </summary>
        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorBorder.Visibility =
                Visibility.Visible;
        }

        private void HideError()
        {
            ErrorBorder.Visibility =
                Visibility.Collapsed;
        }
    }
}
