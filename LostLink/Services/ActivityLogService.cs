// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: ActivityLogService.cs
//
// Purpose: Records and retrieves the activities of users for system auditing purposes.
// ============================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using LostLink.Enums;
using LostLink.Models;

namespace LostLink.Services
{
    public class ActivityLogService
    {
        private const string ConnectionString =
            "Data Source=lostlink.db";

        // Retrieve recorded activities, newest first.
        // Only active Administrator and Staff accounts
        // can access the activity history.
        public async Task<List<ActivityLog>> GetAllActivitiesAsync(
            User currentUser)
        {
            var activities = new List<ActivityLog>();

            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await VerifyUserAsync(connection, currentUser);

            await using var command =
                connection.CreateCommand();

            command.CommandText = @"
                SELECT
                    a.LogId,
                    a.UserId,
                    a.Action,
                    a.Details,
                    a.CreatedAt,
                    COALESCE(u.FullName, 'Unknown user'),
                    COALESCE(u.Role, 'Unknown')
                FROM ActivityLogs a
                LEFT JOIN Users u
                    ON a.UserId = u.UserId
                ORDER BY a.CreatedAt DESC, a.LogId DESC;
            ";

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string dateValue = reader.GetString(4);

                DateTime createdAt = DateTime.Parse(
                    dateValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind);

                activities.Add(new ActivityLog
                {
                    LogId = reader.GetInt32(0),

                    UserId = reader.GetInt32(1),

                    Action = reader.GetString(2),

                    Details = reader.GetString(3),

                    CreatedAt = createdAt.ToLocalTime(),

                    PerformedBy = reader.GetString(5),

                    UserRole = reader.GetString(6)
                });
            }

            return activities;
        }

        // Save an activity performed by an active
        // Administrator or Staff account.
        public async Task RecordActivityAsync(
            int userId,
            string action,
            string details)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                throw new ArgumentException(
                    "An activity action is required.",
                    nameof(action));
            }

            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await using var transaction =
                (SqliteTransaction)
                await connection.BeginTransactionAsync();

            try
            {
                await using (var verifyCommand =
                    connection.CreateCommand())
                {
                    verifyCommand.Transaction = transaction;

                    verifyCommand.CommandText = @"
                        SELECT Role
                        FROM Users
                        WHERE UserId = $userId
                          AND IsActive = 1;
                    ";

                    verifyCommand.Parameters.AddWithValue(
                        "$userId", userId);

                    object? result =
                        await verifyCommand.ExecuteScalarAsync();

                    string role =
                        Convert.ToString(result) ?? "";

                    if (role != nameof(UserRole.Administrator) &&
                        role != nameof(UserRole.Staff))
                    {
                        throw new UnauthorizedAccessException(
                            "Only active Administrator or Staff accounts " +
                            "can record activities.");
                    }
                }

                await using (var insertCommand =
                    connection.CreateCommand())
                {
                    insertCommand.Transaction = transaction;

                    insertCommand.CommandText = @"
                        INSERT INTO ActivityLogs
                        (
                            UserId,
                            Action,
                            Details,
                            CreatedAt
                        )
                        VALUES
                        (
                            $userId,
                            $action,
                            $details,
                            $createdAt
                        );
                    ";

                    insertCommand.Parameters.AddWithValue(
                        "$userId", userId);

                    insertCommand.Parameters.AddWithValue(
                        "$action", action.Trim());

                    insertCommand.Parameters.AddWithValue(
                        "$details", details ?? string.Empty);

                    insertCommand.Parameters.AddWithValue(
                        "$createdAt",
                        DateTime.UtcNow.ToString("O"));

                    await insertCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Verify that the current user is an active
        // Administrator or Staff member.
        private static async Task VerifyUserAsync(
            SqliteConnection connection,
            User currentUser)
        {
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException(
                    "Please log in to view activity history.");
            }

            await using var command =
                connection.CreateCommand();

            command.CommandText = @"
                SELECT Role
                FROM Users
                WHERE UserId = $userId
                  AND IsActive = 1;
            ";

            command.Parameters.AddWithValue(
                "$userId", currentUser.UserId);

            object? result =
                await command.ExecuteScalarAsync();

            string role =
                Convert.ToString(result) ?? "";

            if (role != nameof(UserRole.Administrator) &&
                role != nameof(UserRole.Staff))
            {
                throw new UnauthorizedAccessException(
                    "You do not have permission to view activity history.");
            }
        }
    }
}
