# LostLink

**LostLink – Smart Campus Lost and Found Management System**

LostLink is a completed C# WPF desktop application developed to manage and match lost and found items within a campus environment. It provides a centralised system for recording, searching, and managing lost and found property, with smart match suggestions and claim management functionality.

## Purpose

The application is designed to help campus staff and administrators efficiently manage lost and found property. LostLink provides a structured approach to reporting items, identifying possible matches, verifying ownership claims, and maintaining records of system activities.

## Implemented Functionality

- Administrator and staff login with role-based access
- Lost item reporting
- Found item reporting
- Optional item photograph uploads
- Item searching and filtering
- Rule-based match scoring
- Smart match suggestions
- Claim submission and management
- Administrator claim approval and rejection
- Dashboard and data visualisation
- Activity logging
- Local SQLite database storage
- Custom application notification and confirmation dialogs

## Smart Matching

LostLink compares characteristics such as item category, colour, location, date, and description keywords to identify potential matches between lost and found item reports.

The application uses rule-based scoring to generate match suggestions and assist staff in identifying possible item matches.

## Technologies

- **Programming Language:** C#
- **Framework:** .NET 10.0
- **Desktop UI:** Windows Presentation Foundation (WPF)
- **Interface Markup:** XAML
- **Database:** SQLite
- **Database Library:** Microsoft.Data.Sqlite (10.0.12)
- **Development Environment:** Visual Studio Community 2026
- **Version Control:** Git and GitHub

## Demonstration Login Accounts

The following accounts are provided for assignment demonstration and evaluation.

### Administrator Account

**Username:** `admin`  
**Password:** `Admin@123`

### Staff Account

**Username:** `staff`  
**Password:** `Staff@LostLink123`

Both accounts are automatically created if they do not already exist when the application starts.

## Application Setup

1. Download or clone the LostLink repository.
2. Open `LostLink.slnx` in Visual Studio 2026.
3. Ensure the .NET 10 SDK and Windows desktop development tools are installed.
4. Restore NuGet packages if prompted.
5. Build the solution.
6. Run the application.
7. The SQLite database and demonstration accounts are automatically created during initial startup.
8. Log in using either demonstration account listed above.

## Project Structure

- **Data:** SQLite database initialisation and configuration
- **Enums:** Application enumerations
- **Models:** Data models for users, items, claims, matches and activity logs
- **Services:** Authentication, item management, matching, claims and activity logging
- **Views:** WPF windows for login, reporting, claim submission and custom dialogs
- **MainWindow.xaml:** Main application interface
- **App.xaml:** Application configuration and startup

## Project Status

**Completed – CSP3341 Programming Languages Assignment 2 (2026)**

The LostLink desktop application has been implemented and tested as part of the university assignment.

## Note

The demonstration accounts and passwords are provided for academic evaluation purposes only. They are not intended for production use.

The application stores its SQLite database locally. Generated database files, uploaded item photographs and compiled build files are excluded from the GitHub repository.
