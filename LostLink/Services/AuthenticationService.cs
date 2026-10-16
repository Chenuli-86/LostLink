// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: AuthenticationService.cs
//
// Purpose: Manages user authentication, password security and default accounts.
// ============================================================

using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using LostLink.Enums;
using LostLink.Models;

namespace LostLink.Services
{
    public class AuthenticationService
    {
        private const string ConnectionString = "Data Source=lostlink.db";

        // Creates a salted password hash for secure storage.
        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                32);

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        // Checks an entered password against the stored salted hash.
        public bool VerifyPassword(string password, string storedPasswordHash)
        {
            string[] parts = storedPasswordHash.Split(':');

            if (parts.Length != 2)
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHash = Convert.FromBase64String(parts[1]);

            byte[] enteredHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                32);

            return CryptographicOperations.FixedTimeEquals(
                enteredHash,
                storedHash);
        }


        // Creates the initial Administrator and Staff accounts
        // if their usernames do not already exist in the database.
        public async Task CreateInitialAdminAsync()
        {
            await EnsureInitialAccountAsync(
                "admin",
                "Admin@123",
                "System Administrator",
                UserRole.Administrator);

            await EnsureInitialAccountAsync(
                "staff",
                "Staff@LostLink123",
                "Campus Staff",
                UserRole.Staff);
        }

        // Ensures that each default account is created only once.
        // Existing account records and passwords are preserved.
        private async Task EnsureInitialAccountAsync(
            string username,
            string password,
            string fullName,
            UserRole role)
        {
            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            // Check whether this specific username already exists.
            await using var checkCommand =
                connection.CreateCommand();

            checkCommand.CommandText = @"
         SELECT COUNT(*)
         FROM Users
         WHERE Username = $username;
     ";

            checkCommand.Parameters.AddWithValue(
                "$username",
                username);

            long accountCount = Convert.ToInt64(
                await checkCommand.ExecuteScalarAsync());

            // Preserve existing accounts without changing passwords.
            if (accountCount > 0)
            {
                return;
            }

            // Hash the initial password before inserting the account.
            string passwordHash = HashPassword(password);

            await using var insertCommand =
                connection.CreateCommand();

            insertCommand.CommandText = @"
         INSERT INTO Users
         (
             Username,
             PasswordHash,
             FullName,
             Role,
             IsActive,
             CreatedAt
         )
         VALUES
         (
             $username,
             $passwordHash,
             $fullName,
             $role,
             $isActive,
             $createdAt
         );
     ";

            insertCommand.Parameters.AddWithValue(
                "$username",
                username);

            insertCommand.Parameters.AddWithValue(
                "$passwordHash",
                passwordHash);

            insertCommand.Parameters.AddWithValue(
                "$fullName",
                fullName);

            insertCommand.Parameters.AddWithValue(
                "$role",
                role.ToString());

            insertCommand.Parameters.AddWithValue(
                "$isActive",
                1);

            insertCommand.Parameters.AddWithValue(
                "$createdAt",
                DateTime.Now.ToString("O"));

            await insertCommand.ExecuteNonQueryAsync();
        }



        // Authenticates an active LostLink user.
        public async Task<User?> LoginAsync(
            string username,
            string password)
        {
            await using var connection =
                new SqliteConnection(ConnectionString);

            await connection.OpenAsync();

            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT
                    UserId,
                    Username,
                    PasswordHash,
                    FullName,
                    Role,
                    IsActive,
                    CreatedAt
                FROM Users
                WHERE Username = $username
                  AND IsActive = 1;
            ";

            command.Parameters.AddWithValue(
                "$username",
                username.Trim());

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            string storedPasswordHash =
                reader.GetString(2);

            if (!VerifyPassword(password, storedPasswordHash))
            {
                return null;
            }

            return new User
            {
                UserId = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = storedPasswordHash,
                FullName = reader.GetString(3),

                Role = Enum.Parse<UserRole>(
                    reader.GetString(4)),

                IsActive = reader.GetInt32(5) == 1,

                CreatedAt = DateTime.Parse(
                    reader.GetString(6))
            };
        }
    }
}