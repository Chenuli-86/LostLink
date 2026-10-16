// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: ClaimService.cs
//
// Purpose:
// Owens items in LostLink (manage ownership claims).
// Validates Claim Information and permissions, assists Claim submission,
// Claim Approval, Rejection, and Claim collection, updates
// the status on related item/claims in SQLite.
// Implements database transactions to ensure consistency, and
// logs claim activities for auditing.
// ============================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using LostLink.Enums;
using LostLink.Models;
using Microsoft.Data.Sqlite;

namespace LostLink.Services
{
    public class ClaimService
    {
        private const string ConnectionString =
            "Data Source=lostlink.db";

        // Verify permissions against the database, not
        // just the role stored in the UI.
        private static async Task VerifyRoleAsync(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            User user,
            bool administratorOnly = false)
        {
            if (user == null)
                throw new UnauthorizedAccessException(
                    "Please log in first.");

            await using var command =
                connection.CreateCommand();

            command.Transaction = transaction;

            command.CommandText = @"
                SELECT Role
                FROM Users
                WHERE UserId = $userId
                  AND IsActive = 1;
            ";

            command.Parameters.AddWithValue(
                "$userId", user.UserId);

            object? result =
                await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                throw new UnauthorizedAccessException(
                    "Your account is not active or does not exist.");

            string role = Convert.ToString(result) ?? "";

            bool isAdministrator =
                role == nameof(UserRole.Administrator);

            bool isStaff =
                role == nameof(UserRole.Staff);

            if (administratorOnly && !isAdministrator)
                throw new UnauthorizedAccessException(
                    "Only administrators can approve or reject claims.");

            if (!isStaff && !isAdministrator)
                throw new UnauthorizedAccessException(
                    "You do not have permission to manage claims.");
        }

        // 1. STAFF / ADMIN: SUBMIT A STUDENT'S CLAIM
        public async Task<int> SubmitClaimAsync(
            User currentUser,
            int foundItemId,
            string claimantName,
            string verificationNotes,
            int? lostItemId = null)
        {
            if (string.IsNullOrWhiteSpace(claimantName))
                throw new ArgumentException(
                    "Please enter the student's name.");

            if (string.IsNullOrWhiteSpace(verificationNotes))
                throw new ArgumentException(
                    "Please enter identifying details or ownership evidence.");

            if (foundItemId <= 0)
                throw new ArgumentException(
                    "Please select a valid found item.");

            if (claimantName.Trim().Length > 150)
                throw new ArgumentException(
                    "The student's name is too long.");

            if (verificationNotes.Trim().Length > 3000)
                throw new ArgumentException(
                    "Verification notes must not exceed 3000 characters.");

            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await using var transaction =
                (SqliteTransaction)
                await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                await VerifyRoleAsync(
                    connection, transaction, currentUser);

                // Check the found item.
                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        SELECT COUNT(*)
                        FROM Items
                        WHERE ItemId = $itemId
                          AND ItemType = 'Found'
                          AND Status = 'Open';
                    ";

                    command.Parameters.AddWithValue(
                        "$itemId", foundItemId);

                    long count = Convert.ToInt64(
                        await command.ExecuteScalarAsync());

                    if (count == 0)
                        throw new InvalidOperationException(
                            "This found item is not available for claiming.");
                }

                // An already approved or collected item
                // must not receive another claim.
                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        SELECT COUNT(*)
                        FROM Claims
                        WHERE FoundItemId = $foundItemId
                          AND Status IN ('Approved', 'Collected');
                    ";

                    command.Parameters.AddWithValue(
                        "$foundItemId", foundItemId);

                    long count = Convert.ToInt64(
                        await command.ExecuteScalarAsync());

                    if (count > 0)
                        throw new InvalidOperationException(
                            "This item already has an approved or completed claim.");
                }

                // Prevent an obvious duplicate pending claim
                // for the same student and found item.
                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        SELECT COUNT(*)
                        FROM Claims
                        WHERE FoundItemId = $foundItemId
                          AND Status = 'Pending'
                          AND LOWER(TRIM(ClaimantName)) =
                              LOWER(TRIM($claimantName));
                    ";

                    command.Parameters.AddWithValue(
                        "$foundItemId", foundItemId);

                    command.Parameters.AddWithValue(
                        "$claimantName", claimantName.Trim());

                    long count = Convert.ToInt64(
                        await command.ExecuteScalarAsync());

                    if (count > 0)
                        throw new InvalidOperationException(
                            "A pending claim already exists for this student and item.");
                }

                // Optional linked lost report.
                if (lostItemId.HasValue)
                {
                    await using var command =
                        connection.CreateCommand();

                    command.Transaction = transaction;

                    command.CommandText = @"
                        SELECT COUNT(*)
                        FROM Items
                        WHERE ItemId = $lostItemId
                          AND ItemType = 'Lost'
                          AND Status = 'Open';
                    ";

                    command.Parameters.AddWithValue(
                        "$lostItemId", lostItemId.Value);

                    long count = Convert.ToInt64(
                        await command.ExecuteScalarAsync());

                    if (count == 0)
                        throw new InvalidOperationException(
                            "The linked lost report is not available.");
                }

                int claimId;

                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        INSERT INTO Claims (
                            LostItemId,
                            FoundItemId,
                            ClaimantUserId,
                            ClaimantName,
                            VerificationNotes,
                            Status,
                            SubmittedBy,
                            ProcessedBy,
                            CreatedAt,
                            ProcessedAt,
                            CollectedAt
                        )
                        VALUES (
                            $lostItemId,
                            $foundItemId,
                            NULL,
                            $claimantName,
                            $notes,
                            'Pending',
                            $submittedBy,
                            NULL,
                            $createdAt,
                            NULL,
                            NULL
                        );

                        SELECT last_insert_rowid();
                    ";

                    command.Parameters.AddWithValue(
                        "$lostItemId",
                        (object?)lostItemId ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "$foundItemId", foundItemId);

                    command.Parameters.AddWithValue(
                        "$claimantName", claimantName.Trim());

                    command.Parameters.AddWithValue(
                        "$notes", verificationNotes.Trim());

                    command.Parameters.AddWithValue(
                        "$submittedBy", currentUser.UserId);

                    command.Parameters.AddWithValue(
                        "$createdAt",
                        DateTime.UtcNow.ToString("O"));

                    claimId = Convert.ToInt32(
                        await command.ExecuteScalarAsync());
                }

                await AddActivityLogAsync(
                    connection,
                    transaction,
                    currentUser.UserId,
                    "Claim Submitted",
                    $"Claim #{claimId} recorded for found item #{foundItemId}.");

                await transaction.CommitAsync();

                return claimId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 2. STAFF / ADMIN: VIEW ALL CLAIMS
        public async Task<List<Claim>> GetAllClaimsAsync(
            User currentUser)
        {
            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await VerifyRoleAsync(
                connection, null, currentUser);

            await using var command =
                connection.CreateCommand();

            command.CommandText = @"
                SELECT
                    ClaimId,
                    LostItemId,
                    FoundItemId,
                    ClaimantUserId,
                    ClaimantName,
                    VerificationNotes,
                    Status,
                    SubmittedBy,
                    ProcessedBy,
                    CreatedAt,
                    ProcessedAt,
                    CollectedAt
                FROM Claims
                ORDER BY
                    CASE Status
                        WHEN 'Pending' THEN 0
                        WHEN 'Approved' THEN 1
                        WHEN 'Rejected' THEN 2
                        WHEN 'Collected' THEN 3
                        ELSE 4
                    END,
                    CreatedAt DESC;
            ";

            return await ReadClaimsAsync(command);
        }

        // 3. STAFF / ADMIN: VIEW CLAIMS THEY RECORDED
        public async Task<List<Claim>> GetSubmittedClaimsAsync(
            User currentUser)
        {
            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await VerifyRoleAsync(
                connection, null, currentUser);

            await using var command =
                connection.CreateCommand();

            command.CommandText = @"
                SELECT
                    ClaimId,
                    LostItemId,
                    FoundItemId,
                    ClaimantUserId,
                    ClaimantName,
                    VerificationNotes,
                    Status,
                    SubmittedBy,
                    ProcessedBy,
                    CreatedAt,
                    ProcessedAt,
                    CollectedAt
                FROM Claims
                WHERE SubmittedBy = $userId
                ORDER BY CreatedAt DESC;
            ";

            command.Parameters.AddWithValue(
                "$userId", currentUser.UserId);

            return await ReadClaimsAsync(command);
        }

        // 4. ADMIN ONLY: APPROVE
        public Task ApproveClaimAsync(
            User currentUser,
            int claimId)
        {
            return ProcessClaimAsync(
                currentUser, claimId, "Approved");
        }

        // 5. ADMIN ONLY: REJECT
        public Task RejectClaimAsync(
            User currentUser,
            int claimId)
        {
            return ProcessClaimAsync(
                currentUser, claimId, "Rejected");
        }

        private async Task ProcessClaimAsync(
            User currentUser,
            int claimId,
            string newStatus)
        {
            if (newStatus != "Approved" &&
                newStatus != "Rejected")
                throw new ArgumentException(
                    "Invalid claim decision.");

            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await using var transaction =
                (SqliteTransaction)
                await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                await VerifyRoleAsync(
                    connection,
                    transaction,
                    currentUser,
                    administratorOnly: true);

                int foundItemId;

                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        SELECT FoundItemId
                        FROM Claims
                        WHERE ClaimId = $claimId
                          AND Status = 'Pending';
                    ";

                    command.Parameters.AddWithValue(
                        "$claimId", claimId);

                    object? result =
                        await command.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value)
                        throw new InvalidOperationException(
                            "Only pending claims can be reviewed.");

                    foundItemId = Convert.ToInt32(result);
                }

                if (newStatus == "Approved")
                {
                    // Confirm that the item is still open.
                    await using (var command =
                        connection.CreateCommand())
                    {
                        command.Transaction = transaction;

                        command.CommandText = @"
                            SELECT COUNT(*)
                            FROM Items
                            WHERE ItemId = $foundItemId
                              AND ItemType = 'Found'
                              AND Status = 'Open';
                        ";

                        command.Parameters.AddWithValue(
                            "$foundItemId", foundItemId);

                        long count = Convert.ToInt64(
                            await command.ExecuteScalarAsync());

                        if (count == 0)
                            throw new InvalidOperationException(
                                "The found item is no longer available.");
                    }

                    // Prevent multiple approved claims.
                    await using (var command =
                        connection.CreateCommand())
                    {
                        command.Transaction = transaction;

                        command.CommandText = @"
                            SELECT COUNT(*)
                            FROM Claims
                            WHERE FoundItemId = $foundItemId
                              AND Status IN ('Approved', 'Collected');
                        ";

                        command.Parameters.AddWithValue(
                            "$foundItemId", foundItemId);

                        long count = Convert.ToInt64(
                            await command.ExecuteScalarAsync());

                        if (count > 0)
                            throw new InvalidOperationException(
                                "Another claim has already been approved.");
                    }
                }

                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        UPDATE Claims
                        SET Status = $status,
                            ProcessedBy = $processedBy,
                            ProcessedAt = $processedAt
                        WHERE ClaimId = $claimId
                          AND Status = 'Pending';
                    ";

                    command.Parameters.AddWithValue(
                        "$status", newStatus);

                    command.Parameters.AddWithValue(
                        "$processedBy", currentUser.UserId);

                    command.Parameters.AddWithValue(
                        "$processedAt",
                        DateTime.UtcNow.ToString("O"));

                    command.Parameters.AddWithValue(
                        "$claimId", claimId);

                    int updated =
                        await command.ExecuteNonQueryAsync();

                    if (updated != 1)
                        throw new InvalidOperationException(
                            "The claim could not be updated.");
                }

                await AddActivityLogAsync(
                    connection,
                    transaction,
                    currentUser.UserId,
                    $"Claim {newStatus}",
                    $"Claim #{claimId} was {newStatus.ToLowerInvariant()}.");

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 6. STAFF / ADMIN: CONFIRM COLLECTION
        public async Task MarkCollectedAsync(
            User currentUser,
            int claimId)
        {
            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await using var transaction =
                (SqliteTransaction)
                await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                await VerifyRoleAsync(
                    connection, transaction, currentUser);

                int foundItemId;
                int? lostItemId;

                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        SELECT FoundItemId, LostItemId
                        FROM Claims
                        WHERE ClaimId = $claimId
                          AND Status = 'Approved';
                    ";

                    command.Parameters.AddWithValue(
                        "$claimId", claimId);

                    await using var reader =
                        await command.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        throw new InvalidOperationException(
                            "Only approved claims can be collected.");

                    foundItemId = reader.GetInt32(0);

                    lostItemId = reader.IsDBNull(1)
                        ? null
                        : reader.GetInt32(1);
                }

                // Update the found item only if it is open.
                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        UPDATE Items
                        SET Status = 'Claimed'
                        WHERE ItemId = $itemId
                          AND ItemType = 'Found'
                          AND Status = 'Open';
                    ";

                    command.Parameters.AddWithValue(
                        "$itemId", foundItemId);

                    int updated =
                        await command.ExecuteNonQueryAsync();

                    if (updated != 1)
                        throw new InvalidOperationException(
                            "The found item is no longer available.");
                }

                // Close the linked lost report, if present.
                if (lostItemId.HasValue)
                {
                    await using var command =
                        connection.CreateCommand();

                    command.Transaction = transaction;

                    command.CommandText = @"
                        UPDATE Items
                        SET Status = 'Claimed'
                        WHERE ItemId = $itemId
                          AND ItemType = 'Lost'
                          AND Status = 'Open';
                    ";

                    command.Parameters.AddWithValue(
                        "$itemId", lostItemId.Value);

                    await command.ExecuteNonQueryAsync();
                }

                // Complete the approved claim.
                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        UPDATE Claims
                        SET Status = 'Collected',
                            CollectedAt = $collectedAt
                        WHERE ClaimId = $claimId
                          AND Status = 'Approved';
                    ";

                    command.Parameters.AddWithValue(
                        "$collectedAt",
                        DateTime.UtcNow.ToString("O"));

                    command.Parameters.AddWithValue(
                        "$claimId", claimId);

                    int updated =
                        await command.ExecuteNonQueryAsync();

                    if (updated != 1)
                        throw new InvalidOperationException(
                            "The claim could not be completed.");
                }

                // Reject other pending claims for this item,
                // so they do not remain in the pending queue.
                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        UPDATE Claims
                        SET Status = 'Rejected',
                            ProcessedBy = $processedBy,
                            ProcessedAt = $processedAt
                        WHERE FoundItemId = $foundItemId
                          AND ClaimId <> $claimId
                          AND Status = 'Pending';
                    ";

                    command.Parameters.AddWithValue(
                        "$processedBy", currentUser.UserId);

                    command.Parameters.AddWithValue(
                        "$processedAt",
                        DateTime.UtcNow.ToString("O"));

                    command.Parameters.AddWithValue(
                        "$foundItemId", foundItemId);

                    command.Parameters.AddWithValue(
                        "$claimId", claimId);

                    await command.ExecuteNonQueryAsync();
                }

                await AddActivityLogAsync(
                    connection,
                    transaction,
                    currentUser.UserId,
                    "Item Collected",
                    $"Claim #{claimId} completed for found item #{foundItemId}.");

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 7. DASHBOARD: COUNT PENDING CLAIMS
        public async Task<int> GetPendingClaimCountAsync(
            User currentUser)
        {
            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await VerifyRoleAsync(
                connection, null, currentUser);

            await using var command =
                connection.CreateCommand();

            command.CommandText = @"
                SELECT COUNT(*)
                FROM Claims
                WHERE Status = 'Pending';
            ";

            return Convert.ToInt32(
                await command.ExecuteScalarAsync());
        }

        // Convert SQLite rows into Claim objects.
        private static async Task<List<Claim>> ReadClaimsAsync(
            SqliteCommand command)
        {
            var claims = new List<Claim>();

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                claims.Add(new Claim
                {
                    ClaimId = reader.GetInt32(0),

                    LostItemId = reader.IsDBNull(1)
                        ? null
                        : reader.GetInt32(1),

                    FoundItemId = reader.GetInt32(2),

                    ClaimantUserId = reader.IsDBNull(3)
                        ? null
                        : reader.GetInt32(3),

                    ClaimantName = reader.GetString(4),

                    VerificationNotes = reader.GetString(5),

                    Status = reader.GetString(6),

                    SubmittedBy = reader.IsDBNull(7)
                        ? null
                        : reader.GetInt32(7),

                    ProcessedBy = reader.IsDBNull(8)
                        ? null
                        : reader.GetInt32(8),

                    CreatedAt = DateTime.Parse(
                        reader.GetString(9)),

                    ProcessedAt = reader.IsDBNull(10)
                        ? null
                        : DateTime.Parse(reader.GetString(10)),

                    CollectedAt = reader.IsDBNull(11)
                        ? null
                        : DateTime.Parse(reader.GetString(11))
                });
            }

            return claims;
        }

        // Save an audit entry with the claim operation.
        private static async Task AddActivityLogAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int userId,
            string action,
            string details)
        {
            await using var command =
                connection.CreateCommand();

            command.Transaction = transaction;

            command.CommandText = @"
                INSERT INTO ActivityLogs (
                    UserId,
                    Action,
                    Details,
                    CreatedAt
                )
                VALUES (
                    $userId,
                    $action,
                    $details,
                    $createdAt
                );
            ";

            command.Parameters.AddWithValue(
                "$userId", userId);

            command.Parameters.AddWithValue(
                "$action", action);

            command.Parameters.AddWithValue(
                "$details", details);

            command.Parameters.AddWithValue(
                "$createdAt",
                DateTime.UtcNow.ToString("O"));

            await command.ExecuteNonQueryAsync();
        }
    }
}
