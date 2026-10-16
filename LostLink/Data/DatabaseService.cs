// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: DatabaseService.cs
//
// Purpose: Initialises and maintains the SQLite database and tables.
// ============================================================

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace LostLink.Data
{
    public class DatabaseService
    {
        private const string ConnectionString =
            "Data Source=lostlink.db";

        public async Task InitializeDatabaseAsync()
        {
            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            // Enable SQLite foreign-key enforcement.
            await using (var foreignKeyCommand =
                connection.CreateCommand())
            {
                foreignKeyCommand.CommandText =
                    "PRAGMA foreign_keys = ON;";

                await foreignKeyCommand.ExecuteNonQueryAsync();
            }

            // Preserve existing application tables.
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        FullName TEXT NOT NULL,
                        Role TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Items (
                        ItemId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        ItemType TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        Colour TEXT NOT NULL,
                        Location TEXT NOT NULL,
                        IncidentDate TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        ImagePath TEXT,
                        Status TEXT NOT NULL,
                        ReportedBy INTEGER NOT NULL,
                        CreatedAt TEXT NOT NULL,

                        FOREIGN KEY (ReportedBy)
                            REFERENCES Users(UserId)
                    );

                    CREATE TABLE IF NOT EXISTS MatchResults (
                        MatchId INTEGER PRIMARY KEY AUTOINCREMENT,
                        LostItemId INTEGER NOT NULL,
                        FoundItemId INTEGER NOT NULL,
                        MatchScore REAL NOT NULL,
                        Classification TEXT NOT NULL,
                        CategoryMatched INTEGER NOT NULL,
                        ColourMatched INTEGER NOT NULL,
                        LocationMatched INTEGER NOT NULL,
                        DateScore REAL NOT NULL,
                        KeywordScore REAL NOT NULL,
                        MatchedKeywords TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,

                        FOREIGN KEY (LostItemId)
                            REFERENCES Items(ItemId),

                        FOREIGN KEY (FoundItemId)
                            REFERENCES Items(ItemId)
                    );

                    CREATE TABLE IF NOT EXISTS ActivityLogs (
                        LogId INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        Action TEXT NOT NULL,
                        Details TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,

                        FOREIGN KEY (UserId)
                            REFERENCES Users(UserId)
                    );
                ";

                await command.ExecuteNonQueryAsync();
            }

            // Check whether the Claims table already exists.
            bool claimsTableExists;

            await using (var checkCommand =
                connection.CreateCommand())
            {
                checkCommand.CommandText = @"
                    SELECT COUNT(*)
                    FROM sqlite_master
                    WHERE type = 'table'
                      AND name = 'Claims';
                ";

                long count = Convert.ToInt64(
                    await checkCommand.ExecuteScalarAsync());

                claimsTableExists = count > 0;
            }

            if (!claimsTableExists)
            {
                // New installations receive the Claims schema.
                await CreateClaimsTableAsync(connection);
            }
            else
            {
                // Upgrade older Claims tables while retaining records.
                await UpgradeClaimsTableAsync(connection);
            }

            // Add SubmittedBy to databases created before
            // the staff-managed claims workflow.
            await EnsureSubmittedByColumnAsync(connection);

            // Create indexes for faster claim queries.
            await using (var indexCommand =
                connection.CreateCommand())
            {
                indexCommand.CommandText = @"
                    CREATE INDEX IF NOT EXISTS
                        IX_Claims_FoundItemId
                    ON Claims(FoundItemId);

                    CREATE INDEX IF NOT EXISTS
                        IX_Claims_ClaimantUserId
                    ON Claims(ClaimantUserId);

                    CREATE INDEX IF NOT EXISTS
                        IX_Claims_Status
                    ON Claims(Status);

                    CREATE INDEX IF NOT EXISTS
                        IX_Claims_SubmittedBy
                    ON Claims(SubmittedBy);
                ";

                await indexCommand.ExecuteNonQueryAsync();
            }
        }

        // Create Claims table for a new database.
        private static async Task CreateClaimsTableAsync(
            SqliteConnection connection)
        {
            await using var command =
                connection.CreateCommand();

            command.CommandText = @"
                CREATE TABLE Claims (
                    ClaimId INTEGER PRIMARY KEY AUTOINCREMENT,

                    LostItemId INTEGER,

                    FoundItemId INTEGER NOT NULL,

                    ClaimantUserId INTEGER,

                    ClaimantName TEXT NOT NULL,

                    VerificationNotes TEXT NOT NULL,

                    Status TEXT NOT NULL DEFAULT 'Pending',

                    SubmittedBy INTEGER,

                    ProcessedBy INTEGER,

                    CreatedAt TEXT NOT NULL,

                    ProcessedAt TEXT,

                    CollectedAt TEXT,

                    FOREIGN KEY (LostItemId)
                        REFERENCES Items(ItemId),

                    FOREIGN KEY (FoundItemId)
                        REFERENCES Items(ItemId),

                    FOREIGN KEY (ClaimantUserId)
                        REFERENCES Users(UserId),

                    FOREIGN KEY (SubmittedBy)
                        REFERENCES Users(UserId),

                    FOREIGN KEY (ProcessedBy)
                        REFERENCES Users(UserId),

                    CHECK (
                        Status IN (
                            'Pending',
                            'Approved',
                            'Rejected',
                            'Collected'
                        )
                    )
                );
            ";

            await command.ExecuteNonQueryAsync();
        }

        // Upgrade the original Claims table if needed.
        private static async Task UpgradeClaimsTableAsync(
            SqliteConnection connection)
        {
            bool hasClaimantUserId = false;
            bool hasProcessedAt = false;
            bool hasCollectedAt = false;
            bool hasSubmittedBy = false;

            bool lostItemIdIsRequired = false;
            bool processedByIsRequired = false;

            await using (var infoCommand =
                connection.CreateCommand())
            {
                infoCommand.CommandText =
                    "PRAGMA table_info(Claims);";

                await using var reader =
                    await infoCommand.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string columnName =
                        reader.GetString(1);

                    bool isRequired =
                        reader.GetInt32(3) == 1;

                    switch (columnName)
                    {
                        case "ClaimantUserId":
                            hasClaimantUserId = true;
                            break;

                        case "ProcessedAt":
                            hasProcessedAt = true;
                            break;

                        case "CollectedAt":
                            hasCollectedAt = true;
                            break;

                        case "SubmittedBy":
                            hasSubmittedBy = true;
                            break;

                        case "LostItemId":
                            lostItemIdIsRequired = isRequired;
                            break;

                        case "ProcessedBy":
                            processedByIsRequired = isRequired;
                            break;
                    }
                }
            }

            // The previous schema is already compatible.
            // SubmittedBy can be added separately using ALTER TABLE.
            if (hasClaimantUserId &&
                hasProcessedAt &&
                hasCollectedAt &&
                !lostItemIdIsRequired &&
                !processedByIsRequired)
            {
                return;
            }

            // Rebuild the older Claims table in a transaction.
            // Existing claim records are copied before the
            // original table is removed.
            await using var transaction =
                (SqliteTransaction)
                await connection.BeginTransactionAsync();

            try
            {
                await using (var createCommand =
                    connection.CreateCommand())
                {
                    createCommand.Transaction = transaction;

                    createCommand.CommandText = @"
                        CREATE TABLE Claims_Updated (
                            ClaimId INTEGER PRIMARY KEY AUTOINCREMENT,
                            LostItemId INTEGER,
                            FoundItemId INTEGER NOT NULL,
                            ClaimantUserId INTEGER,
                            ClaimantName TEXT NOT NULL,
                            VerificationNotes TEXT NOT NULL,
                            Status TEXT NOT NULL DEFAULT 'Pending',
                            SubmittedBy INTEGER,
                            ProcessedBy INTEGER,
                            CreatedAt TEXT NOT NULL,
                            ProcessedAt TEXT,
                            CollectedAt TEXT,

                            FOREIGN KEY (LostItemId)
                                REFERENCES Items(ItemId),

                            FOREIGN KEY (FoundItemId)
                                REFERENCES Items(ItemId),

                            FOREIGN KEY (ClaimantUserId)
                                REFERENCES Users(UserId),

                            FOREIGN KEY (SubmittedBy)
                                REFERENCES Users(UserId),

                            FOREIGN KEY (ProcessedBy)
                                REFERENCES Users(UserId),

                            CHECK (
                                Status IN (
                                    'Pending',
                                    'Approved',
                                    'Rejected',
                                    'Collected'
                                )
                            )
                        );
                    ";

                    await createCommand.ExecuteNonQueryAsync();
                }

                string claimantUserIdExpression =
                    hasClaimantUserId
                        ? "ClaimantUserId"
                        : "NULL";

                string processedAtExpression =
                    hasProcessedAt
                        ? "ProcessedAt"
                        : "NULL";

                string collectedAtExpression =
                    hasCollectedAt
                        ? "CollectedAt"
                        : "NULL";

                string submittedByExpression =
                    hasSubmittedBy
                        ? "SubmittedBy"
                        : "NULL";

                await using (var copyCommand =
                    connection.CreateCommand())
                {
                    copyCommand.Transaction = transaction;

                    copyCommand.CommandText = $@"
                        INSERT INTO Claims_Updated (
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
                        )
                        SELECT
                            ClaimId,
                            LostItemId,
                            FoundItemId,
                            {claimantUserIdExpression},
                            ClaimantName,
                            VerificationNotes,
                            Status,
                            {submittedByExpression},
                            ProcessedBy,
                            CreatedAt,
                            {processedAtExpression},
                            {collectedAtExpression}
                        FROM Claims;
                    ";

                    await copyCommand.ExecuteNonQueryAsync();
                }

                await using (var dropCommand =
                    connection.CreateCommand())
                {
                    dropCommand.Transaction = transaction;
                    dropCommand.CommandText =
                        "DROP TABLE Claims;";

                    await dropCommand.ExecuteNonQueryAsync();
                }

                await using (var renameCommand =
                    connection.CreateCommand())
                {
                    renameCommand.Transaction = transaction;

                    renameCommand.CommandText = @"
                        ALTER TABLE Claims_Updated
                        RENAME TO Claims;
                    ";

                    await renameCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Ensure older databases also have SubmittedBy.
        private static async Task EnsureSubmittedByColumnAsync(
            SqliteConnection connection)
        {
            bool hasSubmittedBy = false;

            await using (var infoCommand =
                connection.CreateCommand())
            {
                infoCommand.CommandText =
                    "PRAGMA table_info(Claims);";

                await using var reader =
                    await infoCommand.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (reader.GetString(1) == "SubmittedBy")
                    {
                        hasSubmittedBy = true;
                        break;
                    }
                }
            }

            if (hasSubmittedBy)
            {
                return;
            }

            await using var alterCommand =
                connection.CreateCommand();

            alterCommand.CommandText = @"
                ALTER TABLE Claims
                ADD COLUMN SubmittedBy INTEGER
                    REFERENCES Users(UserId);
            ";

            await alterCommand.ExecuteNonQueryAsync();
        }
    }
}
