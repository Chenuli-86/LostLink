// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management

// Author  : Chenuli Alwis
// File    : MainWindow.xaml.cs
// Purpose :
// Handles the main LostLink application window,
// his includes navigation, dashboard updates,
// lost and unclaimed devices, and user information.
// The collection features smart matching, found item
// displays and claim features.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LostLink.Enums;
using LostLink.Models;
using LostLink.Services;
using LostLink.Views;

namespace LostLink
{
    public partial class MainWindow : Window
    {
        public User CurrentUser { get; }

        // Services are used for database operations and business logic, which are separated from UI.
        private readonly ItemService _itemService =
            new ItemService();

        private readonly MatchingService _matchingService =
            new MatchingService();

        private readonly ClaimService _claimService =
            new ClaimService();

        private readonly ActivityLogService _activityLogService =
            new ActivityLogService();

        private List<ActivityLog> _activityLogs = new List<ActivityLog>();

        private static readonly Brush ActiveNavBrush =
            new SolidColorBrush(Color.FromRgb(39, 165, 159));

        // Starts the workspace for the authenticated staff or administrator.
        public MainWindow(User currentUser)
        {
            InitializeComponent();

            CurrentUser = currentUser;

            DisplayCurrentUser();

            Loaded += MainWindow_Loaded;
        }

        // DISPLAY LOGGED-IN USER
        private void DisplayCurrentUser()
        {
            CurrentUserNameTextBlock.Text =
                CurrentUser.FullName;

            CurrentUserRoleTextBlock.Text =
                FormatRole(CurrentUser.Role.ToString());

            WelcomeUserTextBlock.Text =
                $"{CurrentUser.FullName}.";
        }

        // Fill the first reports and claims at the opening of the main window.
        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await RefreshAllItemsAsync();
            await RefreshClaimsAsync();
        }

        // REPORT LOST ITEM
        private async void ReportLostItemButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var reportWindow =
                new ReportLostItemWindow(CurrentUser)
                {
                    Owner = this
                };

            reportWindow.ShowDialog();

            await RefreshAllItemsAsync();
            await RefreshClaimsAsync();
        }

        // REPORT FOUND ITEM
        private async void ReportFoundItemButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var reportWindow =
                new ReportFoundItemWindow(CurrentUser)
                {
                    Owner = this
                };

            reportWindow.ShowDialog();

            await RefreshAllItemsAsync();
            await RefreshClaimsAsync();
        }

        // DASHBOARD NAVIGATION
        private void DashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowPage("Dashboard");
        }

        // LOST ITEMS NAVIGATION
        private async void LostItemsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (await RefreshAllItemsAsync())
            {
                ShowPage("Lost Items");
            }
        }

        // FOUND ITEMS NAVIGATION
        private async void FoundItemsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (await RefreshAllItemsAsync())
            {
                ShowPage("Found Items");
            }
        }

        // SMART MATCHES NAVIGATION
        private async void SmartMatchesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (await RefreshAllItemsAsync())
            {
                ShowPage("Smart Matches");
            }
        }

        // CLAIMS NAVIGATION
        private async void ClaimsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (await RefreshClaimsAsync())
            {
                ShowPage("Claims");
            }
        }

        // ACTIVITY LOG NAVIGATION
        private async void ActivityLogButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (await RefreshActivityLogAsync())
            {
                ShowPage("Activity Log");
            }
        }

        // MANUAL REFRESH
        private async void RefreshActivityLogButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await RefreshActivityLogAsync();
        }

        // SEARCH THE LOADED AUDIT RECORDS
        private void ActivityLogSearchTextBox_TextChanged(
            object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterActivityLogs();
        }

        private async Task<bool> RefreshActivityLogAsync()
        {
            try
            {
                _activityLogs = await _activityLogService
                    .GetAllActivitiesAsync(CurrentUser);

                FilterActivityLogs();
                return true;
            }

            catch (Exception ex)
            {
                // Display a custom LostLink error notification
                // if activity history cannot be retrieved.
                LostLinkDialog.ShowMessage(
                    this,
                    "Activity Log Error",
                    "Unable to load activity history.\n\n" + ex.Message,
                    LostLinkDialogType.Error);

                return false;
            }

        }

        // Filter loaded audit records without making another database request.
        private void FilterActivityLogs()
        {
            // The TextChanged event can fire during initialization.
            if (ActivityLogSearchTextBox == null ||
                ActivityLogItemsControl == null)
            {
                return;
            }

            string search = ActivityLogSearchTextBox.Text.Trim();

            List<ActivityLog> filtered = _activityLogs
                .Where(log =>
                    string.IsNullOrEmpty(search) ||
                    log.Action.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    log.Details.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    log.PerformedBy.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    log.UserRole.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ActivityLogItemsControl.ItemsSource = filtered;
            ActivityLogItemsControl.Visibility = filtered.Count > 0
                ? Visibility.Visible : Visibility.Collapsed;
            ActivityLogEmptyState.Visibility = filtered.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
            ActivityLogEmptyState.Text = _activityLogs.Count == 0
                ? "No activities have been recorded yet."
                : "No activities match your search.";

            ActivityLogCountTextBlock.Text = string.IsNullOrEmpty(search)
                ? $"{_activityLogs.Count} recorded activities"
                : $"{filtered.Count} of {_activityLogs.Count} activities shown";
        }

        // Change the appearance of the content and mark the selected sidebar button.
        private void ShowPage(string pageName)
        {
            DashboardContent.Visibility =
                pageName == "Dashboard"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            LostItemsContent.Visibility =
                pageName == "Lost Items"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            FoundItemsContent.Visibility =
                pageName == "Found Items"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            SmartMatchesContent.Visibility =
                pageName == "Smart Matches"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ClaimsContent.Visibility =
                pageName == "Claims"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ActivityLogContent.Visibility =
                pageName == "Activity Log"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            PageTitleTextBlock.Text = pageName;

            DashboardButton.Background =
                pageName == "Dashboard"
                    ? ActiveNavBrush
                    : Brushes.Transparent;

            LostItemsButton.Background =
                pageName == "Lost Items"
                    ? ActiveNavBrush
                    : Brushes.Transparent;

            FoundItemsButton.Background =
                pageName == "Found Items"
                    ? ActiveNavBrush
                    : Brushes.Transparent;

            SmartMatchesButton.Background =
                pageName == "Smart Matches"
                    ? ActiveNavBrush
                    : Brushes.Transparent;

            ClaimsButton.Background =
                pageName == "Claims"
                    ? ActiveNavBrush
                    : Brushes.Transparent;

            ActivityLogButton.Background =
                pageName == "Activity Log"
                    ? ActiveNavBrush
                    : Brushes.Transparent;
        }

        // Get reports, update counts on dashboard and create possible item matches.
        private async Task<bool> RefreshAllItemsAsync()
        {
            try
            {
                List<Item> lostItems =
                    await _itemService.GetLostItemsAsync();

                List<Item> foundItems =
                    await _itemService.GetFoundItemsAsync();

                // DASHBOARD REPORT COUNTS
                LostReportsCountTextBlock.Text =
                    lostItems.Count.ToString();

                FoundReportsCountTextBlock.Text =
                    foundItems.Count.ToString();

                // LOST ITEMS PAGE
                LostItemsPageCountTextBlock.Text =
                    $"{lostItems.Count} lost report" +
                    (lostItems.Count == 1 ? "" : "s");

                LostItemsItemsControl.ItemsSource =
                    lostItems;

                LostItemsItemsControl.Visibility =
                    lostItems.Count > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                LostItemsEmptyState.Visibility =
                    lostItems.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                // FOUND ITEMS PAGE
                FoundItemsPageCountTextBlock.Text =
                    $"{foundItems.Count} found report" +
                    (foundItems.Count == 1 ? "" : "s");

                FoundItemsItemsControl.ItemsSource =
                    foundItems;

                FoundItemsItemsControl.Visibility =
                    foundItems.Count > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                FoundItemsEmptyState.Visibility =
                    foundItems.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                // RECENT REPORTS
                List<Item> recentItems =
                    lostItems
                        .Concat(foundItems)
                        .OrderByDescending(
                            item => item.CreatedAt)
                        .Take(5)
                        .ToList();

                RecentReportsItemsControl.ItemsSource =
                    recentItems;

                RecentReportsItemsControl.Visibility =
                    recentItems.Count > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                RecentReportsEmptyState.Visibility =
                    recentItems.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                // SMART MATCHING
                List<SmartMatchDisplay> potentialMatches =
                    new List<SmartMatchDisplay>();
                // Only compare open lost reports to open found reports.
                foreach (Item lostItem in lostItems)
                {
                    if (lostItem.Status != ItemStatus.Open)
                    {
                        continue;
                    }

                    foreach (Item foundItem in foundItems)
                    {
                        if (foundItem.Status != ItemStatus.Open)
                        {
                            continue;
                        }

                        MatchResult result =
                            _matchingService.CalculateMatch(
                                lostItem,
                                foundItem);
                        // Remove matches that are not highly confident from suggestions.
                        if (result.Classification ==
                            MatchClassification.Low)
                        {
                            continue;
                        }

                        potentialMatches.Add(
                            new SmartMatchDisplay
                            {
                                LostTitle =
                                    lostItem.Title,

                                FoundTitle =
                                    foundItem.Title,

                                LostItemId =
                                    lostItem.ItemId,

                                FoundItemId =
                                    foundItem.ItemId,

                                LostImagePath =
                                    lostItem.ImagePath,

                                FoundImagePath =
                                    foundItem.ImagePath,

                                MatchScore =
                                    result.MatchScore,

                                Classification =
                                    result.Classification.ToString(),

                                CategoryMatched =
                                    result.CategoryMatched,

                                ColourMatched =
                                    result.ColourMatched,

                                LocationMatched =
                                    result.LocationMatched,

                                DateScore =
                                    result.DateScore,

                                KeywordScore =
                                    result.KeywordScore,

                                MatchedKeywords =
                                    string.IsNullOrWhiteSpace(
                                        result.MatchedKeywords)
                                        ? "No shared keywords"
                                        : result.MatchedKeywords,

                                LostLocation =
                                    lostItem.Location,

                                FoundLocation =
                                    foundItem.Location,

                                LostDate =
                                    lostItem.IncidentDate,

                                FoundDate =
                                    foundItem.IncidentDate
                            });
                    }
                }
                // Show the first potential matches that have the highest scores.
                potentialMatches =
                    potentialMatches
                        .OrderByDescending(
                            match => match.MatchScore)
                        .ThenBy(
                            match => match.LostItemId)
                        .ThenBy(
                            match => match.FoundItemId)
                        .ToList();

                PotentialMatchesCountTextBlock.Text =
                    potentialMatches.Count.ToString();

                SmartMatchesPageCountTextBlock.Text =
                    $"{potentialMatches.Count} potential match" +
                    (potentialMatches.Count == 1 ? "" : "es");

                SmartMatchesItemsControl.ItemsSource =
                    potentialMatches;

                SmartMatchesItemsControl.Visibility =
                    potentialMatches.Count > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                SmartMatchesEmptyState.Visibility =
                    potentialMatches.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                // DASHBOARD SMART MATCH PREVIEW
                List<SmartMatchDisplay> topMatches =
                    potentialMatches.Take(3).ToList();

                DashboardMatchesItemsControl.ItemsSource =
                    topMatches;

                DashboardMatchesItemsControl.Visibility =
                    topMatches.Count > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                DashboardMatchesEmptyState.Visibility =
                    topMatches.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                return true;
            }

            catch (Exception ex)
            {
                // Notify the user if item loading or
                // smart match calculations fail.
                LostLinkDialog.ShowMessage(
                    this,
                    "Item Loading Error",
                    "Unable to load reports or calculate matches.\n\n" +
                    ex.Message,
                    LostLinkDialogType.Error);

                return false;
            }

        }

        // OPEN SUBMIT CLAIM WINDOW
        private async void SubmitClaimButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button ||
                button.Tag is not Item foundItem)
            {
                return;
            }


            if (foundItem.Status != ItemStatus.Open)
            {
                // Prevent claims against items that are no longer open.
                LostLinkDialog.ShowMessage(
                    this,
                    "Claim Unavailable",
                    "Only open found items can be claimed.",
                    LostLinkDialogType.Warning);

                return;
            }

            var claimWindow =
                new SubmitClaimWindow(
                    CurrentUser,
                    foundItem)
                {
                    Owner = this
                };

            if (claimWindow.ShowDialog() == true)
            {
                await RefreshClaimsAsync();
                await RefreshAllItemsAsync();

                ShowPage("Claims");
            }
        }

        // LOAD CLAIMS MANAGEMENT
        private async Task<bool> RefreshClaimsAsync()
        {
            try
            {
                List<Claim> claims =
                    await _claimService.GetAllClaimsAsync(
                        CurrentUser);

                List<Item> foundItems =
                    await _itemService.GetFoundItemsAsync();

                var foundLookup =
                    foundItems.ToDictionary(
                        item => item.ItemId);
                // Only administrators can see claim approval controls.
                bool isAdministrator =
                    CurrentUser.Role ==
                    UserRole.Administrator;

                var displayClaims =
                    claims.Select(claim =>
                    {
                        string foundTitle =
                            foundLookup.TryGetValue(
                                claim.FoundItemId,
                                out Item? foundItem)
                                ? foundItem.Title
                                : "Unknown found item";

                        return new ClaimDisplay
                        {
                            ClaimId =
                                claim.ClaimId,

                            ClaimTitle =
                                $"Claim #{claim.ClaimId}",

                            FoundItemTitle =
                                $"Found item #{claim.FoundItemId} - {foundTitle}",

                            ClaimantName =
                                claim.ClaimantName,

                            VerificationNotes =
                                claim.VerificationNotes,

                            Status =
                                claim.Status,

                            CreatedAt =
                                claim.CreatedAt.ToLocalTime(),

                            SubmittedByName =
                                claim.SubmittedBy.HasValue
                                    ? $"Staff account #{claim.SubmittedBy.Value}"
                                    : "Legacy claim / not recorded",

                            LostReportText =
                                claim.LostItemId.HasValue
                                    ? $"#{claim.LostItemId.Value}"
                                    : "Not linked",

                            ProcessedByText =
                                claim.ProcessedBy.HasValue
                                    ? $"Reviewed by account #{claim.ProcessedBy.Value}"
                                    : "Not yet reviewed",

                            ApprovalButtonsVisibility =
                                isAdministrator &&
                                claim.Status == "Pending"
                                    ? Visibility.Visible
                                    : Visibility.Collapsed,

                            CollectionButtonVisibility =
                                claim.Status == "Approved"
                                    ? Visibility.Visible
                                    : Visibility.Collapsed
                        };
                    })
                    .ToList();

                ClaimsItemsControl.ItemsSource =
                    displayClaims;

                ClaimsItemsControl.Visibility =
                    displayClaims.Count > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                ClaimsEmptyState.Visibility =
                    displayClaims.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                ClaimsPageCountTextBlock.Text =
                    $"{displayClaims.Count} claim" +
                    (displayClaims.Count == 1 ? "" : "s");

                // LIVE CLAIMS OVERVIEW: count each current status.
                // Recalculated every time claims are refreshed.
                PendingClaimsOverviewCountTextBlock.Text =
                    claims.Count(claim => claim.Status == "Pending")
                        .ToString();

                ApprovedClaimsOverviewCountTextBlock.Text =
                    claims.Count(claim => claim.Status == "Approved")
                        .ToString();

                CollectedClaimsOverviewCountTextBlock.Text =
                    claims.Count(claim => claim.Status == "Collected")
                        .ToString();

                int pendingCount =
                    await _claimService.GetPendingClaimCountAsync(
                        CurrentUser);

                OpenClaimsCountTextBlock.Text =
                    pendingCount.ToString();

                return true;
            }

            catch (Exception ex)
            {
                // Display an error if claim records cannot be loaded.
                LostLinkDialog.ShowMessage(
                    this,
                    "Claims Loading Error",
                    "Unable to load claims.\n\n" + ex.Message,
                    LostLinkDialogType.Error);

                return false;
            }
        }


        // APPROVE CLAIM - ADMINISTRATOR ONLY
        private async void ApproveClaimButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button ||
                button.Tag is not ClaimDisplay claim)
            {
                return;
            }

            // Only administrators are permitted to approve claims.
            if (CurrentUser.Role != UserRole.Administrator)
            {
                LostLinkDialog.ShowMessage(
                    this,
                    "Access Denied",
                    "Only administrators can approve claims.",
                    LostLinkDialogType.Warning);

                return;
            }

            // Ask for confirmation before updating the claim status.
            bool confirmation = LostLinkDialog.ShowConfirmation(
                this,
                "Approve Claim",
                $"Approve claim #{claim.ClaimId} for {claim.ClaimantName}?");

            if (!confirmation)
            {
                return;
            }

            await ExecuteClaimActionAsync(
                () => _claimService.ApproveClaimAsync(
                    CurrentUser,
                    claim.ClaimId),
                "Claim approved. The item has not yet been collected.");
        }

        // REJECT CLAIM - ADMINISTRATOR ONLY
        private async void RejectClaimButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button ||
                button.Tag is not ClaimDisplay claim)
            {
                return;
            }

            // Prevent staff users from rejecting ownership claims.
            if (CurrentUser.Role != UserRole.Administrator)
            {
                LostLinkDialog.ShowMessage(
                    this,
                    "Access Denied",
                    "Only administrators can reject claims.",
                    LostLinkDialogType.Warning);

                return;
            }

            // Require confirmation before rejecting the claim.
            bool confirmation = LostLinkDialog.ShowConfirmation(
                this,
                "Reject Claim",
                $"Reject claim #{claim.ClaimId} for {claim.ClaimantName}?");

            if (!confirmation)
            {
                return;
            }

            await ExecuteClaimActionAsync(
                () => _claimService.RejectClaimAsync(
                    CurrentUser,
                    claim.ClaimId),
                "Claim rejected.");
        }

        // MARK APPROVED CLAIM AS COLLECTED
        private async void CollectClaimButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button ||
                button.Tag is not ClaimDisplay claim)
            {
                return;
            }

            // Confirm that the item has physically been handed over.
            bool confirmation = LostLinkDialog.ShowConfirmation(
                this,
                "Confirm Collection",
                $"Confirm that the item for claim #{claim.ClaimId} " +
                $"has physically been handed to {claim.ClaimantName}?");

            if (!confirmation)
            {
                return;
            }

            await ExecuteClaimActionAsync(
                () => _claimService.MarkCollectedAsync(
                    CurrentUser,
                    claim.ClaimId),
                "Item collection recorded successfully.");
        }

        // EXECUTE CLAIM ACTION AND REFRESH
        private async Task ExecuteClaimActionAsync(
            Func<Task> action,
            string successMessage)
        {
            try
            {
                // Execute the requested claim operation.
                await action();

                // Display a consistent success notification.
                LostLinkDialog.ShowMessage(
                    this,
                    "LostLink",
                    successMessage,
                    LostLinkDialogType.Success);

                // Refresh the item and claim displays after the update.
                await RefreshAllItemsAsync();
                await RefreshClaimsAsync();
            }
            catch (Exception ex)
            {
                // Display the error without closing the main window.
                LostLinkDialog.ShowMessage(
                    this,
                    "Claim Action Failed",
                    "The claim action could not be completed.\n\n" +
                    ex.Message,
                    LostLinkDialogType.Error);

                await RefreshClaimsAsync();
            }
        }


        // LOGOUT - STAFF AND ADMINISTRATOR
        private void LogoutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Display a custom LostLink confirmation dialog before logging out.
            bool confirmation = LostLinkDialog.ShowConfirmation(
                this,
                "Confirm Logout",
                "Are you sure you want to log out of LostLink?");

            if (!confirmation)
            {
                return;
            }

            try
            {
                // Create the existing login window.
                LoginWindow loginWindow =
                    new LoginWindow();

                // Set the login window as the application's
                // main window before closing this window.
                Application.Current.MainWindow =
                    loginWindow;

                // Display the login screen.
                loginWindow.Show();

                // Close the current staff/admin workspace.
                Close();
            }

            catch (Exception ex)
            {
                // Notify the user if returning to login fails.
                LostLinkDialog.ShowMessage(
                    this,
                    "Logout Error",
                    "Unable to return to the login page.\n\n" +
                    ex.Message,
                    LostLinkDialogType.Error);
            }
        }

        // FORMAT USER ROLE
        private static string FormatRole(string role)
        {
            return role switch
            {
                "Administrator" => "Administrator",
                "Staff" => "Staff",
                _ => role
            };
        }
    }

    // SMART MATCH DISPLAY MODEL
    public class SmartMatchDisplay
    {
        public int LostItemId { get; set; }

        public int FoundItemId { get; set; }

        public string LostTitle { get; set; } =
            string.Empty;

        public string FoundTitle { get; set; } =
            string.Empty;

        public string? LostImagePath { get; set; }

        public string? FoundImagePath { get; set; }

        public string LostLocation { get; set; } =
            string.Empty;

        public string FoundLocation { get; set; } =
            string.Empty;

        public DateTime LostDate { get; set; }

        public DateTime FoundDate { get; set; }

        public double MatchScore { get; set; }

        public string Classification { get; set; } =
            string.Empty;

        public bool CategoryMatched { get; set; }

        public bool ColourMatched { get; set; }

        public bool LocationMatched { get; set; }

        public double DateScore { get; set; }

        public double KeywordScore { get; set; }

        public string MatchedKeywords { get; set; } =
            string.Empty;

        public string CategoryMatchText =>
            CategoryMatched
                ? "Matched"
                : "Not matched";

        public string ColourMatchText =>
            ColourMatched
                ? "Matched"
                : "Not matched";

        public string LocationMatchText =>
            LocationMatched
                ? "Matched"
                : "Not matched";
    }

    // CLAIMS MANAGEMENT DISPLAY MODEL
    public class ClaimDisplay
    {
        public int ClaimId { get; set; }

        public string ClaimTitle { get; set; } =
            string.Empty;

        public string FoundItemTitle { get; set; } =
            string.Empty;

        public string ClaimantName { get; set; } =
            string.Empty;

        public string VerificationNotes { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public DateTime CreatedAt { get; set; }

        public string SubmittedByName { get; set; } =
            string.Empty;

        public string LostReportText { get; set; } =
            string.Empty;

        public string ProcessedByText { get; set; } =
            string.Empty;

        public Visibility ApprovalButtonsVisibility { get; set; } =
            Visibility.Collapsed;

        public Visibility CollectionButtonVisibility { get; set; } =
            Visibility.Collapsed;
    }
}
