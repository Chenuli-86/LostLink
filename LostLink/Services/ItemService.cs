// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: ItemService.cs
//
// Purpose: Responsible for the production, retrieval and updating of item reports.
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using LostLink.Enums;
using LostLink.Models;

namespace LostLink.Services
{
    public class ItemService
    {
        private const string ConnectionString =
            "Data Source=lostlink.db";

        // Create a lost item report.
        public async Task<int> CreateLostItemAsync(
            string title,
            string category,
            string colour,
            string location,
            DateTime incidentDate,
            string description,
            int reportedBy,
            string? imagePath = null)
        {
            return await CreateItemAsync(
                title,
                "Lost",
                category,
                colour,
                location,
                incidentDate,
                description,
                reportedBy,
                imagePath);
        }

        // Create a found item report.
        public async Task<int> CreateFoundItemAsync(
            string title,
            string category,
            string colour,
            string location,
            DateTime incidentDate,
            string description,
            int reportedBy,
            string? imagePath = null)
        {
            return await CreateItemAsync(
                title,
                "Found",
                category,
                colour,
                location,
                incidentDate,
                description,
                reportedBy,
                imagePath);
        }

        // Save the item and its activity log together.
        private async Task<int> CreateItemAsync(
            string title,
            string itemType,
            string category,
            string colour,
            string location,
            DateTime incidentDate,
            string description,
            int reportedBy,
            string? imagePath)
        {
            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await using var transaction =
                (SqliteTransaction)
                await connection.BeginTransactionAsync();

            try
            {
                int itemId;

                // Insert the new lost or found item.
                await using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        INSERT INTO Items
                        (
                            Title,
                            ItemType,
                            Category,
                            Colour,
                            Location,
                            IncidentDate,
                            Description,
                            ImagePath,
                            Status,
                            ReportedBy,
                            CreatedAt
                        )
                        VALUES
                        (
                            $title,
                            $itemType,
                            $category,
                            $colour,
                            $location,
                            $incidentDate,
                            $description,
                            $imagePath,
                            $status,
                            $reportedBy,
                            $createdAt
                        );

                        SELECT last_insert_rowid();
                    ";

                    command.Parameters.AddWithValue(
                        "$title", title);

                    command.Parameters.AddWithValue(
                        "$itemType", itemType);

                    command.Parameters.AddWithValue(
                        "$category", category);

                    command.Parameters.AddWithValue(
                        "$colour", colour);

                    command.Parameters.AddWithValue(
                        "$location", location);

                    command.Parameters.AddWithValue(
                        "$incidentDate",
                        incidentDate.ToString("O"));

                    command.Parameters.AddWithValue(
                        "$description", description);

                    command.Parameters.AddWithValue(
                        "$imagePath",
                        (object?)imagePath ?? DBNull.Value);

                    command.Parameters.AddWithValue(
                        "$status", "Open");

                    command.Parameters.AddWithValue(
                        "$reportedBy", reportedBy);

                    command.Parameters.AddWithValue(
                        "$createdAt",
                        DateTime.Now.ToString("O"));

                    object? result =
                        await command.ExecuteScalarAsync();

                    itemId = Convert.ToInt32(result);
                }

                // Record who submitted the item report.
                await using (var logCommand =
                    connection.CreateCommand())
                {
                    logCommand.Transaction = transaction;

                    logCommand.CommandText = @"
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

                    logCommand.Parameters.AddWithValue(
                        "$userId", reportedBy);

                    logCommand.Parameters.AddWithValue(
                        "$action",
                        itemType == "Lost"
                            ? "Lost Item Reported"
                            : "Found Item Reported");

                    logCommand.Parameters.AddWithValue(
                        "$details",
                        $"{itemType} item #{itemId}: " +
                        $"'{title}' reported at {location}.");

                    logCommand.Parameters.AddWithValue(
                        "$createdAt",
                        DateTime.UtcNow.ToString("O"));

                    await logCommand.ExecuteNonQueryAsync();
                }

                // Both records are committed together.
                await transaction.CommitAsync();

                return itemId;
            }
            catch
            {
                // Avoid saving an item without its log.
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Retrieve lost items.
        public async Task<List<Item>> GetLostItemsAsync()
        {
            return await GetItemsByTypeAsync("Lost");
        }

        // Retrieve found items.
        public async Task<List<Item>> GetFoundItemsAsync()
        {
            return await GetItemsByTypeAsync("Found");
        }

        // Retrieve items of the requested type.
        private async Task<List<Item>> GetItemsByTypeAsync(
            string itemType)
        {
            List<Item> items = new List<Item>();

            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            await using var command =
                connection.CreateCommand();

            command.CommandText = @"
                SELECT
                    ItemId,
                    Title,
                    ItemType,
                    Category,
                    Colour,
                    Location,
                    IncidentDate,
                    Description,
                    ImagePath,
                    Status,
                    ReportedBy,
                    CreatedAt
                FROM Items
                WHERE ItemType = $itemType
                ORDER BY CreatedAt DESC;
            ";

            command.Parameters.AddWithValue(
                "$itemType", itemType);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Item item = new Item
                {
                    ItemId = reader.GetInt32(0),

                    Title = reader.GetString(1),

                    Type = Enum.Parse<ItemType>(
                        reader.GetString(2)),

                    Category = reader.GetString(3),

                    Colour = reader.GetString(4),

                    Location = reader.GetString(5),

                    IncidentDate = DateTime.Parse(
                        reader.GetString(6)),

                    Description = reader.GetString(7),

                    ImagePath = reader.IsDBNull(8)
                        ? null
                        : reader.GetString(8),

                    Status = Enum.Parse<ItemStatus>(
                        reader.GetString(9)),

                    ReportedBy = reader.GetInt32(10),

                    CreatedAt = DateTime.Parse(
                        reader.GetString(11))
                };

                items.Add(item);
            }

            return items;
        }
    }
}
