// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: SubmitClaimWindow.xaml.cs
//
// Purpose:
// Responsible for submitting ownership claims for found items
// Gets lost item reports to be linked, validates claimant data and
// ownership evidence, and uploads claims to ClaimService.
// Shows validation, success and error messages in the LostLink dialog interface.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LostLink.Enums;
using LostLink.Models;
using LostLink.Services;

namespace LostLink.Views
{
    public partial class SubmitClaimWindow : Window
    {
        private readonly User _currentUser;
        private readonly Item _foundItem;

        private readonly ClaimService _claimService =
            new ClaimService();

        private readonly ItemService _itemService =
            new ItemService();

        public SubmitClaimWindow(
            User currentUser,
            Item foundItem)
        {
            InitializeComponent();

            _currentUser = currentUser;
            _foundItem = foundItem;

            ItemTitleTextBlock.Text =
                $"#{foundItem.ItemId} - {foundItem.Title}";

            ItemDetailsTextBlock.Text =
                $"{foundItem.Category}  |  " +
                $"{foundItem.Colour}  |  " +
                $"{foundItem.Location}";

            Loaded += SubmitClaimWindow_Loaded;
        }

        private async void SubmitClaimWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                List<Item> lostItems =
                    await _itemService.GetLostItemsAsync();

                var options = new List<LostReportOption>
                {
                    new LostReportOption
                    {
                        ItemId = null,
                        DisplayText = "No linked lost report"
                    }
                };

                options.AddRange(
                    lostItems
                        .Where(item =>
                            item.Status == ItemStatus.Open)
                        .Select(item => new LostReportOption
                        {
                            ItemId = item.ItemId,
                            DisplayText =
                                $"#{item.ItemId} - {item.Title}"
                        }));

                LostReportComboBox.ItemsSource = options;
                LostReportComboBox.SelectedIndex = 0;
            }

            catch (Exception ex)
            {
                // Notify the user if available lost reports cannot be loaded.
                LostLinkDialog.ShowMessage(
                    this,
                    "Lost Reports Error",
                    "Unable to load lost reports.\n\n" +
                    ex.Message,
                    LostLinkDialogType.Error);
            }
        }

        private async void SubmitButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string claimantName =
                ClaimantNameTextBox.Text.Trim();

            string verificationNotes =
                VerificationNotesTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(claimantName))
            {
                // Require the claimant's name before submitting a claim.
                LostLinkDialog.ShowMessage(
                    this,
                    "Missing Information",
                    "Please enter the student's name.",
                    LostLinkDialogType.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(verificationNotes))
            {
                // Require ownership verification details for review.
                LostLinkDialog.ShowMessage(
                    this,
                    "Missing Information",
                    "Please enter ownership verification details.",
                    LostLinkDialogType.Warning);

                return;
            }

            int? lostItemId =
                (LostReportComboBox.SelectedItem
                    as LostReportOption)?.ItemId;

            SubmitButton.IsEnabled = false;

            try
            {
                int claimId =
                    await _claimService.SubmitClaimAsync(
                        _currentUser,
                        _foundItem.ItemId,
                        claimantName,
                        verificationNotes,
                        lostItemId);

                // Confirm successful submission and explain the next step.
                LostLinkDialog.ShowMessage(
                    this,
                    "Claim Submitted",
                    $"Claim #{claimId} was submitted.\n\n" +
                    "Status: Pending\n" +
                    "An administrator must review the claim.",
                    LostLinkDialogType.Success);

                // Close the claim window and refresh the main workspace.
                DialogResult = true;

            }
            catch (Exception ex)
            {
                // Display an error if the claim could not be saved.
                LostLinkDialog.ShowMessage(
                    this,
                    "Claim Submission Failed",
                    "Unable to submit claim.\n\n" +
                    ex.Message,
                    LostLinkDialogType.Error);
            }
            finally
            {
                SubmitButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private class LostReportOption
        {
            public int? ItemId { get; set; }

            public string DisplayText { get; set; } =
                string.Empty;
        }
    }
}
