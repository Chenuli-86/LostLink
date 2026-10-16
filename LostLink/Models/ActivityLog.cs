// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// Date: 13 October 2026
//
// Purpose: Records user actions and system activities.
// ============================================================

using System;

namespace LostLink.Models
{
    // Represents one recorded action in the ActivityLogs table.
    public class ActivityLog
    {
        public int LogId { get; set; }

        public int UserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Display information for the Activity Log page.
        public string PerformedBy { get; set; } = string.Empty;

        public string UserRole { get; set; } = string.Empty;
    }
}
